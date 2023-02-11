using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Azure.Functions.Worker;
using payment.greeninvoice;

namespace payment
{
    public static class InvoiceCustomer
    {


        [Function("InvoiceCustomer")]
        [ServiceBusOutput("%InvoiceResponseQueueName%", Connection = "InvoicingQueueConnectionString")]
        public static async Task<InvoiceResponse> Run([ServiceBusTrigger("%InvoiceQueueName%", Connection = "InvoicingQueueConnectionString")] string myQueueItem, FunctionContext context)
        {
            var log = context.GetLogger("InvoiceCustomer");
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var response = await ProcessInvoice(myQueueItem, log, config);
               return response;

        }
        private static async Task<BillingFunctions.BillingStatus> GetBillingStatus(int id)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("BillingDatabaseConnectionString");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT Status FROM Billing WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                var result = await command.ExecuteScalarAsync();
                return (BillingFunctions.BillingStatus)result;
            }
            catch (Exception any)
            {

                throw;
            }
        }
        public static async Task UpdateBillingStatus(int id, BillingFunctions.BillingStatus status)
        {
            var connectionString = Environment.GetEnvironmentVariable("BillingDatabaseConnectionString");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand("UPDATE Billing SET Status = @Status WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Status", status);
            await command.ExecuteNonQueryAsync();
        }
        public static async Task<InvoiceResponse> ProcessInvoice(string myQueueItem, ILogger log, IConfigurationRoot config)
        {
            var paymentRequest = JsonConvert.DeserializeObject<InvoiceRequest>(myQueueItem);

            var status = await GetBillingStatus(paymentRequest.BillingId);
            if (status != BillingFunctions.BillingStatus.PaymentReceived)
            {
                log.LogWarning($"Billing status is {status} for billing id {paymentRequest.BillingId}");
                return new InvoiceResponse()
                {
                    BillingId = paymentRequest.BillingId,
                    TenantId = paymentRequest.Tenant.Id,
                    UserId = paymentRequest.User.UserId,
                    Error = $"Billing status is {status} for billing id {paymentRequest.BillingId}"
                };
            }

            await UpdateBillingStatus(paymentRequest.BillingId, BillingFunctions.BillingStatus.InvoicePending);
            
            var requestCustomer = MapToCustomer(paymentRequest);


            var response = new InvoiceResponse();
            try
            {
                var greenInvoiceClient = new GreenInvoiceClient(config, log);

                var customer = await greenInvoiceClient.SaveCustomer(requestCustomer);
                var invoice = CreateInvoiceReceiptRequest(customer, paymentRequest);

                response = await greenInvoiceClient.AddInvoice(invoice);

                await UpdateBillingStatus(paymentRequest.BillingId, BillingFunctions.BillingStatus.Invoiced);
            }
            catch (Exception anyException)
            {
                log.LogCritical(anyException, "Unable to create customer or invoice.");
                response.Error = anyException.ToString();
                await UpdateBillingStatus(paymentRequest.BillingId, BillingFunctions.BillingStatus.InvoiceFailed);
            }


            response.BillingId = paymentRequest.BillingId;
            response.TenantId = paymentRequest.Tenant.Id;
            response.UserId = paymentRequest.User.UserId;
            return response;
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
                Labels = new[] { "RedoxApp", "Azure" },
                Name = paymentRequest.Account.Name,
                Phone = paymentRequest.User.Phone,
                TaxId = paymentRequest.Account.TaxId
            };
            //#if DEBUG
            //            requestCustomer.Emails = new[] { "yair@redox.co.il" };
            //#endif

            return requestCustomer;
        }

        private static InvoiceReceipt CreateInvoiceReceiptRequest(Customer customer, InvoiceRequest paymentRequest)
        {
            var paymentDate = paymentRequest.Payment.PaymentDate.ToString("yyyy-MM-dd");
            var invoice = new InvoiceReceipt
            {
                Client = customer,

                Description = paymentRequest.Description,
                Remarks = paymentRequest.Comments,
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
                Quantity = item.Quantity,
                VatType = item.VatIncluded ? 1 : 0
            };
            return result;
        }

    }
}
