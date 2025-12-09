namespace InventoryApi.Models
{
    public class HardwareItem
    {
        public int Id { get; set; }
        public string EquipmentType { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Status { get; set; } = "Activo";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }
    }
}