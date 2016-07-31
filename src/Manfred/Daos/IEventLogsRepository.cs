using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Manfred.Models;

namespace Manfred.Daos
{
    public interface IEventLogsRepository
    {
        Task AddEventLog(EventLog eventLog);

        Task QueryEventLogs(string groupId, string roomId, DateTime startDate, DateTime endDate, Func<List<EventLog>,Task> receiver);
    }
}