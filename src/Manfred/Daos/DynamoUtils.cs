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
    public class DynamoUtils
    {
        private ILogger logger;

        public AmazonDynamoDBClient Client {get; set;}

        public DynamoUtils(ILoggerFactory loggerFactory, AmazonDynamoDBClient client)
        {
            logger = loggerFactory.CreateLogger<DynamoUtils>();
            Client = client;
        }
        
        public string FullTableName(Settings settings, string tableName)
        {
            return $"{settings.TableNamePrefix ?? ""}{tableName}";
        }

        public bool TableExists(Settings settings, string unprefixedTableName)
        {
            var tableName = FullTableName(settings, unprefixedTableName);
            var status = "";

            try
            {
                logger.LogInformation($"checking table {tableName}");

                var response = Client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableName
                }).Result;

                logger.LogInformation("Table = {0}, Status = {1}",
                    response.Table.TableName,
                    response.Table.TableStatus);

                status = response.Table.TableStatus;
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => {
                    if (e is ResourceNotFoundException)
                    {
                        logger.LogInformation("DescribeTable = {0}, Error = {1}", tableName, e.Message);
                        // DescribeTable is eventually consistent. So you might
                        //   get resource not found. 
                        return true;
                    }

                    return false;
                });

                return false;
            }

            return true;
        }
        
        public void CreateTable(Settings settings, string unprefixedTableName, List<AttributeDefinition> attributes, List<KeySchemaElement> keys, ProvisionedThroughput throughput = null, List<GlobalSecondaryIndex> globalSecondaryIndexes = null)
        {
            string tableName = FullTableName(settings, unprefixedTableName);
            
            int sleepTime = 1;
            
            throughput = throughput ?? new ProvisionedThroughput
            {
                ReadCapacityUnits = 1,
                WriteCapacityUnits = 1
            };
          
            globalSecondaryIndexes = globalSecondaryIndexes ?? new List<GlobalSecondaryIndex>();

            while (!TableExists(settings, unprefixedTableName))
            {
                try
                {
                    var request = new CreateTableRequest
                    {
                        TableName = tableName,
                        AttributeDefinitions = attributes,
                        KeySchema = keys,
                        ProvisionedThroughput = throughput,
                        GlobalSecondaryIndexes = globalSecondaryIndexes
                    };

                    logger.LogInformation($"creating table {tableName}");
                    var response = Client.CreateTableAsync(request).Result;

                    logger.LogInformation($"create table result: {response.HttpStatusCode} requestId: {response.ResponseMetadata.RequestId} ");
                }
                catch (ResourceInUseException e)
                {
                    logger.LogInformation($"CreateTable = {tableName}, Error = {e.Message}");
                }

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(sleepTime++));
            }
        }
    }
}