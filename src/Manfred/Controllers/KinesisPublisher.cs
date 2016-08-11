using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Manfred;
using Manfred.Models;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Newtonsoft.Json;

namespace Manfred.Controllers
{
    public class KinesisPublisher : IEventHandler
    {
        private ILogger logger;
        private Settings Settings {get; set;}
        private AmazonKinesisClient kinesisClient;
        
        public KinesisPublisher(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<KinesisPublisher>();
            Settings = settings.Value;
            
            logger.LogInformation($"creating kinesis client for stream {Settings.KinesisStreamName}");
            
            AmazonKinesisConfig config = new AmazonKinesisConfig();
            config.RegionEndpoint = Amazon.RegionEndpoint.EUWest1;
            kinesisClient = new AmazonKinesisClient(config);
        }
        
        public async Task HandleEvent(EventLog e)
        {
            logger.LogInformation($"sending event GroupId={e.GroupId} RoomId={e.RoomId} Timestamp={e.Timestamp} to Kinesis");
            
            string dataAsJson = JsonConvert.SerializeObject(e);
            byte[] dataAsBytes = Encoding.UTF8.GetBytes(dataAsJson);
            using (MemoryStream memoryStream = new MemoryStream(dataAsBytes))
            {
                try
                {                       
                    PutRecordRequest requestRecord = new PutRecordRequest();
                    requestRecord.StreamName = Settings.KinesisStreamName;
                    requestRecord.PartitionKey = $"group:{e.GroupId}_room:{e.RoomId}";
                    requestRecord.Data = memoryStream;
    
                    PutRecordResponse responseRecord = await kinesisClient.PutRecordAsync(requestRecord);
                    logger.LogInformation($"Successfully sent GroupId={e.GroupId} RoomId={e.RoomId} Timestamp={e.Timestamp} to Kinesis. Sequence number={responseRecord.SequenceNumber}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Failed to send record GroupId={e.GroupId} RoomId={e.RoomId} Timestamp={e.Timestamp} to Kinesis. Exception: {ex.Message}");
                }
            }
        }
    }
}