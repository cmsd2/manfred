using System;

namespace Manfred.Models
{
    public class Oauth
    {
        public string OauthId {get; set;}

        public String OauthSecret {get; set;}

        public string GroupId { get; set; }

        public string RoomId {get; set;}

        public string CapabilitiesUrl {get; set;}
    }
}