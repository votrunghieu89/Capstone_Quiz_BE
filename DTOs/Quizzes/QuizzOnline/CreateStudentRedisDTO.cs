namespace Capstone.DTOs.Quizzes.QuizzOnline
{
    public class CreateStudentRedisDTO
    {
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int Rank { get; set; }
        public List<InsertWrongAnswerDTO> WrongAnswerRedisDTOs { get; set; } = new List<InsertWrongAnswerDTO>();
    }
}
