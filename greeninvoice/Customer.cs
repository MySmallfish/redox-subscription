namespace payment
{
    public class Customer
    {

        public string Id { get; set; }
        public string Name { get; set; }
        public string TaxId { get; set; }
        public string AccountingKey { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string[] Emails { get; set; }
        public string[] Labels { get; set; }
    }
}