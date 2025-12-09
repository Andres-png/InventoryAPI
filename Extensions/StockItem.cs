namespace InventoryApi.Extensions
{
    internal class StockItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public int MinStock { get; set; }
        public int UserId { get; set; }
    }
}