using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Redox.Payments
{
    public static class AcceptTranzilaPayment
    {
        
        [FunctionName("Accept")]
        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            log.LogInformation("C# HTTP trigger function processed a request.");
            var items = new StringBuilder();
            var properties = new Dictionary<string,string>();
            foreach (var item in req.Form)
            {

                properties[item.Key] = item.Value;
            }


            await PostPaymentMessage(properties, log, config);


            foreach (var item in properties)
            {

                items.Append($"<li>{item.Key}: {item.Value}</li>");
            }
            //outputMessage

            var result = await Task.FromResult((ActionResult)new ContentResult()
            {
                Content= $"<html><body><h1 color='green'>ACCEPTED!</h1>Response:<ul>{items}</ul></body></html>",
                
                StatusCode = (int)HttpStatusCode.OK,
                ContentType = "text/html"
            });
return result;
        //     string name = req.Query["contact"];
        // if (string.IsNullOrEmpty(name)){
        //     name = req.Form["contact"];
        // }

        //     string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //     dynamic data = JsonConvert.DeserializeObject(requestBody);
        //     name = name ?? data?.name;

        //     return name != null
        //         ? (ActionResult)new OkObjectResult($"Hello, {name}")
        //         : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        private static async Task PostPaymentMessage(Dictionary<string, string> properties, ILogger log, IConfiguration config)
        {

            var connectionString = config["PaymentsQueueConnectionString"];
            var queueName = config["AcceptedPaymentsQueueName"];
            log.LogInformation($"Posting message to '{queueName}'.");
            var payment = new
            {
                UserId = int.Parse(properties["userid"]),
                TenantId = int.Parse(properties["tenantid"]),
                AccountId = int.Parse(properties["accountid"]),
                Agents = int.Parse(properties["agents"]),
                Token = properties["TranzilaTK"],
                ExpiryMonth = properties["expmonth"],
                ExpiryYear = properties["expyear"],
                ReferenceId = properties["ConfirmationCode"],
                Amount = properties["sum"]
            };
            var json = JsonConvert.SerializeObject(payment);
            log.LogInformation($"Message Content: {json}.");
            var encoded = Encoding.UTF8.GetBytes(json);
            var message = new Message(encoded);
            var queueClient = new QueueClient(connectionString, queueName);
            
            await queueClient.SendAsync(message);


        }
    }
}
