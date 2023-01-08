using System;

namespace payment
{
    public class InvoiceSearchQuery
    {
        public string Description { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int[] Types { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
    }
    public class SearchRestuls<T>
    {
        public T[] Items { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
        public int PageSize { get; set; }
    }
    public class ErrorDetails
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}