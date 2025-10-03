namespace Capstone.DTOs.Group
{
    public class ViewQuizDTO
    {
        public int quizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string DateCreated { get; set; } = string.Empty;
        public string? Message { get; set; }
    }
}
