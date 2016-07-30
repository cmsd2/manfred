using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.Runtime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DataModel;
using Manfred.Models;

namespace Manfred.Daos
{

    [DynamoDBTable("Memberships")]
    public class DynamoMemberships
    {
        [DynamoDBHashKey]
        public string Jid { get; set; }
        public List<string> Rooms { get; set; }
    }

    public class DynamoMembershipRepository : IMembershipRepository
    {
        private readonly ILogger logger;

        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "Memberships";

        public DynamoMembershipRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<DynamoMembershipRepository>();
            Settings = settings.Value;

            AWSCredentials credentials = new BasicAWSCredentials(settings.Value.Aws.AccessKeyId, settings.Value.Aws.SecretAccessKey);
            Client = new AmazonDynamoDBClient(credentials);
            Context = new DynamoDBContext(Client);

            CreateTable();
        }

        public void CreateTable()
        {
            int sleepTime = 1;

            while (!TableExists())
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
                            }
                        },
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                            {
                                AttributeName = "Jid",
                                // "HASH" = hash key, "RANGE" = range key.
                                KeyType = "HASH"
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

        public bool TableExists()
        {
            var status = "";

            try
            {
                logger.LogInformation($"checking table {TableName}");

                var response = Client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = this.TableName
                }).Result;

                logger.LogInformation("Table = {0}, Status = {1}",
                    response.Table.TableName,
                    response.Table.TableStatus);

                status = response.Table.TableStatus;
            }
            catch (ResourceNotFoundException e)
            {
                logger.LogInformation("DescribeTable = {0}, Error = {1}", this.TableName, e.Message);
                // DescribeTable is eventually consistent. So you might
                //   get resource not found. 
                return false;
            }

            return true;
        }

        public async Task<List<string>> GetMembershipsAsync()
        {
            var memberships = await Context.LoadAsync<DynamoMemberships>(Settings.Jid);

            return memberships.Rooms;
        }

        public async Task AddMembershipAsync(string roomId)
        {
            var request = new UpdateItemRequest
            {
                TableName = this.TableName,
                Key = new Dictionary<string, AttributeValue>() { { "Jid", new AttributeValue { S = Settings.Jid } } },
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    {"#R", "Rooms"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":room", new AttributeValue { SS = {roomId}}}
                },

                UpdateExpression = "ADD #R :room"
            };

            await Client.UpdateItemAsync(request);
        }

        public async Task RemoveMembershipAsync(string roomId)
        {
            var request = new UpdateItemRequest
            {
                TableName = this.TableName,
                Key = new Dictionary<string, AttributeValue>() { { "Jid", new AttributeValue { S = Settings.Jid } } },
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    {"#R", "Rooms"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":room", new AttributeValue { SS = {roomId}}}
                },
                UpdateExpression = "DELETE #R :room"
            };

            await Client.UpdateItemAsync(request);
        }

        public async Task<bool> IsMemberAsync(string roomId)
        {
            var memberships = await GetMembershipsAsync();

            return memberships.Contains(roomId);
        }
    }
}