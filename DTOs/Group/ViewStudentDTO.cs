namespace Capstone.DTOs.Group
{
    public class ViewStudentDTO
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; };
        public string Permission { get; set; } = string.Empty;
    }
}
