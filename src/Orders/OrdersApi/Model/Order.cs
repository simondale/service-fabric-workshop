using System;

namespace OrdersApi.Model
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTimeOffset OrderDateTime { get; set; }
        public Product[] Products { get; set; }
    }
}
