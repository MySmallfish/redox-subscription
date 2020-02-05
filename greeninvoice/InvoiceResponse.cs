namespace payment.greeninvoice
{
    public class InvoiceResponse
    {
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public int BillingId { get; set; }
        public string Id { get; set; }
        public string Number { get; set; }
        public string Lang { get; set; }

        public DocumentUrl Url { get; set; }

        public string Error { get; set; }

    }


}