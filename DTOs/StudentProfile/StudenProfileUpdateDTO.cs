namespace Capstone.DTOs.StudentProfile
{
    public class StudenProfileUpdateDTO
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public IFormFile FormFile { get; set; } = null;
    }
}
