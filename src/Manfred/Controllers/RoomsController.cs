using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HipChat.Net.Http;
using HipChat.Net.Clients;
using Manfred.Models;
using Manfred.Daos;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class RoomsController : Controller
    {
        private readonly ILogger logger;
        
        public IRoomsClient RoomsClient {get; set;}
        
        public IInstallationsRepository Installations {get; set;}
        
        public ITokensRepository Tokens {get; set;}
        
        public RoomsController(ILoggerFactory loggerFactory, IRoomsClient roomsClient, IInstallationsRepository installations, ITokensRepository tokens)
        {
            logger = loggerFactory.CreateLogger<RoomsController>();
            
            RoomsClient = roomsClient;
            Installations = installations;
            Tokens = tokens;
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
        
        [HttpGet("{groupId}/room/{roomId}/users")]
        public async Task<IActionResult> GetUsers(string groupId, string roomId)
        {           
            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            if(installation == null)
            {
                return NotFound();
            }

            logger.LogInformation($"found token OauthId={installation.OauthId} ExpiresAt={installation.ExpiresAt}");

            var response = await Tokens.Exec(installation, async hipChatClient => {
                return await hipChatClient.Rooms.GetParticipantsAsync(roomId);
            });
                       
            logger.LogInformation($"message response = {response.Code} {response.Model}");
            
            var users = new List<User>();
                
            foreach(HipChat.Net.Models.Response.Mention item in response.Model.Items)
            {
                users.Add(new User { Name = item.MentionName });
            }
   
            return Ok(users);
        }
        
        [HttpPost("{groupId}/room/{roomId}/message")]
        public async Task<IActionResult> SendMessage(string groupId, string roomId, [FromBody] Message message)
        {            
            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            if(installation == null)
            {
                return NotFound();
            }

            logger.LogInformation($"found token OauthId={installation.OauthId} ExpiresAt={installation.ExpiresAt}");

            var response = await Tokens.Exec(installation, async hipChatClient => {
                return await hipChatClient.Rooms.SendMessageAsync(roomId, message.Content);
            });
                       
            logger.LogInformation($"message response = {response.Code} {response.Model}");
            
            return Ok();
        }
    }
}