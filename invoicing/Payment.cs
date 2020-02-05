using System;

namespace payment
{
    public class Payment
    {
        public string SubscriptionEndDate { get; set; }
        public DateTime PaymentDate { get; set; }
        public double Amount { get; set; }
        public int Agents { get; set; }
        public string CardNum { get; set; }
    }
}