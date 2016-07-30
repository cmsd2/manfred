using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HipChat.Net.Models.Request
{
  [JsonObject]
  public class CreateWebhook
  {
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("pattern")]
    public string Pattern { get; set; }

    [JsonProperty("authentication"), JsonConverter(typeof(StringEnumConverter))]
    public WebhookAuthentication Authentication { get; set; }

    [JsonProperty("event"), JsonConverter(typeof(StringEnumConverter))]
    public WebhookEvent Event { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }

    public CreateWebhook()
    {
      Event = WebhookEvent.RoomMessage;
      Name = "HipChatDotNet_Default";
    }
  }
}
