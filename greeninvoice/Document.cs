namespace payment.greeninvoice
{
    public class Document
    {
        public Document(string type)
        {
            Type = type;
            Lang = "he";
            Currency = "ILS";

        }

        public bool Attachment { get; set; }

        public bool Rounding { get; set; } = true;
        public Customer Client { get; set; }
        public string Description { get; set; }
        public string Remarks { get; set; }

        public IncomeItem[] Income { get; set; }
        public PaymentItem[] Payment { get; set; }

        public int VatType { get; set; }
        public string Currency { get; set; }
        public string Type { get; set; }
        public string Lang { get; set; }
    }
}