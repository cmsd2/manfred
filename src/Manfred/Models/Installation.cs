using System;
using System.Collections.Generic;

namespace Manfred.Models
{
    public class Installation
    {
        public string CapabilitiesUrl {get; set;}
        public string OauthId {get; set;}
        public String OauthSecret {get; set;}
        public string GroupId {get;set;}
        public string RoomId {get;set;}
        public string AccessToken {get; set;}
        public string ExpiresAt {get; set;}
        public List<string> Scopes {get; set;}
    }
}