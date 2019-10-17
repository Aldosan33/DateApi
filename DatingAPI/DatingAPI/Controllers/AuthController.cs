using DatingAPI.Data;
using DatingAPI.Dto;
using DatingAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DatingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        public AuthController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpPost("{register}")]
        public async Task<IActionResult> Register([FromBody] UserDTO model)
        {
            model.Username = model.Username.ToLower();

            if(await _authRepository.UserExists(model.Username))
            {
                return BadRequest("Username already exists");
            }

            var newUser = new User
            {
                Username = model.Username
            };

            await _authRepository.Register(newUser, model.Password);

            return StatusCode(StatusCodes.Status201Created);
        }
    }
}