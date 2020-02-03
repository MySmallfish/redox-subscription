using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Redox.Payments
{
    public static class NotifyTranzilaPayment
    {
        
        [FunctionName("Notify")]
        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var items = new StringBuilder();
            var properties = new Dictionary<string,string>();

            await PostPaymentMessage(properties, log);


            foreach (var item in req.Form){
                items.Append($"<li>{item.Key}: {item.Value}</li>");
                properties[item.Key] = item.Value;
            }

            //outputMessage

            var result = await Task.FromResult((ActionResult)new ContentResult()
            {
                Content= $"<html><body><h1 color='green'>NOTIFIED!</h1>Response:<ul>{items}</ul></body></html>",
                
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

        private static async Task PostPaymentMessage(Dictionary<string, string> properties, ILogger log)
        {
            var connectionString =
                "Endpoint=sb://simplylog-eu.servicebus.windows.net/;SharedAccessKeyName=tranzila-redox-payment;SharedAccessKey=+Jf1uZEO5qg6y9mrr2jQ7iV0/3I/DBWUC+ITKqz8j+8=;EntityPath=redox-payments";
            var queueName = "redox-payments";
            log.LogInformation($"Posting message to '{queueName}'.");
            var json = JsonConvert.SerializeObject(properties);
            log.LogInformation($"Message Content: {json}.");
            var encoded = Encoding.UTF8.GetBytes(json);
            var message = new Message(encoded);
            var queueClient = new QueueClient(connectionString, queueName);
            
            await queueClient.SendAsync(message);
        }
    }
}