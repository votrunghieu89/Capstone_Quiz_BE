namespace Capstone.DTOs.Gemini
{
    public class Gemini_InputDTO
    {
        public int TeacherId { get; set; }
        public int FolderId { get; set; }
        public int? TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public IFormFile? AvatarURL { get; set; }
        public int NumberOfQuestion { get; set; }
        public int Score { get; set; }
        public int Time { get; set; }
        public IFormFile formFile { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
