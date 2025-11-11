namespace Capstone.DTOs
{
    public class ViewFavouriteDTO
    {
        public int FavouriteId { get; set; }
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AvatarURL { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public string? CreatedBy { get; set; }
    }
}
