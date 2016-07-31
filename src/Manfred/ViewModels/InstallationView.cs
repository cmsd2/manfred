using System;
using System.Collections.Generic;
using Manfred.Models;

namespace Manfred.ViewModels
{
    public class InstallationView
    {
        public string CapabilitiesUrl {get; set;}
        public string OauthId {get; set;}
        public string GroupId {get;set;}
        public string RoomId {get;set;}

        public InstallationView(Installation installation)
        {
            CapabilitiesUrl = installation.CapabilitiesUrl;
            OauthId = installation.CapabilitiesUrl;
            GroupId = installation.GroupId;
            RoomId = installation.RoomId;
        }
    }
}