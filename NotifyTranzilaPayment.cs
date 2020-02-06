using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using payment;

namespace Redox.Payments
{
    public class PaymentRecord : TableEntity
    {
        private IReadOnlyDictionary<string, string> m_properties;
        public PaymentRecord(IReadOnlyDictionary<string, string> properties)
        {
            RowKey = properties["ConfirmationCode"];
            PartitionKey = properties["tenantid"];
            m_properties = properties;
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var result =  base.WriteEntity(operationContext);
            foreach (var keyValuePair in m_properties)
            {
                result[keyValuePair.Key] = new EntityProperty(keyValuePair.Value);
            }
            return result;
        }
    }
    public static class NotifyTranzilaPayment
    {
        public static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }

        public static async Task<PaymentRecord> InsertOrMergeEntityAsync(CloudTable table, PaymentRecord entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
         
                // Create the InsertOrReplace table operation
                var insertOrMergeOperation = TableOperation.Insert(entity);

                // Execute the operation.
                
                var result = await table.ExecuteAsync(insertOrMergeOperation);
                var insertedRecord = result.Result as PaymentRecord;

                return insertedRecord;
          
        }

        [FunctionName("Notify")]
        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var properties = new Dictionary<string, string>();
            foreach (var item in req.Form)
            {
                properties[item.Key] = item.Value;
            }
            log.LogInformation($"Accepted payment notification. Parameters: {JsonConvert.SerializeObject(properties)}");

            try
            {
                var storageAccount = CreateStorageAccountFromConnectionString(config["NotifiedPaymentsLogConnectionString"]);
                var paymentRecord = new PaymentRecord(properties);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference(config["NotifiedTransactionsLogTableName"]);
                await InsertOrMergeEntityAsync(table, paymentRecord);

            }
            catch (Exception anyException)
            {
                log.LogError(anyException, "Unable to store payment record.");
                throw;
            }
       
            var result = await Task.FromResult((ActionResult) new OkResult());
            return result;
        }

   }
}