namespace payment.greeninvoice
{
    public class InvoiceSearchResult
    {
        public int Total { get; set; }
        public InvoiceResponse[] Items { get; set; }
    }
}