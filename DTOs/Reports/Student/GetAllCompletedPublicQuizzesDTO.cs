namespace Capstone.DTOs.Reports.Student
{
    public class GetAllCompletedPublicQuizzesDTO
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public string AvatarURL { get; set; }
        public DateTime CompletedAt { get; set; }
        public string createBy { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public DateTime CreatAt { get; set; }

    }
}
