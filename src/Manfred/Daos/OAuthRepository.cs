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
        [DynamoDBTable("OAuth")]
        public class OAuth
        {
            [DynamoDBHashKey]
            public string OauthId {get; set;}

            public string GroupId { get; set; }

            public string RoomId {get; set;}

            public string CapabilitiesUrl {get; set;}
            
            public string OauthSecret {get; set;}

            public string AccessToken {get; set;}

            public string ExpiresAt {get; set;}

            public List<string> Scopes {get; set;}
        }
    }

    public class OAuthRepository : IOAuthRepository
    {
        private ILogger logger;
        public AmazonDynamoDBClient Client { get; set; }
        public DynamoDBContext Context { get; set; }
        public Settings Settings { get; set; }
        public readonly string TableName = "OAuth";

        private DynamoUtils dynamoUtils;

        public OAuthRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<OAuthRepository>();
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
                                AttributeName = "OauthId",
                                // "S" = string, "N" = number, and so on.
                                AttributeType = "S"
                            }
                        },
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                            {
                                AttributeName = "OauthId",
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
        
        public async Task CreateOauthAsync(Oauth oauth)
        {
            var row = new Tables.OAuth {
                GroupId = oauth.GroupId,
                RoomId = oauth.RoomId,
                CapabilitiesUrl = oauth.CapabilitiesUrl,
                OauthId = oauth.OauthId,
                OauthSecret = oauth.OauthSecret,
                AccessToken = oauth.AccessToken,
                ExpiresAt = oauth.ExpiresAt,
                Scopes = oauth.Scopes
            };

            await Context.SaveAsync(row);
        }

        public async Task<Oauth> GetOauthAsync(string oauthId)
        {
            var row = await Context.LoadAsync<Tables.OAuth>(oauthId);

            if(row == null)
            {
                return null;
            }
            
            return new Oauth {
                OauthId = row.OauthId,
                OauthSecret = row.OauthSecret,
                GroupId = row.GroupId,
                RoomId = row.RoomId,
                CapabilitiesUrl = row.CapabilitiesUrl,
                AccessToken = row.AccessToken,
                ExpiresAt = row.ExpiresAt,
                Scopes = row.Scopes
            };
        }

        public async Task RemoveOauthAsync(string oauthId)
        {
            await Context.DeleteAsync<Tables.OAuth>(oauthId);
        }
    }
}