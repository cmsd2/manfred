namespace Manfred
{
    public class Settings
    {
        public string ApiKey {get; set;}
        public AwsSettings Aws {get; set;}

        public string Jid {get; set;}
    }

    public class AwsSettings
    {
        public string AccessKeyId {get; set;}
        public string SecretAccessKey {get; set;}
    }
}