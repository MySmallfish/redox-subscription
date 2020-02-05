namespace payment
{
    public class InvoiceItem
    {
        public string Description { get; set; }
        public string Sku { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
    }
}