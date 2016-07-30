using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HipChat.Net.Helpers;
using HipChat.Net.Http;
using HipChat.Net.Models.Request;
using HipChat.Net.Models.Response;
using Newtonsoft.Json;

namespace HipChat.Net.Clients
{
  public class RoomsClient : ApiClient, IRoomsClient
  {
    private readonly JsonSerializerSettings _jsonSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient" /> class.
    /// </summary>
    /// <param name="apiConnection">The API connection.</param>
    public RoomsClient(IApiConnection apiConnection) : base(apiConnection)
    {
      _jsonSettings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};
    }

    /// <summary>
    /// Create the room
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="privacy">The privacy.</param>
    /// <param name="guestAccess">if set to <c>true</c> [guest access].</param>
    /// <returns>Task&lt;IResponse&lt;Entity&gt;&gt;.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public async Task<IResponse<bool>> CreateAsync(string name, string owner, RoomPrivacy privacy = RoomPrivacy.Public, bool guestAccess = false)
    {
      Validate.Length(name, 100, "Room Name");
      Validate.Mention(owner, "Owner");
      
      var room = new CreateRoom
      {
        Name = name,
        OwnerUserId = owner,
        GuestAccess = guestAccess,
        Privacy = privacy
      };

      var json = JsonConvert.SerializeObject(room, Formatting.None, _jsonSettings);

      var payload = new StringContent(json, Encoding.UTF8, "application/json");

      var result = await ApiConnection.Client.PostAsync("room", payload);
      var rawResponse = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = rawResponse,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

#pragma warning disable CS1998
    /// <summary>
    /// Update the room
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="privacy">The privacy.</param>
    /// <param name="isArchived">if set to <c>true</c> [is archived].</param>
    /// <param name="guestAccess">if set to <c>true</c> [guest access].</param>
    /// <param name="owner">The owner.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public async Task<IResponse<bool>> UpdateAsync(string name, RoomPrivacy privacy = RoomPrivacy.Public, bool isArchived = false, bool guestAccess = false, string owner = null)
    {
      throw new NotImplementedException();
    }
#pragma warning restore CS1998

    /// <summary>
    /// Deletes the room
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public async Task<IResponse<bool>> DeleteAsync(string room)
    {
      Validate.Length(room, 100, "Room Name");
      
      var result = await ApiConnection.Client.DeleteAsync(string.Format("room/{0}", room));
      var rawResponse = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = rawResponse,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

    /// <summary>
    /// Gell all rooms
    /// </summary>
    /// <returns>Task&lt;RoomItems&lt;Entity&gt;&gt;.</returns>
    public async Task<IResponse<RoomItems<Entity>>> GetAllAsync()
    {
      var rawResponse = await ApiConnection.Client.GetStringAsync("room");
      var context = JsonConvert.DeserializeObject<RoomItems<Entity>>(rawResponse);
      var response = new Response<RoomItems<Entity>>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }

    /// <summary>
    /// Get the room
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Task&lt;Room&gt;.</returns>
    public async Task<IResponse<Room>> GetAsync(string room)
    {
      Validate.Length(room, 100, "Room Id/Name");
      
      var rawResponse = await ApiConnection.Client.GetStringAsync(string.Format("room/{0}", room));
      var context = JsonConvert.DeserializeObject<Room>(rawResponse);
      var response = new Response<Room>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }

    /// <summary>
    /// Get all room members
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Task&lt;IResponse&lt;RoomItems&lt;Mention&gt;&gt;&gt;.</returns>
    public async Task<IResponse<RoomItems<Mention>>> GetMembersAsync(string room)
    {
      Validate.Length(room, 100, "Room Id/Name");

      var rawResponse = await ApiConnection.Client.GetStringAsync(string.Format("room/{0}/member", room));
      var context = JsonConvert.DeserializeObject<RoomItems<Mention>>(rawResponse);
      var response = new Response<RoomItems<Mention>>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }
    
    /// <summary>
    /// Get all room participants
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Task&lt;IResponse&lt;RoomItems&lt;Mention&gt;&gt;&gt;.</returns>
    public async Task<IResponse<RoomItems<Mention>>> GetParticipantsAsync(string room)
    {
      Validate.Length(room, 100, "Room Id/Name");

      var rawResponse = await ApiConnection.Client.GetStringAsync(string.Format("room/{0}/participant", room));
      var context = JsonConvert.DeserializeObject<RoomItems<Mention>>(rawResponse);
      var response = new Response<RoomItems<Mention>>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }
    
    /// <summary>
    /// Send room message.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="message">The message.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    public async Task<IResponse<bool>> SendMessageAsync(string room, string message)
    {
      Validate.NotNull(message, "SendMessage argument");
      Validate.Length(message, 1000, "Notification Message");

      var sendMessage = new SendMessage() { Message = message };
      var json = JsonConvert.SerializeObject(sendMessage, Formatting.None, _jsonSettings);
      var payload = new StringContent(json, Encoding.UTF8, "application/json");

      var result = await ApiConnection.Client.PostAsync(string.Format("room/{0}/message", room), payload);
      var content = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = content,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

    /// <summary>
    /// Send room notification.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="notification">The notification.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    public async Task<IResponse<bool>> SendNotificationAsync(string room, SendNotification notification)
    {
      Validate.NotNull(notification, "SendNotification argument");
      Validate.Length(notification.Message, 10000, "Notification Message");

      var json = JsonConvert.SerializeObject(notification, Formatting.None, _jsonSettings);

      var payload = new StringContent(json, Encoding.UTF8, "application/json");

      var result = await ApiConnection.Client.PostAsync(string.Format("room/{0}/notification", room), payload);
      var content = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = content,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

    /// <summary>
    /// Send room notification
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="message">The message.</param>
    /// <param name="notifyRoom">if set to <c>true</c> [notify room].</param>
    /// <param name="format">The format.</param>
    /// <param name="color">The color.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    public async Task<IResponse<bool>> SendNotificationAsync(string room, string message, bool notifyRoom = true, MessageFormat format = MessageFormat.Html, MessageColor color = MessageColor.Gray)
    {    
      var notification = new SendNotification
      {
        Color = color,
        Message = message,
        Notify = notifyRoom,
        Format = format
      };

      return await SendNotificationAsync(room, notification);
    }

    /// <summary>
    /// Create room webhook
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="hook">The hook.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    public async Task<IResponse<bool>> CreateRoomWebhookAsync(string room, CreateWebhook hook)
    {
      Validate.Length(room, 100, "Room Id/Name");
      Validate.Length(hook.Key, 40, "Webhook Key");
      Validate.NotEmpty(hook.Url, "Webhook URL");

      var json = JsonConvert.SerializeObject(hook, Formatting.None, _jsonSettings);
      var payload = new StringContent(json, Encoding.UTF8, "application/json");

      var result = await ApiConnection.Client.PutAsync(string.Format("room/{0}/extension/webhook/{1}", room, WebUtility.UrlEncode(hook.Key)), payload);
      var content = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = content,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

    /// <summary>
    /// Get all room webhook
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="hookKey">The webhook key.</param>
    /// <returns>Task&lt;IResponse&lt;Room&gt;&gt;.</returns>
    public async Task<IResponse<Webhook>> GetRoomWebhookAsync(string room, string hookKey)
    {
      Validate.Length(room, 100, "Room Id/Name");
      Validate.Length(hookKey, 40, "Webhook Key");

      var rawResponse = await ApiConnection.Client.GetStringAsync(string.Format("room/{0}/extension/webhook/{1}", room, WebUtility.UrlEncode(hookKey)));
      var context = JsonConvert.DeserializeObject<Webhook>(rawResponse);
      var response = new Response<Webhook>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }

    /// <summary>
    /// Create room webhook
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="hook">The hook.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    [Obsolete("Please use Create room webhook instead, which preserves extensions across add-on updates and requires only the view_messages scope")]
    public async Task<IResponse<bool>> CreateWebhookAsync(string room, CreateWebhook hook)
    {
      Validate.Length(room, 100, "Room Id/Name");
      Validate.NotEmpty(hook.Url, "Webhook URL");

      var json = JsonConvert.SerializeObject(hook, Formatting.None, _jsonSettings);
      var payload = new StringContent(json, Encoding.UTF8, "application/json");

      var result = await ApiConnection.Client.PostAsync(string.Format("room/{0}/webhook", room), payload);
      var content = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = content,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

    /// <summary>
    /// Create room webhook
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="url">The URL.</param>
    /// <param name="webhookEvent">The webhook event.</param>
    /// <param name="name">The name.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    [Obsolete("Please use Create room webhook instead, which preserves extensions across add-on updates and requires only the view_messages scope")]
    public async Task<IResponse<bool>> CreateWebhookAsync(string room, Uri url, WebhookEvent webhookEvent, string name = null, string pattern = null)
    {
      var hook = new CreateWebhook
      {
        Url = url.AbsoluteUri,
        Event = webhookEvent,
        Name = string.Format("HipChatDotNet_{0}", name ?? "Default")
      };
      if (pattern != null && (webhookEvent == WebhookEvent.RoomMessage || webhookEvent == WebhookEvent.RoomNotification))
        hook.Pattern = pattern;

// suppress warning of Obselete method usage
#pragma warning disable CS0618
      return await CreateWebhookAsync(room, hook);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Get all room webhooks
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Task&lt;IResponse&lt;Room&gt;&gt;.</returns>
    public async Task<IResponse<RoomItems<Webhook>>> GetWebhooks(string room)
    {
      Validate.Length(room, 100, "Room Id/Name");

      var rawResponse = await ApiConnection.Client.GetStringAsync(string.Format("room/{0}/webhook", room));
      var context = JsonConvert.DeserializeObject<RoomItems<Webhook>>(rawResponse);
      var response = new Response<RoomItems<Webhook>>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }

    /// <summary>
    /// Delete room webhook
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="id">The identifier.</param>
    /// <returns>Task&lt;IResponse&lt;System.Boolean&gt;&gt;.</returns>
    public async Task<IResponse<bool>> DeleteWebhook(string room, string id)
    {
      Validate.Length(room, 100, "Room Id/Name");
      Validate.NotEmpty(id, "Webhook URL");
      
      var result = await ApiConnection.Client.DeleteAsync(string.Format("room/{0}/webhook/{1}", room, id));
      var rawResponse = await result.Content.ReadAsStringAsync();
      var response = new Response<bool>(true)
      {
        Code = result.StatusCode,
        Body = rawResponse,
        ContentType = result.Content.Headers.ContentType.MediaType
      };
      return response;
    }

    /// <summary>
    /// Get recent room history.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Task&lt;IResponse&lt;RoomItems&lt;Message&gt;&gt;&gt;.</returns>
    public async Task<IResponse<RoomItems<Message>>> GetHistoryAsync(string room)
    {
      Validate.Length(room, 100, "Room Id/Name");

      var rawResponse = await ApiConnection.Client.GetStringAsync(string.Format("room/{0}/history", room));
      var context = JsonConvert.DeserializeObject<RoomItems<Message>>(rawResponse);
      var response = new Response<RoomItems<Message>>(context)
      {
        Code = HttpStatusCode.OK,
        Body = rawResponse
      };
      return response;
    }
  }
}
