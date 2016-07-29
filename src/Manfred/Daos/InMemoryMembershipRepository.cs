using Manfred.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public class InMemoryMembershipRepository : IMembershipRepository
    {
        private List<string> Memberships {get; set;}
        
        public InMemoryMembershipRepository()
        {
            Memberships = new List<string>();
        }
        
        public Task<List<string>> GetMembershipsAsync()
        {
            return Task.FromResult(Memberships);
        }
        
        public Task AddMembershipAsync(string roomId)
        {
            Memberships.Add(roomId);

            return Task.CompletedTask;
        }
        
        public Task RemoveMembershipAsync(string roomId)
        {
            Memberships.Remove(roomId);

            return Task.CompletedTask;
        }
        
        public async Task<bool> IsMemberAsync(string roomId)
        {
            var memberships = await GetMembershipsAsync();

            return memberships.Contains(roomId);
        }
    }
}