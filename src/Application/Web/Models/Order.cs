using System;

namespace Web.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTimeOffset OrderDateTime { get; set; }
        public Product[] Products { get; set; }
    }
}
