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
using Newtonsoft.Json;
using payment;

namespace Redox.Payments
{
    public static class NotifyTranzilaPayment
    {
        
        [FunctionName("Notify")]
        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {

           // var config = new ConfigurationBuilder()
           //     .SetBasePath(context.FunctionAppDirectory)
           //     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
           //     .AddEnvironmentVariables()
           //     .Build();
           // var json =
           //     "{\"BillingId\":444,\"Items\":[{\"Description\":\"ברוקר מורחב\",\"Sku\":\"14\",\"Price\":49.0,\"Quantity\":2}],\"Account\":{\"Id\":152,\"Name\":\"בייס אחזקות בע\\\"מ\",\"TaxId\":\"514792456\",\"Tenant\":null,\"User\":null},\"Tenant\":{\"Id\":3655,\"Name\":\"מפתח העיר אריאל\",\"Address\":\"אורי בראון 6\",\"City\":\"אריאל\",\"AdminEmail\":\"elikob15@gmail.com\"},\"User\":{\"ManagementRegion\":null,\"CountDealsTotal\":0,\"CountExelosiveTotal\":0,\"CountSignTotal\":0,\"UserId\":3712,\"FullName\":\"אליקו בן עיון\",\"Email\":\"elikob15@gmail.com\",\"Phone\":\"0544595821\"},\"Description\":\"מנוי REDOX - החתמה דיגיטלית למתווכים\",\"Payment\":{\"PaymentDate\":\"2020-02-06T01:25:08.59Z\",\"Amount\":115.0,\"Agents\":2,\"CardNum\":\"6255\"}}";
           // json =
           //     "{\"BillingId\":445,\"Items\":[{\"Description\":\"סוכן במשרד\",\"Sku\":\"4\",\"Price\":39.0,\"Quantity\":3}],\"Account\":{\"Id\":124,\"Name\":\"הומניר נכסים\",\"TaxId\":\"038719944\",\"Tenant\":null,\"User\":null},\"Tenant\":{\"Id\":4004,\"Name\":\"הומניר נכסים\",\"Address\":\"מצדה 7 , בניין ב.ס.ר 4 קומה 24\",\"City\":\"בני ברק\",\"AdminEmail\":\"office@homenir.co.il\"},\"User\":{\"ManagementRegion\":null,\"CountDealsTotal\":0,\"CountExelosiveTotal\":0,\"CountSignTotal\":0,\"UserId\":4099,\"FullName\":\"דרור ניר\",\"Email\":\"office@homenir.co.il\",\"Phone\":\"0548038317\"},\"Description\":\"מנוי REDOX - החתמה דיגיטלית למתווכים\",\"Payment\":{\"PaymentDate\":\"2020-02-06T01:35:09.2Z\",\"Amount\":137.0,\"Agents\":4,\"CardNum\":\"6407\"}}";
           //// await InvoiceCustomer.ProcessInvoice(json, log,config );
           // log.LogInformation("C# HTTP trigger function processed a request.");
           // var items = new StringBuilder();
           // var properties = new Dictionary<string,string>();

           // await PostPaymentMessage(properties, log);


           // foreach (var item in req.Form){
           //     items.Append($"<li>{item.Key}: {item.Value}</li>");
           //     properties[item.Key] = item.Value;
           // }

           // //outputMessage

           // var result = await Task.FromResult((ActionResult)new ContentResult()
           // {
           //     Content= $"<html><body><h1 color='green'>NOTIFIED!</h1>Response:<ul>{items}</ul></body></html>",
                
           //     StatusCode = (int)HttpStatusCode.OK,
           //     ContentType = "text/html"
           // });
           var result = await Task.FromResult((ActionResult) new OkResult());
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
                "PaymentsQueueConnectionString";
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