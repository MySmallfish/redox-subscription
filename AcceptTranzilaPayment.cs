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

namespace Redox.Payments
{
    public static class AcceptTranzilaPayment
    {
        [FunctionName("AcceptTranzilaPayment")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var items = new StringBuilder();
            foreach(var item in req.Form){
                items.Append($"<li>{item.Key}: {item.Value}</li>");
            }
            
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
    }
}
