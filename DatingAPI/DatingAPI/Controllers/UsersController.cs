using AutoMapper;
using DatingAPI.Data;
using DatingAPI.Dto;
using DatingAPI.Helpers;
using DatingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DatingAPI.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;

        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserParams userParams)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var userFromRepo = await _repo.GetUser(currentUserId);

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }

            var users = await _repo.GetUsers(currentUserId, userParams);
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDTO>>(users);

            Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _repo.GetUser(id);
            var userToReturn = _mapper.Map<UserForDetailsDTO>(user);

            return Ok(userToReturn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDTO userFromModel)
        {
            if (!ValidateAuthenticationUserId(id)) return Unauthorized();

            var userFromRepo = await _repo.GetUser(id);
            _mapper.Map(userFromModel, userFromRepo);

            if (await _repo.SaveAll())
            {
                return NoContent();
            }
            else
            {
                throw new Exception($"Updating user {id} failed on save");
            }
        }
            
        [HttpPost("{id}/likes/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            if (!ValidateAuthenticationUserId(id)) return Unauthorized();

            var like = await _repo.GetLike(id, recipientId);

            if (like != null) return BadRequest("You already liked this user");
            if (await _repo.GetUser(recipientId) == null) return NotFound();

            like = new Like
            {
                LikerId = id,
                LikeeId = recipientId
            };

            _repo.Add(like);

            if(await _repo.SaveAll())
            {
                return Ok();
            }

            return BadRequest("Failed to like User");
        }
    }
}