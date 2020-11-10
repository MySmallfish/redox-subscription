using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace payment.greeninvoice
{
    public class GreenInvoiceClient
    {
        private Uri ApiUrl { get; }
        private string GreenInvoiceApiId { get; }
        private string GreenInvoiceApiSecret { get; }
        private ILogger Log { get; }
        public GreenInvoiceClient(IConfiguration config, ILogger log)
        {
            ApiUrl = new Uri(config["ApiUrl"]);
            GreenInvoiceApiId = config["GreenInvoiceApiId"];
            GreenInvoiceApiSecret = config["GreenInvoiceApiSecret"];
            Log = log;
        }
      

        private  string m_token;
        public async Task<T> WithClient<T>(Func<HttpClient, Task<T>> run)
        {
            using (var httpClient = new HttpClient { BaseAddress = ApiUrl })
            {
                await AcquireToken<T>(httpClient);

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Bearer {m_token}");

                return await run(httpClient);

            }
        }

        private async Task AcquireToken<T>(HttpClient httpClient)
        {
            if (m_token == null)
            {
                using (var tokenRequest = AsJson(new
                {
                    id = GreenInvoiceApiId,
                    secret = GreenInvoiceApiSecret
                }))
                {
                    var response = await httpClient.PostAsync("account/token", tokenRequest);
                    var json = await response.Content.ReadAsStringAsync();
                    m_token = JsonConvert.DeserializeObject<dynamic>(json).token.ToString();
                }
            }
        }

        private async Task<T> FromJson<T>(HttpContent content)
        {
            var json = await content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            return result;
        }
        private StringContent AsJson<T>(T customer)
        {
            var json = JsonConvert.SerializeObject(customer, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var content = new StringContent(json, System.Text.Encoding.Default, "application/json");
            return content;
        }

        public async Task<Customer> SaveCustomer(Customer customer)
        {
            var result = await WithClient(async client =>
            {
                using (var content = AsJson(customer))
                {
                    using (var response = await client.PostAsync("clients", content))
                    {
                        if (response.IsSuccessStatusCode)
                        {


                            var receivedCustomer = await FromJson<Customer>(response.Content);
                            return receivedCustomer;
                        }
                        else
                        {
                            var error = await FromJson<ErrorDetails>(response.Content);
                            if (error.ErrorCode == 1010)
                            {
                                customer.Id = error.ErrorMessage;
                                return customer;
                                //    new Customer()
                                //{
                                //    Id = error.ErrorMessage

                                //};
                            }
                            else
                            {
                                throw new ArgumentException($"Unable to Create Customer: Code: {error.ErrorCode}, Message: {error.ErrorMessage}");
                            }
                        }
                    }
                }
            });
            return result;
        }

        private async Task<InvoiceResponse> CheckExistingDocument(InvoiceReceipt invoice)
        {
            var result = await WithClient(async client =>
            {
                using (var content = AsJson(new
                {
                    ClientId = invoice.Client.Id,
                    FromDate = invoice.Payment[0].Date,
                    ToDate = invoice.Payment[0].Date,
                    Description = invoice.Description
                }))
                {
                    using (var response = await client.PostAsync("documents/search", content))
                    {
                        var invoiceResults = default(InvoiceResponse);
                        if (response.IsSuccessStatusCode)
                        {
                            var searchResult = await FromJson<InvoiceSearchResult>(response.Content);
                            if (searchResult != null && searchResult.Total > 0)
                            {
                                invoiceResults = searchResult.Items.First();
                            }
                        }
                        return invoiceResults;

                    }
                }
            });
            return result;
        }

        public async Task<InvoiceResponse> AddInvoice(InvoiceReceipt receipt)
        {
            var result = await CheckExistingDocument(receipt);
            if (result == null)
            {
                result = await WithClient(async client =>
                {
                    using (var content = AsJson(receipt))
                    {
                        using (var response = await client.PostAsync("documents", content))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var receivedCustomer = await FromJson<InvoiceResponse>(response.Content);

                                return receivedCustomer;
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to create invoice");
                            }
                        }
                    }
                });
            }

            return result;
        }
    }
}
