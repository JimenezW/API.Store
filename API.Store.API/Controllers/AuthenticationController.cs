using API.Store.API.Configurations;
using API.Store.Shared.Auth;
using API.Store.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace API.Store.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Security_Scret _Scret;
        private readonly IEmailSender _emailSender;
        public AuthenticationController(
                UserManager<IdentityUser> userManager, 
                IOptions<Security_Scret> scret,
                IEmailSender emailSender)
        {
            _userManager = userManager;
            _Scret = scret.Value;
            _emailSender = emailSender;
            
        }
        [HttpPost("register")]
        public async Task<IActionResult> register([FromBody] UserRegistrationRequestDtos request)
        {
            if(!ModelState.IsValid) { return  BadRequest(ModelState); }

            var emailExists = await _userManager.FindByEmailAsync(request.EmailAddress);

            if(emailExists != null) 
            { return BadRequest(new AuthResult()
                {
                    Result = true,
                    Errors = new List<string>()
                    {
                        "Email already exists"
                    }
                }); 
            }

            var user = new IdentityUser()
            {
                Email = request.EmailAddress,
                UserName = request.Name,
                EmailConfirmed = false
            };

            var isCreated = await _userManager.CreateAsync(user, request.Password);

            if (isCreated.Succeeded) 
            {
                await SendVerificacionEmail(user);

               // var token = GenerateToken(user);
                return Ok(new AuthResult()
                {
                    Result = true
                    //Token = token
                });
            }
            else
            {
                var errors = new List<string>();
                foreach (var err in isCreated.Errors)
                    errors.Add(err.Description);

                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = errors
                });

            }


        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginResquestDtos dto)
        {
            if (!ModelState.IsValid) return BadRequest();


            var existingUser =  await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser == null)
             return BadRequest(new AuthResult { Errors = new List<string> { "Invalid payload" }, Result = false });

            if(!existingUser.EmailConfirmed)
                return BadRequest(new AuthResult { Errors = new List<string> { "Email needs to be confirmed." }, Result = false });


            var checkUserAndPass = await _userManager.CheckPasswordAsync(existingUser, dto.Password);
            if (!checkUserAndPass)
            return BadRequest(new AuthResult { Errors = new List<string> { "Invalid credentials" }, Result = false });

            var token = GenerateToken(existingUser);
            return Ok(new AuthResult()
            {
                Result = true,
                Token = token
            });
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code)) 
                return BadRequest(
                        new AuthResult
                        {
                            Errors = new List<string> { "Invalid email confirmation url" },
                            Result = false
                        }
                    );
            

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Unable to load user with Id '{userId}'");

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            var result = await _userManager.ConfirmEmailAsync(user, code);

            var status = result.Succeeded ? "Thank you for confirming your email." : "There has been an error confirming your email.";

            return Ok(status);

        }

        private string GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_Scret.SigningKey);

            DateTime hoy = DateTime.Now;
            var fechaExpiracion = hoy.Add(_Scret.expirytTime);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Issuer = _Scret.Issuer,
                Audience = _Scret.Audience,
                Subject = 
                    new ClaimsIdentity(
                        new ClaimsIdentity(
                            new[] {
                                new Claim("Id", user.Id),
                                new Claim(JwtRegisteredClaimNames.Sub,user.Email),
                                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                new Claim(JwtRegisteredClaimNames.Iat, fechaExpiracion.ToUniversalTime().ToString())
                            })),
                NotBefore = hoy,
                Expires = fechaExpiracion,
                SigningCredentials=new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            return jwtTokenHandler.WriteToken(token);
        }

        private async Task SendVerificacionEmail(IdentityUser user)
        {
            var verificacionCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            verificacionCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(verificacionCode));

            //example: https://localhost:8080/api/authentication/verifyEmail/userId=exampleuserId&core=Examplecode
            var callbackUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("ConfirmEmail", controller: "Authentication", new { userId = user.Id, code = verificacionCode })}";

            var emailBody = $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", emailBody);
        }
    }
}
