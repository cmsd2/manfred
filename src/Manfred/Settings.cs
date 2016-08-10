namespace Manfred
{
    public class Settings
    {
        public string ApiKey {get; set;}
        public AwsSettings Aws {get; set;}

        public string Jid {get; set;}

        public string Url {get; set;}
        
        public string TableNamePrefix {get; set;}

        public HipChat HipChat {get; set;}
    }

    public class AwsSettings
    {
        public string AccessKeyId {get; set;}
        public string SecretAccessKey {get; set;}
    }

    public class HipChat
    {
        public string Name {get; set;}
        public string Key {get; set;}
        public string Description {get; set;}
    }
}