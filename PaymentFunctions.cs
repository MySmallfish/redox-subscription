using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using payment;
using payment.greeninvoice;


namespace Redox.Payments
{
    public static class RequestExtensions
    {
        public static async Task<(string Key, string Value)[]> Parse(this HttpRequestData request)
        {
            var body = await request.ReadAsStringAsync();
            var bodyParts = body.Split('&');
            var result = bodyParts
                .Select(part => (Key: part.Split("=")[0], Value: part.Split("=").ElementAtOrDefault(1))).ToArray();
            return result;
        }

        public static async Task<HttpResponseData> Process<T>(this HttpRequestData request, ILogger log, IConfiguration config, string header, string queueName, Func<IDictionary<string,string>,T> prepareMessage)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);
            try
            {
                var items = new StringBuilder();
                var properties = new Dictionary<string, string>();
                var formValues = await request.Parse();
                foreach (var item in formValues)
                {
                    properties[item.Key] = item.Value;
                }
                var message = prepareMessage(properties);
                await PostPaymentMessage(log, config, queueName, message);


                foreach (var item in properties)
                {

                    items.Append($"<li>{item.Key}: {item.Value}</li>");
                }

                response.Headers.Add("Content-Type", "text/html");
                await response.WriteStringAsync($"<html><body><h1>Payment {header}</h1><ul>{items}</ul></body></html>");
                return response;
            }
            catch (Exception e)
            {
                response = request.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync(e.Message);
            }

            return response;
        }   

        public static async Task PostPaymentMessage<T>(ILogger log, IConfiguration config, string queueConfigName, T content)
        {

            var connectionString = config["PaymentsQueueConnectionString"];
            var queueName = config[queueConfigName];
            log.LogInformation($"Posting message to '{queueName}'.");

            var json = JsonConvert.SerializeObject(content);
            log.LogInformation($"Message Content: {json}.");
            var encoded = Encoding.UTF8.GetBytes(json);
            var message = new Azure.Messaging.ServiceBus.ServiceBusMessage(encoded);
            var queueClient = new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
            var sender = queueClient.CreateSender(queueName);

            await sender.SendMessageAsync(message);


        }
    }
    public static class PaymentFunctions
    {
        
        [Function("Accept")]
        
        public static async Task<HttpResponseData> RunAccepted(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var config = PrepareConfig();
            var log = context.GetLogger("Accept");
            log.LogInformation("C# HTTP trigger function processed Accepted request.");

            var response = await req.Process(log, config, "Accepted", "AcceptedPaymentsQueueName", properties =>
            {
                var payment = new
                {
                    BillingId = int.Parse(properties["billingid"]),
                    UserId = int.Parse(properties["userid"]),
                    TenantId = int.Parse(properties["tenantid"]),
                    AccountId = int.Parse(properties["accountid"]),
                    Agents = int.Parse(properties["agents"]),
                    Token = properties["TranzilaTK"],
                    ExpiryMonth = properties["expmonth"],
                    ExpiryYear = properties["expyear"],
                    ReferenceId = properties["ConfirmationCode"],
                    Amount = properties["sum"],
                    Status = BillingFunctions.BillingStatus.PaymentReceived,
                };
                return payment;
            });
            return response;
        }
        [Function("Reject")]
        
        public static async Task<HttpResponseData> RunRejected(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var config = PrepareConfig();
            var log = context.GetLogger("Reject");
            log.LogInformation("C# HTTP trigger function processed Rejected request.");

            var response = await req.Process(log, config, "Rejected", "RejectedPaymentsQueueName", properties => new
            {
                BillingId = int.Parse(properties["billingid"]),
                UserId = int.Parse(properties["userid"]),
                TenantId = int.Parse(properties["tenantid"]),
                AccountId = int.Parse(properties["accountid"]),
                Status = BillingFunctions.BillingStatus.PaymentFailed,
            });
            return response;
        }
        [Function("Notify")]
        
        public static async Task<HttpResponseData> RunNotify(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var config = PrepareConfig();

            var log = context.GetLogger("Notify");
            log.LogInformation("C# HTTP trigger function processed Notify request.");

            var response = await req.Process(log, config, "Notify", "NotifiedPaymentsQueueName",properties =>
            {
                var payment = new
                {
                    BillingId = int.Parse(properties["billingid"]),
                    UserId = int.Parse(properties["userid"]),
                    TenantId = int.Parse(properties["tenantid"]),
                    AccountId = int.Parse(properties["accountid"]),
                    Agents = int.Parse(properties["agents"]),
                    Token = properties["TranzilaTK"],
                    ExpiryMonth = properties["expmonth"],
                    ExpiryYear = properties["expyear"],
                    ReferenceId = properties["ConfirmationCode"],
                    Amount = properties["sum"],
                    Status = BillingFunctions.BillingStatus.PaymentReceived,
                };
                return payment;
            });
            return response;
        }

        private static IConfigurationRoot PrepareConfig()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}
