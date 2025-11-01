namespace Capstone.DTOs.Folder.Teacher
{
    public class QuizzFolderDTO
    {
        public int QuizzId { get; set; }
        public string Title { get; set; }
        public string AvatarURL { get; set; }
        public int TotalQuestion { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public string TeacherName { get; set; }
    }
}
