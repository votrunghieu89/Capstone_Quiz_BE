namespace Capstone.DTOs.Group
{
    public class UpdateGroupDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
    }
}
