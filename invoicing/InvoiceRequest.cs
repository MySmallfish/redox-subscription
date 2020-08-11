namespace payment
{
   
    public class InvoiceRequest
    {
        public string Comments { get; set; }
        public string Description { get; set; }
        public int BillingId { get; set; }
        public InvoiceItem[] Items { get; set; }
        public Account Account { get; set; }
        public Tenant Tenant { get; set; }
        public User User { get; set; }

        public Payment Payment { get; set; }
    }
}