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
}