namespace Capstone.DTOs.Quizzes.QuizzOnline
{
    public class OnlineAnswerDTO
    {
        public string roomCode { get; set; }
        public string studentId { get; set; }
        public int quizId { get; set; }
        public int questionId { get; set; }
        public int? optionId { get; set; }
    }
}
