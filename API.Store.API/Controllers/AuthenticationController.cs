using API.Store.API.Configurations;
using API.Store.Data;
using API.Store.Shared;
using API.Store.Shared.Auth;
using API.Store.Shared.Common;
using API.Store.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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
        private readonly APIStoreContext _apiContext;
        private readonly TokenValidationParameters _validationParametersToken;
        public AuthenticationController(
                UserManager<IdentityUser> userManager, 
                IOptions<Security_Scret> scret,
                IEmailSender emailSender,
                APIStoreContext aPIStore,
                TokenValidationParameters validationParametersToken)
        {
            _userManager = userManager;
            _Scret = scret.Value;
            _emailSender = emailSender;
            _apiContext = aPIStore;
            _validationParametersToken = validationParametersToken;

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
            return Ok(token);
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> refreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid) return BadRequest(new AuthResult
            {
                Errors = new List<string> { "Invalid parameters"},
                Result = false
            });

            var results = VerifyAndGenerateTokenAsyn(tokenRequest);

            if(results == null) return BadRequest(new AuthResult
            {
                Errors = new List<string> { "Invalid token" },
                Result = false
            });

            return Ok(results);

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

        private async Task<AuthResult> GenerateToken(IdentityUser user)
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

            var jwt = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                Token = RandomGenerator.GenerateRandomString(24),
                AddedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(1),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id
            };

            await _apiContext.RefreshTokens.AddAsync(refreshToken);
            await _apiContext.SaveChangesAsync();

            return new AuthResult
            {
                Token = jwt,
                RefreshToken = refreshToken.Token,
                Result = true
            };

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
    
        private async Task<AuthResult> VerifyAndGenerateTokenAsyn(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            try
            {
                _validationParametersToken.ValidateLifetime = true;

                var tokenBegindVerified = jwtTokenHandler.ValidateToken(tokenRequest.Token, _validationParametersToken, out var validatedtoken);
                if(validatedtoken is JwtSecurityToken securityToken)
                {
                    var result = securityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if(!result || tokenBegindVerified == null) {
                        throw new Exception("Invalid Token");
                    }
                }

                /*
                 Error al implementar esta validacion, fechas incorrectas. Corregir.
                 */
                //var utcExpiryDate = long.Parse(tokenBegindVerified.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp).Value);
                //var expireDate = DateTimeOffset.FromUnixTimeMilliseconds(utcExpiryDate).UtcDateTime;
                //if (expireDate < DateTime.UtcNow) throw new Exception("Token Expired");

                var storeToken = await _apiContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenRequest.RefreshToken);

                if (storeToken == null) throw new Exception("Invalid Token");

                if(storeToken.IsUsed || storeToken.IsRevoked) throw new Exception("Invalid Token");

                var jti = tokenBegindVerified.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

                if(jti != storeToken.JwtId) throw new Exception("Invalid Token");

                if(storeToken.ExpiryDate < DateTime.UtcNow) throw new Exception("Token Expired");

                storeToken.IsUsed = true;
                _apiContext.RefreshTokens.Update(storeToken);
                await _apiContext.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storeToken.UserId);

                return await GenerateToken(dbUser);

            }
            catch(Exception ex)
            {
                var message = ex.Message == "Invalid Token" || ex.Message == "Token Expired" ? ex.Message : "Internal server error";
                
                return new AuthResult() { Result=false, Errors = new List<string> { message} };
            }

        }
    }
}
