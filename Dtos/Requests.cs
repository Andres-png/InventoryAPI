using InventoryApi.Models;

namespace InventoryApi.Dtos
{
    public record RegisterRequest(string Username, string Password);
    public record LoginRequest(string Username, string Password);
    public record CreateUserRequest(string Username, string Password);
    public record CreateHardwareRequest(string EquipmentType, string Brand, string Model, string SerialNumber, string Status, int? AssignedToId);
    public record UpdateHardwareRequest(string? EquipmentType, string? Brand, string? Model, string? SerialNumber, string? Status, int? AssignedToId);
    public record CreateStockRequest(string Name, string Category, int Quantity, int MinStock);
    public record UpdateStockRequest(string Name, string Category, int Quantity, int MinStock);
    public record CreateAssignmentRequest(int HardwareItemId, int AssignedToId, DateTime? ExpectedReturnDate, string? Notes);
    public record RejectAssignmentRequest(string Reason);
    public record UpdateUserRequest(string? Username, string? Password);
}