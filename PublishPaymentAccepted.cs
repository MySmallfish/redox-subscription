using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace payment
{
    public static class PublishPaymentAccepted
    {
        //[FunctionName("PublishPaymentAccepted")]
        //public static async Task Run([ServiceBusTrigger("redox-payments", Connection = "PaymentsQueueConnectionString")]string paymentMessage, ILogger log)
        //{
        //    log.LogInformation($"C# ServiceBus queue trigger function processed message: {paymentMessage}");

        //    var topicClient = new TopicClient("Endpoint=sb://simplylog-eu.servicebus.windows.net/;SharedAccessKeyName=manage;SharedAccessKey=Za90GIvrbhrAiFPQB30KncnHPfJVUUXMwqv8EsW/gvw=", "redox-events-debug");

        //    //var encoded = Encoding.UTF8.GetBytes(paymentMessage);
        //    var encoded = Encoding.UTF8.GetBytes(@"<s:Envelope xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:s=""http://www.w3.org/2003/05/soap-envelope""><s:Header><a:Action s:mustUnderstand=""1"">https://simplevision.co.il/platform//services/IEventsSubscriber/HandleEvent</a:Action><ActivityId CorrelationId=""561e0fae-2308-4869-bd81-759241b691af"" xmlns=""http://schemas.microsoft.com/2004/09/ServiceModel/Diagnostics"">00000000-0000-0000-0000-000000000000</ActivityId><VsDebuggerCausalityData xmlns=""http://schemas.microsoft.com/vstudio/diagnostics/servicemodelsink"">uIDPo7LmFUAXKZ1NorrZwJgBSV8AAAAAZeFgE6ZYg0iEmPL+2RsBGmaGcx21mu9AvDNIfAX/EF4ACQAA</VsDebuggerCausalityData><ApplicationContext xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://simplevision.co.il/application""><Language xmlns=""https://simplevision.co.il/platform//data"">he-IL</Language><Tenant xmlns=""https://simplevision.co.il/platform//data"">1</Tenant><Username i:nil=""true"" xmlns=""https://simplevision.co.il/platform//data"" /></ApplicationContext></s:Header><s:Body><HandleEvent xmlns=""https://simplevision.co.il/platform//services""><eventInfo xmlns:d4p1=""https://simplevision.co.il/platform//data"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><d4p1:Name>PaymentAccepted</d4p1:Name><d4p1:Parameters><d4p1:EventParameterInfo><d4p1:Name>Balance</d4p1:Name><d4p1:Value>0</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>CreditPoints</d4p1:Name><d4p1:Value>0</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>LinkedCustomerId</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>LinkedUserId</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>MatchFilters</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>ReferenceId</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Links</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>AdditionalPhone</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>CompanyName</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>PrivateCompanyId</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Comments</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>ZipCode</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Status</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Id</d4p1:Name><d4p1:Value>21106</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>UserId</d4p1:Name><d4p1:Value>1</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>CreatedAt</d4p1:Name><d4p1:Value>2018-08-31T06:44:41</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>LastModified</d4p1:Name><d4p1:Value>2020-02-04T11:25:44</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>UniqueId</d4p1:Name><d4p1:Value>15bd8bc8-2108-4493-b953-423562e37d69</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Name</d4p1:Name><d4p1:Value>שלו</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>IdNumber</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Phone</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Email</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Address</d4p1:Name><d4p1:Value>5543</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>HomeNumber</d4p1:Name><d4p1:Value>4554</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>City</d4p1:Name><d4p1:Value>הרצליה</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>CustomerType</d4p1:Name><d4p1:Value>2</d4p1:Value></d4p1:EventParameterInfo><d4p1:EventParameterInfo><d4p1:Name>Attributes</d4p1:Name><d4p1:Value /></d4p1:EventParameterInfo></d4p1:Parameters><d4p1:Source>1</d4p1:Source></eventInfo></HandleEvent></s:Body></s:Envelope>");
        //    var message = new Message(encoded);
        //    message.MessageId = Guid.NewGuid().ToString();
        //    message.SessionId = message.MessageId;
        //    message.UserProperties["Name"] = "PaymentAccepted";
        //    message.UserProperties["Source"] = "tranzila-redox-payment";
        //    await topicClient.SendAsync(message);
        //}
    }
}
