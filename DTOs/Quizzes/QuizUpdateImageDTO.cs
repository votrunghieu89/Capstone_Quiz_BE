namespace Capstone.DTOs.Quizzes
{
    public class QuizUpdateImageDTO
    {
        public int QuizId { get; set; }
        public IFormFile? AvatarURL { get; set; }
    }
}
