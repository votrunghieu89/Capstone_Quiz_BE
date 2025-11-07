namespace Capstone.DTOs
{
    public class ViewingAuditLogDTO
    {
        public int AccountId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}
