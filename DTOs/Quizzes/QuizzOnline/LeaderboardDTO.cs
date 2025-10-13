namespace Capstone.DTOs.Quizzes.QuizzOnline
{
    public class LeaderboardDTO
    {
        public string StudentId { get; set; } = default!;
        public string StudentName { get; set; } = default!;
        public int Score { get; set; }
        public int Rank { get; set; }
    }
}
