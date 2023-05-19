using API.Store.API.Configurations;
using API.Store.Shared.Auth;
using API.Store.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Store.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Security_Scret _Scret;
        public AuthenticationController(UserManager<IdentityUser> userManager, IOptions<Security_Scret> scret)
        {
            _userManager = userManager;
            _Scret = scret.Value;
            
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
                UserName = request.Name
            };

            var isCreated = await _userManager.CreateAsync(user, request.Password);

            if (isCreated.Succeeded) 
            {
                var token = GenerateToken(user);
                return Ok(new AuthResult()
                {
                    Result = true,
                    Token = token
                });
            }

            var errors = new List<string>();
            foreach (var err in isCreated.Errors)
                errors.Add(err.Description);

            return BadRequest(new AuthResult()
            {
                Result = false,
                Errors = errors.Count == 0 ? errors : new List<string> {"User couldn't be created"}
            });

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginResquestDtos dto)
        {
            if (!ModelState.IsValid) return BadRequest();


            var existingUser =  await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser == null)
            {
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid payload" },
                    Result = false
                });
            }

            var checkUserAndPass = await _userManager.CheckPasswordAsync(existingUser, dto.Password);
            if (!checkUserAndPass)
            {
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid credentials" },
                    Result = false
                });
            }

            var token = GenerateToken(existingUser);
            return Ok(new AuthResult()
            {
                Result = true,
                Token = token
            });
        }

        private string GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_Scret.SigningKey);

            DateTime hoy = DateTime.Now;
            var fechaExpiracion = hoy.Add(TimeSpan.FromMinutes(30));

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

    }
}
