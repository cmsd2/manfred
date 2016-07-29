using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HipChat.Net.Models.Request
{
 [JsonObject]
  public class SendMessage
  {
    [JsonProperty("message")]
    public string Message { get; set; }
  }
}
