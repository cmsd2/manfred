using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Manfred.Models;

namespace Manfred.Daos
{
    public class InMemoryInstallationsRepository : IInstallationsRepository
    {
        private List<Installation> installations = new List<Installation>();

        public Task CreateInstallationAsync(Installation installation)
        {
            installations.Add(installation);

            return Task.CompletedTask;
        }

        public Task<Installation> GetInstallationAsync(string groupId, string roomId = null)
        {
            return Task.FromResult(installations.Find(i => i.GroupId == groupId && ((roomId == null && i.RoomId == null) || i.RoomId == roomId)));
        }

        public Task RemoveInstallationAsync(string groupId, string roomId = null)
        {
            installations.RemoveAll(i => i.GroupId == groupId && ((roomId == null && i.RoomId == null) || i.RoomId == roomId));

            return Task.CompletedTask;
        }

        public Task<Installation> GetInstallationByOauthIdAsync(string oauthId)
        {
            return Task.FromResult(installations.Find(i => i.OauthId == oauthId));
        }

        public Task RemoveInstallationByOauthAsync(string oauthId)
        {
            installations.RemoveAll(i => i.OauthId == oauthId);

            return Task.CompletedTask;
        }
    }
}