//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using System.Net;
//using System.Text;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Azure.Functions.Worker.Http;

//namespace Redox.Payments
//{
//    public static class RejectTranzilaPayment
//    {
//        [Function("Reject")]
//        public static async Task<HttpResponseData> Run(
//            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
//            ILogger log)
//        {

//            var token ="Hb82fc842601f966956";
//            var pwd = "3JPShdSq";
//            var expmonth = "09";
//            var expyear = "23";
//            var exp = $"{expmonth}{expyear}";
//            var sum = 57;
//            var url = $"https://secure5.tranzila.com/cgi-bin/tranzila71u.cgi";


//            // var client = new System.Net.Http.HttpClient();
//            // var content = "supplier=redoxtok&TranzilaPW={pwd}&TranzilaTK={token}&expdate={exp}&sum={sum}&currency=1&cred_type=1&tranmode=A";
//            // var response = await client.PostAsync(url, new StringContent(content, "application/x-www-form-urlencoded"));
            
//            // var text = await response.Content.ReadAsStringAsync();
//            // log.LogInformation("C# HTTP trigger function processed a request.");

//            // var result = (ActionResult)new OkObjectResult(content);
//            // return result;
//            var items = new StringBuilder();
//            foreach(var item in req.Form){
//                items.Append($"<li>{item.Key}: {item.Value}</li>");
//            }


//            var result = await Task.FromResult((ActionResult)new ContentResult()
//            {
//                Content = $"<html><body><h1 color='red'>REJECTED!</h1>Response:<ul>{items}</ul></body></html>",
//                StatusCode = (int)HttpStatusCode.OK,
//                ContentType = "text/html"
//            });
//            return result;
//            //     string name = req.Query["contact"];
//            // if (string.IsNullOrEmpty(name)){
//            //     name = req.Form["contact"];
//            // }

//            //     string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
//            //     dynamic data = JsonConvert.DeserializeObject(requestBody);
//            //     name = name ?? data?.name;

//            //     return name != null
//            //         ? (ActionResult)new OkObjectResult($"Hello, {name}")
//            //         : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
//        }
//    }
//}
