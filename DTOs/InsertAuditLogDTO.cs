namespace Capstone.DTOs
{
    public class InsertAuditLogDTO
    {
        public int AccountId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }
}
