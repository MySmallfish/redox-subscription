namespace payment.greeninvoice
{
    public class PaymentItem
    {
        public PaymentItem(int type)
        {
            Type = type;
        }
        public int Type { get; set; }
        public double Price { get; set; }
        public string Date { get; set; }
        public string Currency { get; set; } = "ILS";
        public string CardNum { get; set; }
    }
}