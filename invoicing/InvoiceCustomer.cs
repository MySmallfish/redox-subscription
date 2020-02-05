using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Net.NetworkInformation;
using payment.greeninvoice;

namespace payment
{
    public static class InvoiceCustomer
    {


        [FunctionName("InvoiceCustomer")]
        public static async Task Run([ServiceBusTrigger("redox-invoices", Connection = "InvoicingQueueConnectionString")]string myQueueItem, [ServiceBus("redox-invoices-response", Connection = "InvoicingQueueConnectionString")]IAsyncCollector<InvoiceResponse> output, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var paymentRequest = JsonConvert.DeserializeObject<InvoiceRequest>(myQueueItem);
            var requestCustomer = MapToCustomer(paymentRequest);


            var response = new InvoiceResponse();
            try
            {
                var greenInvoiceClient = new GreenInvoiceClient(config, log);

                var customer = await greenInvoiceClient.SaveCustomer(requestCustomer);
                var invoice = CreateInvoiceReceiptRequest(customer, paymentRequest);

                response = await greenInvoiceClient.AddInvoice( invoice);
                
            }
            catch (Exception anyException)
            {
                log.LogCritical(anyException, "Unable to create customer or invoice.");
                response.Error = anyException.ToString();
            }

            response.BillingId = paymentRequest.BillingId;
            response.TenantId = paymentRequest.Tenant.Id;
            response.UserId = paymentRequest.User.UserId;
            await output.AddAsync(response);
        }

        private static Customer MapToCustomer(InvoiceRequest paymentRequest)
        {
            var requestCustomer = new Customer()
            {
                AccountingKey = paymentRequest.Account.Id.ToString(),
                Address = paymentRequest.Tenant.Address,
                City = paymentRequest.Tenant.City,
                Emails = new[]
                {
                    paymentRequest.User.Email,
                    paymentRequest.Tenant.AdminEmail
                },
                Labels = new[] {"RedoxApp", "Azure"},
                Name = paymentRequest.Account.Name,
                Phone = paymentRequest.User.Phone,
                TaxId = paymentRequest.Account.TaxId
            };
#if DEBUG
            requestCustomer.Emails = new[] { "yair@redox.co.il" };
#endif

            return requestCustomer;
        }

        private static InvoiceReceipt CreateInvoiceReceiptRequest(Customer customer, InvoiceRequest paymentRequest)
        {
            var paymentDate = paymentRequest.Payment.PaymentDate.ToString("yyyy-MM-dd");
            var invoice = new InvoiceReceipt
            {
                Client = customer,
                Description = paymentRequest.Description,
                Income = paymentRequest.Items.Select(MapToIncomeItem).ToArray(),
                Attachment = true,
                Payment = new[]
                {
                    new CreditCardPaymentItem()
                    {
                        CardNum = paymentRequest.Payment.CardNum,
                        Date = paymentDate,
                        Price = paymentRequest.Payment.Amount
                    },
                }
            };
            return invoice;
        }

        private static IncomeItem MapToIncomeItem(InvoiceItem item)
        {
            var result = new IncomeItem()
            {
                CatalogNum = item.Sku,
                Description = item.Description,
                Price = item.Price,
                Quantity = item.Quantity
            };
            return result;
        }

    }
}
