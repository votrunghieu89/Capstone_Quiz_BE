namespace Capstone.DTOs.Reports.Teacher.OnlineReport
{
    public class ViewAllOnlineReportDTO
    {
        public int OnlineReportId { get; set; }
        public int quizId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
