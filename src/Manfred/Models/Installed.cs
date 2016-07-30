using System;
using System.Net;
using Newtonsoft.Json;

namespace Manfred.Models
{
    [JsonObject]
    public class Installed
    {
        public string CapabilitiesUrl {get; set;}
        public string OauthId {get; set;}
        public string OauthSecret {get; set;}
        public string GroupId {get;set;}
        public string RoomId {get;set;}
    }
}