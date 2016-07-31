using Manfred.Models;

namespace Manfred.ViewModels
{
    public class OAuthView
    {
        public string OauthId {get; set;}

        public string GroupId { get; set; }

        public string RoomId {get; set;}

        public string CapabilitiesUrl {get; set;}

        public OAuthView(Oauth oauth)
        {
            OauthId = oauth.OauthId;
            GroupId = oauth.GroupId;
            RoomId = oauth.RoomId;
            CapabilitiesUrl = oauth.CapabilitiesUrl;
        }
    }
}