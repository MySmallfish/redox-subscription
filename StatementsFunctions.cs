using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace payment
{
    public class BillingFunctions
    {
        [Function("BillingStatements")]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req, ILogger log)
        {
            var items = await ReadStoredProcedureAsync<BillingStatement>("SearchBillingStatements", new { Status = BillingStatus.Created });
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            await response.WriteStringAsync(JsonConvert.SerializeObject(items));

            

            return response;
        }

        private static async Task<T> ReadStoredProcedureAsync<T>(string storedProcedureName, object parameters) where T : new()
        {

            var query = new StringBuilder();
            query.Append($"EXEC {storedProcedureName} ");

            var parametersList = new List<SqlParameter>();
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(parameters);
                if (value == null)
                {
                    continue;
                }

                var parameterName = $"@{property.Name}";
                query.Append($"{parameterName}, ");

                var parameter = new SqlParameter(parameterName, value);
                parametersList.Add(parameter);
            }

            query.Remove(query.Length - 2, 2);

            var sql = query.ToString();
            var items = await ReadAsync<T>(sql, parametersList.ToArray());
            return items.FirstOrDefault();
        }

        private static async Task<T[]> ReadAsync<T>(string sql, params SqlParameter[] parameters) where T : new()
        {
            var items = new List<T>();

            var connectionString = Environment.GetEnvironmentVariable("BillingDatabaseConnectionString");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddRange(parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new T();
                            var properties = item.GetType().GetProperties();
                            foreach (var property in properties)
                            {
                                var value = reader[property.Name];
                                if (value == DBNull.Value)
                                {
                                    continue;
                                }

                                property.SetValue(item, value);
                            }

                            items.Add(item);
                        }
                    }
                }
            }

            return items.ToArray();
        }   

        public enum BillingStatus
        {
            Created,
            PaymentReceived,
            Invoiced,
            PaymentFailed,
            InvoiceFailed,
            InvoicePending,
            BillingFailure
        }

        public class BillingStatementQuery
        {
            public int? Id { get; set; }

            public BillingStatus? Status { get; set; }
        }
        public class BillingStatement
        {
            public BillingStatus Status { get; set; }
            public int BillingMonth { get; set; }
            public int BillingYear { get; set; }
            public int Id { get; set; }
            public double Amount { get; set; }
            public int Agents { get; set; }
            public Account Account { get; set; }
            public Tenant Tenant { get; set; }
            public User User { get; set; }

        }
    }
}
