using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc;
using Manfred.Models;
using Manfred.Daos;
using HipChat.Net.Clients;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class MembershipsController : Controller
    {
        private IMembershipRepository Memberships {get; set;}

        private IRoomsClient Client {get; set;}
        
        public MembershipsController(IMembershipRepository memberships, IRoomsClient client)
        {
            Memberships = memberships;
            Client = client;
        }
        
        [HttpGet]
        public async Task<List<string>> Index()
        {
            return await Memberships.GetMembershipsAsync();
        }
           
        [HttpPut]
        public async Task<IActionResult> Create([FromBody] Room m)
        {
            if(m.RoomId == null)
            {
                return BadRequest();
            }
            
            await Memberships.AddMembershipAsync(m.RoomId);
            
            return Ok(m);
        }
        
        [HttpDelete("{roomId}")]
        public async Task<IActionResult> Delete(string roomId)
        {
            var m = await Memberships.IsMemberAsync(roomId);
            
            if(m)
            {
                await Memberships.RemoveMembershipAsync(roomId);
                
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpGet("{roomId}")]
        public async Task<IActionResult> Get(string roomId)
        {
            var m = await Memberships.IsMemberAsync(roomId);
            
            if(m)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
    }
}