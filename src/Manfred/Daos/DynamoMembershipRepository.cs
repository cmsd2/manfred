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
    namespace Tables
    {
        [DynamoDBTable("Memberships")]
        public class Memberships
        {
            [DynamoDBHashKey]
            public string Jid { get; set; }
            public List<string> Rooms { get; set; } = new List<string>();
        }
    }

    public class DynamoMembershipRepository : IMembershipRepository
    {
        private readonly ILogger logger;

        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "Memberships";

        private DynamoUtils dynamoUtils;

        public DynamoMembershipRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<DynamoMembershipRepository>();
            Settings = settings.Value;

            AWSCredentials credentials = new BasicAWSCredentials(settings.Value.Aws.AccessKeyId, settings.Value.Aws.SecretAccessKey);
            Client = new AmazonDynamoDBClient(credentials);
            Context = new DynamoDBContext(Client);
            dynamoUtils = new DynamoUtils(loggerFactory, Client);

            CreateTable();
        }

        public void CreateTable()
        {
            dynamoUtils.CreateTable(Settings, TableName, 
                    new List<AttributeDefinition>
                    {
                        new AttributeDefinition
                        {
                            AttributeName = "Jid",
                            // "S" = string, "N" = number, and so on.
                            AttributeType = "S"
                        }
                    },
                    new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "Jid",
                            // "HASH" = hash key, "RANGE" = range key.
                            KeyType = "HASH"
                        }
                    },
                    new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 1,
                        WriteCapacityUnits = 1
                    });
        }

        public async Task<List<string>> GetMembershipsAsync()
        {
            var memberships = await Context.LoadAsync<Tables.Memberships>(Settings.Jid);

            return (memberships ?? new Tables.Memberships()).Rooms;
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