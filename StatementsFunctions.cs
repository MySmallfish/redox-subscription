using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Simple.Data;

namespace payment
{


    public class CreateBillingParameters
    {
        public DateTime ValueDate { get; set; }
        public DateTime BillingDate { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }

        public string Description { get; set; }

        public string Comments { get; set; }
        
        public DateTime SubscriptionEndDate { get; set; }

        public int? AccountId { get; set; }

    }
    public class BillingFunctions
    {
        public static DateTime EndOfMonth(DateTime source)
        {
            return EndOfDay(new DateTime(source.Year, source.Month, DateTime.DaysInMonth(source.Year, source.Month)));
        }
        public static DateTime CombineTime(DateTime date, DateTime time, DateTimeKind kind = DateTimeKind.Unspecified)
        {
            var result = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
            result = DateTime.SpecifyKind(result, kind);
            return result;
        }
        public static DateTime EndOfDay(DateTime source)
        {

            var result = CombineTime(source,DateTime.Parse("23:59:59"));
            return result;
        }
        [Function("CreateBillingStatements")]
        public static async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req, ILogger log)
        {
            
            var parameters = new CreateBillingParameters()
            {
                BillingDate = DateTime.Now,
                BillingMonth = DateTime.Now.Month,
                BillingYear = DateTime.Now.Year,
                ValueDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10),
                Description = $"מנוי REDOX - החתמה דיגיטלית למתווכים - {DateTime.Now.ToString ("MMM", new CultureInfo("he-IL"))} {DateTime.Now.Year}",
                SubscriptionEndDate = EndOfMonth(DateTime.Now),
            };

            await ExecuteStoredProcedureAsync("CreateBillingStatements", parameters);

            return req.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        private static async Task ExecuteStoredProcedureAsync<T>(string spName, T parameters)
        {
            
            var connectionString = Environment.GetEnvironmentVariable("BillingDatabaseConnectionString");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(spName, connection))
                {
                    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(parameters);
                        if (value == null)
                        {
                            continue;
                        }

                        var parameterName = $"@{property.Name}";

                        var parameter = command.CreateParameter();
                        parameter.ParameterName = parameterName;
                        parameter.Value = value;
                        command.Parameters.Add(parameter);
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [Function("BillingStatements")]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req, ILogger log)
        {
            var items = await ReadStoredProcedureAsync<BillingStatement>("SearchBillingStatements", new { Status = BillingStatus.Created });
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            await response.WriteStringAsync(JsonConvert.SerializeObject(items));

            

            return response;
        }

        private static async Task<T[]> ReadStoredProcedureAsync<T>(string storedProcedureName, object parameters) where T : new()
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
            return items;
        }

        private static async Task<T[]> ReadAsync<T>(string sql, params SqlParameter[] parameters) where T : new()
        {
            var mapper = new DbReaderMapper<T>();
            var connectionString = Environment.GetEnvironmentVariable("BillingDatabaseConnectionString");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddRange(parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        
                        mapper.Read(reader);
                        //while (await reader.ReadAsync())
                        //{
                        //    var item = new T();
                            
                        //    var properties = item.GetType().GetProperties();
                        //    foreach (var property in properties)
                        //    {
                        //        var value = reader[property.Name];
                        //        if (value == DBNull.Value)
                        //        {
                        //            continue;
                        //        }

                        //        property.SetValue(item, value);
                        //    }

                        //    items.Add(item);
                        //}
                    }
                }
            }

            return mapper.Results.ToArray();
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
