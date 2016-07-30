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
        [DynamoDBTable("WebHooks")]
        public class WebHooks
        {
            [DynamoDBHashKey]
            public string Jid { get; set; }

            [DynamoDBRangeKey]
            public string RoomId {get; set;}

            [DynamoDBRangeKey]
            public string WebHookKey {get; set;}
        }
    }

    public class WebHooksRepository : IWebHookRepository
    {
        private readonly ILogger logger;

        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "WebHooks";

        private DynamoUtils dynamoUtils;

        public WebHooksRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<WebHooksRepository>();
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
                                AttributeName = "Jid",
                                // "S" = string, "N" = number, and so on.
                                AttributeType = "S"
                            },
                            new AttributeDefinition
                            {
                                AttributeName = "RoomId",
                                AttributeType = "S"
                            },
                            new AttributeDefinition
                            {
                                AttributeName = "WebHookKey",
                                AttributeType = "S"
                            }
                        },
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                            {
                                AttributeName = "Jid",
                                // "HASH" = hash key, "RANGE" = range key.
                                KeyType = "HASH"
                            },
                            new KeySchemaElement
                            {
                                AttributeName = "RoomId",
                                KeyType = "RANGE"
                            },
                            new  KeySchemaElement
                            {
                                AttributeName = "WebHookKey",
                                KeyType = "RANGE"
                            }
                        },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 1,
                            WriteCapacityUnits = 1
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

        public async Task<List<WebHook>> GetWebHooksAsync(string roomId = null, string webhookKey = null)
        {
            var webhooksTable = Table.LoadTable(Client, TableName);

            var webhooks = new List<WebHook>();

            var queryFilter = new QueryFilter();

            queryFilter.AddCondition("Jid", QueryOperator.Equal, Settings.Jid);

            if(roomId != null) 
            {
                queryFilter.AddCondition("RoomId", QueryOperator.Equal, roomId);
                if(webhookKey != null)
                {
                    queryFilter.AddCondition("WebHookKey", QueryOperator.Equal, webhookKey);
                }
            }

            var search = webhooksTable.Query(queryFilter);

            while(!search.IsDone)
            {
                List<Document> rows = await search.GetNextSetAsync();

                foreach(Document row in rows) {
                    webhooks.Add(new WebHook {
                        RoomId = row["RoomId"].AsString(),
                        WebHookKey = row["WebHookKey"].AsString()
                    });
                }
            }

            return webhooks;
        }

        public async Task AddWebHookAsync(WebHook webhook)
        {
            var row = new Tables.WebHooks {
                Jid = Settings.Jid,
                RoomId = webhook.RoomId,
                WebHookKey = webhook.WebHookKey
            };

            await Context.SaveAsync(row);
        }

        public async Task RemoveWebHookAsync(string roomId, string webhookKey)
        {
            await Context.DeleteAsync(new Tables.WebHooks {
                Jid = Settings.Jid,
                RoomId = roomId,
                WebHookKey = webhookKey
            });
        }
    }
}