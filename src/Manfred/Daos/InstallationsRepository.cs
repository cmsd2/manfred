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
        }
    }

    public class InstallationsRepository : IInstallationsRepository
    {
        private ILogger logger;
        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "Installations";

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
                                AttributeName = "GroupId",
                                // "S" = string, "N" = number, and so on.
                                AttributeType = "S"
                            },
                            new AttributeDefinition
                            {
                                AttributeName = "RoomId",
                                AttributeType = "S"
                            }
                        },
                        KeySchema = new List<KeySchemaElement>
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
                CapabilitiesUrl = installation.CapabilitiesUrl
            };
        }
        
        public async Task CreateInstallationAsync(Installed installation)
        {
            var row = new Tables.Installations {
                GroupId = installation.GroupId,
                RoomId = installation.RoomId,
                CapabilitiesUrl = installation.CapabilitiesUrl,
                OauthId = installation.OauthId,
                OauthSecret = installation.OauthSecret
            };

            await Context.SaveAsync(row);
        }

        public async Task RemoveInstallationAsync(string groupId, string roomId = null)
        {
            await Context.DeleteAsync<Tables.Installations>(groupId, roomId);
        }
    }
}