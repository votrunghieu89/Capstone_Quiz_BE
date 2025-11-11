namespace Capstone.DTOs.Group
{
    public class ViewQuizDTO
    {
        public int QGId { get; set; }
        public DeliveredQuizz DeliveredQuiz { get; set; } = new DeliveredQuizz();
        public string Title { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public DateTime ExpiredDate { get; set; }
        public string? Message { get; set; }
        public int? MaxAttempts { get; set; }
    }
    public class DeliveredQuizz
    {
        public int QuizId { get; set; }
        public string AvatarURL { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }

    }
}
