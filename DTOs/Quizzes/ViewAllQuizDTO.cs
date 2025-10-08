namespace Capstone.DTOs.Quizzes
{
    public class ViewAllQuizDTO
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AvatarURL { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
     
    }
}
