using Manfred.Models;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public interface IMembershipRepository
    {
        List<Room> GetMemberships();
        
        void AddMembership(Room m);
        
        void RemoveMembership(Room m);
        
        Room FindMembershipByRoomId(string roomId);
    }
}