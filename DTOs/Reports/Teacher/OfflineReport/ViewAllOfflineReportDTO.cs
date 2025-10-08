namespace Capstone.DTOs.Reports.Teacher.OfflineReport
{
    public class ViewAllOfflineReportDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<DeliveredQuizzDetailDTO> Quizzes { get; set; } = new List<DeliveredQuizzDetailDTO>();
    }
    public class DeliveredQuizzDetailDTO
    {
        public int OfflineReportId { get; set; }
        public int QuizzId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
