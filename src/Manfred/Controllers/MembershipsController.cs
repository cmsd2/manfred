using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc;
using Manfred.Models;
using Manfred.Daos;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class MembershipsController : Controller
    {
        private IMembershipRepository Memberships {get; set;}
        
        public MembershipsController(IMembershipRepository memberships)
        {
            Memberships = memberships;
        }
        
        [HttpGet]
        public List<Room> Index()
        {
            return Memberships.GetMemberships();
        }
           
        [HttpPut]
        public IActionResult Create([FromBody] Room m)
        {
            if(m.RoomId == null || m.RoomName == null)
            {
                return BadRequest();
            }
            
            Memberships.AddMembership(m);
            
            return Ok(m);
        }
        
        [HttpDelete("{roomId}")]
        public IActionResult Delete(string roomId)
        {
            var m = Memberships.FindMembershipByRoomId(roomId);
            
            if(m != null)
            {
                Memberships.RemoveMembership(m);
                
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpGet("{roomId}")]
        public IActionResult Get(string roomId)
        {
            var m = Memberships.FindMembershipByRoomId(roomId);
            
            if(m != null)
            {
                return Ok(m);
            }
            else
            {
                return NotFound();
            }
        }
    }
}