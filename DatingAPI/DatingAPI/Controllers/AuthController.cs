using AutoMapper;
using DatingAPI.Data;
using DatingAPI.Dto;
using DatingAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DatingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController(IAuthRepository authRepository, IConfiguration config, IMapper mapper)
        {
            _authRepository = authRepository;
            _config = config;
            _mapper = mapper;
        }

        [HttpPost("{register}")]
        public async Task<IActionResult> Register([FromBody] UserForRegistrationDTO model)
        {
            model.Username = model.Username.ToLower();

            if (await _authRepository.UserExists(model.Username))
            {
                return BadRequest("Username already exists");
            }

            var newUser = _mapper.Map<User>(model);

            await _authRepository.Register(newUser, model.Password);

            var userToReturn = _mapper.Map<UserForDetailsDTO>(newUser);

            return CreatedAtRoute("GetUser", new { controller = "Users", id = newUser.Id }, userToReturn);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserForLoginDTO model)
        {
            var userFromRepo = await _authRepository.Login(model.Username.ToLower(), model.Password);

            if (userFromRepo == null)
            {
                return Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            var user = _mapper.Map<UserForListDTO>(userFromRepo);

            return Ok(new { token = tokenHandler.WriteToken(token), user });
        }
    }
}