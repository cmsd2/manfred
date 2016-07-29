using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using HipChat.Net.Http;
using HipChat.Net.Clients;
using Manfred.Models;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class RoomsController : Controller
    {
        private readonly ILogger logger;
        
        public IRoomsClient RoomsClient {get; set;}
        
        public RoomsController(IRoomsClient roomsClient, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<RoomsController>();
            
            RoomsClient = roomsClient;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetRooms()
        {
            var response = await RoomsClient.GetAllAsync();

            var rooms = new List<Room>();
            
            foreach(HipChat.Net.Models.Response.Entity item in response.Model.Items)
            {
                rooms.Add(new Room() {
                   RoomName = item.Name,
                   RoomId = item.Id,
                   RoomLink = item.Links.Self
                });
            }
            
            return Ok(rooms);
        }
        
        [HttpGet("{roomId}/users")]
        public async Task<IActionResult> GetUsers(string roomId)
        {
            var response = await RoomsClient.GetParticipantsAsync(roomId);
            
            var users = new List<User>();
                
            foreach(HipChat.Net.Models.Response.Mention item in response.Model.Items)
            {
                users.Add(new User { Name = item.MentionName });
            }
   
            return Ok(users);
        }
        
        [HttpPost("{roomId}/message")]
        public async Task<IActionResult> SendMessage(string roomId, [FromBody] Message message)
        {
            var response = await RoomsClient.SendMessageAsync(roomId, message.Content);
            
            logger.LogInformation($"message response = {response.Code} {response.Model}");
            
            return Ok();
        }
    }
}