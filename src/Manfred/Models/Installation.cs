using System;
using System.Collections.Generic;

namespace Manfred.Models
{
    public class Installation : IToken
    {
        public string CapabilitiesUrl {get; set;}
        public string OauthId {get; set;}
        public String OauthSecret {get; set;}
        public string GroupId {get;set;}
        public string RoomId {get;set;}
        public string AccessToken {get; set;}
        public string ExpiresAt {get; set;}
        public List<string> Scopes {get; set;}

        public Installation()
        {
        }

        public Installation(Installed installed)
        {
            CapabilitiesUrl = installed.CapabilitiesUrl;
            OauthId = installed.OauthId;
            OauthSecret = installed.OauthSecret;
            GroupId = installed.GroupId;
            RoomId = installed.RoomId;
        }
    }
}