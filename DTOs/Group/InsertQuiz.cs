namespace Capstone.DTOs.Group
{
    public class InsertQuiz
    {
        public int QuizId { get; set; }
        public int GroupId { get; set; }
        public string? Message { get; set; }
        public DateTime ExpiredTime { get; set; }
    }
}
