using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HipChat.Net.Models.Response
{
  public class WebhookPayloadConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
        return typeof(WebhookPayload).GetTypeInfo().IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, 
        Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject item = JObject.Load(reader);
        var eventName = item["event"].Value<string>() ?? "";
        switch(eventName)
        {
            case "room_message":
                return item.ToObject<RoomMessage>();
            case "room_notification":
                return item.ToObject<RoomNotification>();
            default:
                throw new JsonSerializationException($"unsupported webhook payload item type {eventName}");
        }
    }

    public override void WriteJson(JsonWriter writer, 
        object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
  }
}