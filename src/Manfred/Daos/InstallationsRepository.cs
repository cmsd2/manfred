using Manfred.Models;
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

namespace Manfred.Daos
{
    namespace Tables
    {
        [DynamoDBTable("Installations")]
        public class Installations
        {
            [DynamoDBHashKey]
            public string GroupId { get; set; }

            [DynamoDBRangeKey]
            public string RoomId {get; set;}

            public string CapabilitiesUrl {get; set;}
            public string OauthId {get; set;}
            public string OauthSecret {get; set;}
            public string AccessToken {get; set;}
            public string ExpiresAt {get; set;}
            public List<string> Scopes {get; set;}
        }
    }

    public class InstallationsRepository : IInstallationsRepository
    {
        private ILogger logger;
        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "Installations";
        public readonly string InstallationsByOauthIdIndex = "InstallationsByOauthId";

        private DynamoUtils dynamoUtils;

        public InstallationsRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<InstallationsRepository>();
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
                            AttributeName = "GroupId",
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
                            AttributeName = "OauthId",
                            AttributeType = "S"
                        }
                    },
                    new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "GroupId",
                            // "HASH" = hash key, "RANGE" = range key.
                            KeyType = "HASH"
                        },
                        new KeySchemaElement
                        {
                            AttributeName = "RoomId",
                            KeyType = "RANGE"
                        }
                    },
                    new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 1,
                        WriteCapacityUnits = 1
                    },
                    new List<GlobalSecondaryIndex> {
                        new GlobalSecondaryIndex {
                            IndexName = InstallationsByOauthIdIndex,
                            KeySchema = new List<KeySchemaElement> 
                            {
                                new KeySchemaElement {
                                    AttributeName = "OauthId",
                                    KeyType = "HASH"
                                }
                            },
                            ProvisionedThroughput = new ProvisionedThroughput
                            {
                                ReadCapacityUnits = 1,
                                WriteCapacityUnits = 1
                            },
                            Projection = new Projection
                            {
                                ProjectionType = ProjectionType.ALL
                            }
                        }
                    });
        }

        public async Task<Installation> GetInstallationAsync(string groupId, string roomId = null)
        {
            var installation = await Context.LoadAsync<Tables.Installations>(groupId, roomId);

            if(installation == null)
            {
                return null;
            }

            return new Installation {
                GroupId = installation.GroupId,
                RoomId = installation.RoomId,
                OauthId = installation.OauthId,
                OauthSecret = installation.OauthSecret,
                CapabilitiesUrl = installation.CapabilitiesUrl,
                AccessToken = installation.AccessToken,
                ExpiresAt = installation.ExpiresAt,
                Scopes = installation.Scopes
            };
        }
        
        public async Task CreateInstallationAsync(Installation installation)
        {
            var row = new Tables.Installations {
                GroupId = installation.GroupId,
                RoomId = installation.RoomId,
                CapabilitiesUrl = installation.CapabilitiesUrl,
                OauthId = installation.OauthId,
                OauthSecret = installation.OauthSecret,
                AccessToken = installation.AccessToken,
                ExpiresAt = installation.ExpiresAt,
                Scopes = installation.Scopes
          };

            await Context.SaveAsync(row);
        }

        public async Task RemoveInstallationAsync(string groupId, string roomId = null)
        {
            await Context.DeleteAsync<Tables.Installations>(groupId, roomId);
        }
        
        public async Task<Installation> GetInstallationByOauthIdAsync(string oauthId)
        {
            QueryRequest queryRequest = new QueryRequest
            {
                TableName = dynamoUtils.FullTableName(Settings, TableName),
                IndexName = InstallationsByOauthIdIndex,
                KeyConditionExpression = "#oid = :oauthId",
                ExpressionAttributeNames = new Dictionary<String, String> {
                    {"#oid", "OauthId"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":oauthId", new AttributeValue { S =  oauthId }}
                },
                ScanIndexForward = true
            };
            
            var result = await Client.QueryAsync(queryRequest);
            
            if(result.Items.Count == 0)
            {
                return null;
            } 
            
            var row = result.Items[0];
            
            return new Installation {
                OauthId = row["OauthId"].S,
                OauthSecret = Lookup(row, "OauthSecret")?.S,
                GroupId = row["GroupId"].S,
                RoomId = row["RoomId"].S,
                CapabilitiesUrl = Lookup(row, "CapabilitiesUrl")?.S,
                AccessToken = Lookup(row, "AccessToken")?.S,
                ExpiresAt = Lookup(row, "ExpiresAt")?.S,
                Scopes = Lookup(row, "Scopes")?.SS
            };
        }

        public AttributeValue Lookup(IDictionary<string, AttributeValue> values, string key)
        {
            AttributeValue attrValue = null;
            values.TryGetValue(key, out attrValue);
            return attrValue;
        }

        public async Task RemoveInstallationByOauthAsync(string oauthId)
        {
            //TODO figure out how to perform a DeleteItemRequest against a Global Secondary Index.
            
            var installation = await GetInstallationByOauthIdAsync(oauthId);
            
            logger.LogInformation($"removing installation GroupId={installation.GroupId} RoomId={installation.RoomId}");
            
            await RemoveInstallationAsync(installation.GroupId, installation.RoomId);
        }
    }
}