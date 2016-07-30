using System.Runtime.Serialization;

namespace HipChat.Net.Models.Request
{
  public enum WebhookAuthentication
  {
    [EnumMember(Value = "none")]
    None,
    [EnumMember(Value = "jwt")]
    Jwt
  }
}
