namespace Capstone.DTOs.Quizzes
{
    public class QuizDetailHPDTO
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarURL { get; set; }
        public int? TotalParticipants { get; set; }
        public int? TotalQuestions { get; set; }
        public string? CreateBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
