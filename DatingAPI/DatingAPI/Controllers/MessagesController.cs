using AutoMapper;
using DatingAPI.Data;
using DatingAPI.Dto;
using DatingAPI.Helpers;
using DatingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DatingAPI.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) return Unauthorized();

            var messageRepo = await _repo.GetMessage(id);

            if (messageRepo == null) return NotFound();

            return Ok(messageRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery] MessageParams messageParams)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) return Unauthorized();

            messageParams.UserId = userId;

            var messagesFromRepo = await _repo.GetMessageForUser(messageParams);

            var messages = _mapper.Map<IEnumerable<MessageForReturnDTO>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize,
                                   messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            return Ok(messages);
        }            

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDTO model)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) return Unauthorized();

            model.SenderId = userId;
            var recipient = await _repo.GetUser(model.RecipientId);

            if (recipient == null) return BadRequest("Could not find specified user");

            var message = _mapper.Map<Message>(model);

            var messageToReturn = _mapper.Map<MessageForCreationDTO>(message);

            _repo.Add(message);

            if (await _repo.SaveAll())
            {
                return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);
            }

            throw new Exception("There was a problem with ths saving");
        }
    }
}