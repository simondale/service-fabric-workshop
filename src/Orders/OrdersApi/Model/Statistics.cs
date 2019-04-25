namespace OrdersApi.Model
{
    public class Statistics
    {
        public StatisticsId Id { get; set; }
        public string Name { get; set; }
        public int OrdersCount { get; set; }
        public decimal OrdersValue { get; set; }
    }

    public class StatisticsId
    {
        public string Date { get; set; }
        public string Product { get; set; }
    }
}
