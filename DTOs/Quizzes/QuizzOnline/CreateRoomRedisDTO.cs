namespace Capstone.DTOs.Quizzes.QuizzOnline
{
    public class CreateRoomRedisDTO
    {
        public int QuizId { get; set; }
        public int TeacherId { get; set; }
        public string TeacherConnectionId { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalQuestion { get; set; } 
        public DateTime StartDate { get; set; }
    }
}
