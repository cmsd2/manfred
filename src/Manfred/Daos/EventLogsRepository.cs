using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.Runtime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Manfred.Models;

namespace Manfred.Daos
{
    namespace Tables
    {
        [DynamoDBTable("EventLogs")]
        public class EventLogs
        {
            [DynamoDBHashKey]
            public string GroupIdRoomIdDayOfMonth { get; set; }

            [DynamoDBRangeKey]
            public string TimestampUuid {get; set;}

            public string GroupId {get; set;}
            public string RoomId {get; set;}
            public string Content {get; set;}

            public EventLogs()
            {
            }

            public EventLogs(Manfred.Models.EventLog e)
            {
                GroupId = e.GroupId;
                RoomId = e.RoomId;
                GroupIdRoomIdDayOfMonth = BuildHashKey(e.GroupId, e.RoomId, e.Timestamp.Day);
                TimestampUuid = BuildRangeKey(e.Timestamp, e.Guid.ToString());
                Content = e.Content;
            }

            public EventLog BuildEventLog()
            {
                return new EventLog {
                    GroupId = GroupId,
                    RoomId = RoomId,
                    Timestamp = GetDateTime(),
                    Guid = GetGuid(),
                    Content = Content
                };
            }

            public DateTime GetDateTime()
            {
                return GetDateTimeFromRangeKey(TimestampUuid);
            }

            public Guid GetGuid()
            {
                return GetGuidFromRangeKey(TimestampUuid);
            }

            public static DateTime GetDateTimeFromRangeKey(string rangeKey)
            {
                return DateTimeUTils.FromIsoString(rangeKey.Split('_')[0]);
            }

            public static Guid GetGuidFromRangeKey(string rangeKey)
            {
                return Guid.Parse(rangeKey.Split('_')[1]);
            }

            public static string BuildHashKey(string groupId, string roomId, int dayOfMonth)
            {
                return $"{groupId}_{roomId}_{dayOfMonth}";
            }

            public static string BuildRangeKey(DateTime secondsSinceEpoch, string guid)
            {
                return $"{DateTimeUTils.ToIsoString(secondsSinceEpoch)}_{guid}";
            }
        }
    }

    public class EventLogsRepository : IEventLogsRepository
    {
        private readonly ILogger logger;

        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "EventLogs";

        private DynamoUtils dynamoUtils;

        public EventLogsRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<EventLogsRepository>();
            Settings = settings.Value;

            AWSCredentials credentials = new BasicAWSCredentials(settings.Value.Aws.AccessKeyId, settings.Value.Aws.SecretAccessKey);
            Client = new AmazonDynamoDBClient(credentials);
            Context = new DynamoDBContext(Client);
            dynamoUtils = new DynamoUtils(loggerFactory, Client);

            CreateTable();
        }

        public void CreateTable()
        {
            int sleepTime = 1;

            while (!dynamoUtils.TableExists(TableName))
            {
                try
                {
                    var request = new CreateTableRequest
                    {
                        TableName = this.TableName,
                        AttributeDefinitions = new List<AttributeDefinition>
                        {
                            new AttributeDefinition
                            {
                                AttributeName = "GroupIdRoomIdDayOfMonth",
                                // "S" = string, "N" = number, and so on.
                                AttributeType = "S"
                            },
                            new AttributeDefinition
                            {
                                AttributeName = "TimestampUuid",
                                AttributeType = "S"
                            }
                        },
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                            {
                                AttributeName = "GroupIdRoomIdDayOfMonth",
                                // "HASH" = hash key, "RANGE" = range key.
                                KeyType = "HASH"
                            },
                            new KeySchemaElement
                            {
                                AttributeName = "TimestampUuid",
                                KeyType = "RANGE"
                            }
                        },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 2,
                            WriteCapacityUnits = 4
                        },
                    };

                    logger.LogInformation($"creating table {TableName}");
                    var response = Client.CreateTableAsync(request).Result;

                    logger.LogInformation("Table created with request ID: " +
                        response.ResponseMetadata.RequestId);
                }
                catch (ResourceInUseException e)
                {
                    logger.LogInformation($"CreateTable = {this.TableName}, Error = {e.Message}");
                }

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(sleepTime++));
            }
        }

        public async Task AddEventLog(EventLog eventLog)
        {
            var row = new Tables.EventLogs(eventLog);

            await Context.SaveAsync(row);
        }

        public async Task QueryEventLogs(string groupId, string roomId, DateTime startDate, DateTime endDate, Func<List<EventLog>,Task> receiver)
        {
            for(int i = 0; i < 31; i++)
            {
                DateTime addedDate = startDate.AddDays(i);

                if(addedDate.Date > endDate)
                {
                    break;
                }

                await QueryEventLogs(groupId, roomId, addedDate.Day, startDate, endDate, receiver);
            }
        }

        async Task QueryEventLogs(string groupId, string roomId, int dayOfMonth, DateTime startDate, DateTime endDate, Func<List<EventLog>,Task> receiver)
        {
            var eventLogsTable = Table.LoadTable(Client, TableName);

            var webhooks = new List<EventLog>();

            var queryFilter = new QueryFilter();

            queryFilter.AddCondition("GroupIdRoomIdDayOfMonth", QueryOperator.Equal, Tables.EventLogs.BuildHashKey(groupId, roomId, dayOfMonth));

            queryFilter.AddCondition("TimestampUuid", QueryOperator.Between,
                    Tables.EventLogs.BuildRangeKey(startDate, ""),
                    Tables.EventLogs.BuildRangeKey(endDate, ""));

            var search = eventLogsTable.Query(queryFilter);

            while(!search.IsDone)
            {
                List<Document> rows = await search.GetNextSetAsync();

                var eventLogs = new List<EventLog>();

                foreach(Document row in rows) {
                    var eventLog = new EventLog {
                        Timestamp = Tables.EventLogs.GetDateTimeFromRangeKey(row["TimestampUuid"].AsString()),
                        Guid = Tables.EventLogs.GetGuidFromRangeKey(row["TimestampUuid"].AsString()),
                        GroupId = row["GroupId"].AsString(),
                        RoomId = row["RoomId"].AsString(),
                        Content = row["Content"].AsString()
                    };

                    eventLogs.Add(eventLog);
                }

                await receiver(eventLogs);
            }
        }
    }
}