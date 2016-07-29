using Manfred.Models;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public class InMemoryMembershipRepository : IMembershipRepository
    {
        private List<Room> Memberships {get; set;}
        
        public InMemoryMembershipRepository()
        {
            Memberships = new List<Room>();
        }
        
        public List<Room> GetMemberships()
        {
            return Memberships;
        }
        
        public void AddMembership(Room m)
        {
            Memberships.Add(m);
        }
        
        public void RemoveMembership(Room m)
        {
            Memberships.Remove(m);
        }
        
        public Room FindMembershipByRoomId(string roomId)
        {
            foreach(Room m in Memberships)
            {
                if(m.RoomId == roomId)
                {
                    return m;
                }
            }
            
            return null;
        }
    }
}