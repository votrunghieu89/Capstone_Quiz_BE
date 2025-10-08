namespace Capstone.DTOs.Reports.Teacher.OnlineReport
{
    public class ViewOnlineStudentReportEachQuizDTO
    {
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int? CorrectCount { get; set; }
        public int ? WrongCount { get; set; }
        public int ? TotalQuestion { get; set; }
        public int ? Rank { get; set; }
    }
}
