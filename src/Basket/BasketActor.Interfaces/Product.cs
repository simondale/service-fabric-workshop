using System;

namespace BasketActor.Interfaces
{
    [Serializable]
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
