namespace Capstone.DTOs.Reports.Student
{
    /// <summary>
    /// Request DTO for getting detailed information about a completed quiz
    /// </summary>
    public class DetailOfCompletedQuizRequest
    {
        public int StudentId { get; set; }
        public int QuizId { get; set; }
        public DateTime CreateAt { get; set; }
    }
}