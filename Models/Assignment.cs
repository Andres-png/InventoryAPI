namespace InventoryApi.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public int HardwareItemId { get; set; }
        public HardwareItem HardwareItem { get; set; } = null!;
        public int AssignedToId { get; set; }
        public User AssignedTo { get; set; } = null!;
        public int AssignedById { get; set; }
        public User AssignedBy { get; set; } = null!;
        public DateTime AssignmentDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; } = "Activo";
        public string? Notes { get; set; }
        public string? RejectionReason { get; set; }
    }
}