using System;

namespace Manfred.Models
{
    public class EventLog
    {
        public string GroupId {get; set;}
        public string RoomId {get; set;}
        public DateTime Timestamp {get; set;} = DateTime.UtcNow;
        public Guid Guid {get; set;} = Guid.NewGuid();
        public string Content {get; set;}
    }
}