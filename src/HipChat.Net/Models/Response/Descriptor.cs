using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HipChat.Net.Models.Response
{
    public class Descriptor
    {
        public string Name {get; set;}
        public string Key {get; set;}
        public string Description {get; set;}
        public Links Links {get; set;}
        public Capabilities Capabilities {get; set;}
    }

    public class Capabilities
    {
        public Installable Installable {get; set;}
        public Configurable Configurable {get; set;}
        public HipchatApiConsumer HipchatApiConsumer {get; set;}
        public List<Webhook> Webhook {get; set;}
    }

    public class Installable {
        public bool AllowGlobal {get; set;}
        public bool AllowRoom {get; set;}
        public string CallbackUrl {get; set;}
        public string UpdateCallbackUrl {get; set;}
    }

    public class Configurable {
        public string Url {get; set;}
    }

    public class HipchatApiConsumer {
        public string FromName {get; set;}
        public List<string> Scopes {get; set;}
        public Avatar Avatar {get; set;}
    }

    public class Avatar {
        public string Url {get; set;}
        [JsonProperty("url@2x")]
        public string Url2x {get; set;}
    }
}