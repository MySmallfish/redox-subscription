namespace payment.greeninvoice
{
    public class IncomeItem
    {
        public string CatalogNum { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }

        public int VatType { get; set; }
    }
}