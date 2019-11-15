using AutoMapper;
using DatingAPI.Data;
using DatingAPI.Dto;
using DatingAPI.Helpers;
using DatingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DatingAPI.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : BaseController
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
            if (!ValidateAuthenticationUserId(userId)) return Unauthorized();

            var messageRepo = await _repo.GetMessage(id);

            if (messageRepo == null) return NotFound();

            return Ok(messageRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery] MessageParams messageParams)
        {
            if (!ValidateAuthenticationUserId(userId)) return Unauthorized();

            messageParams.UserId = userId;

            var messagesFromRepo = await _repo.GetMessageForUser(messageParams);

            var messages = _mapper.Map<IEnumerable<MessageForReturnDTO>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize,
                                   messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            return Ok(messages);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (!ValidateAuthenticationUserId(userId)) return Unauthorized();

            var messagesFromRepo = await _repo.GetMessageThread(userId, recipientId);

            var messageThread = _mapper.Map<IEnumerable<MessageForReturnDTO>>(messagesFromRepo);

            return Ok(messageThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDTO model)
        {
            if (!ValidateAuthenticationUserId(userId)) return Unauthorized();

            var sender = await _repo.GetUser(userId);
            model.SenderId = sender.Id;
            model.Sender = sender;

            var recipient = await _repo.GetUser(model.RecipientId);
            model.RecipientId = recipient.Id;
            model.Recipient = recipient;

            if (recipient == null) return BadRequest("Could not find specified user");

            var message = _mapper.Map<Message>(model);

            _repo.Add(message);

            if (await _repo.SaveAll())
            {
                var messageToReturn = _mapper.Map<MessageForReturnDTO>(message);
                return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);
            }

            return StatusCode(StatusCodes.Status502BadGateway, "There was a problem creating the message");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int userId, int id)
        {
            if (!ValidateAuthenticationUserId(userId)) return Unauthorized();

            var messageFromRepo = await _repo.GetMessage(id);

            if (messageFromRepo == null) return NotFound();

            if(messageFromRepo.SenderId == userId) messageFromRepo.SenderDeleted = true;

            if(messageFromRepo.RecipientId == userId) messageFromRepo.RecipientDeleted = true;

            if(messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
            {
                _repo.Delete(messageFromRepo);
            }

            if(await _repo.SaveAll())
            {
                return NoContent();
            }

            return StatusCode(StatusCodes.Status502BadGateway, "There was a problem deleting the message");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            if (!ValidateAuthenticationUserId(userId)) return Unauthorized();

            var message = await _repo.GetMessage(id);

            if(message.RecipientId != userId) return Unauthorized();

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            await _repo.SaveAll();

            return NoContent();
        }
    }
}