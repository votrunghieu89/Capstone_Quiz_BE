namespace Capstone.DTOs.Reports.Teacher
{
    // Request DTOs for the controller
    public class CheckExpiredTimeRequest
    {
        public int QuizId { get; set; }
        public int QGId { get; set; }
    }

    public class ChangeExpiredTimeRequest
    {
        public int QuizId { get; set; }
        public int QGId { get; set; }
        public DateTime NewExpiredTime { get; set; }
    }

    public class EndNowRequest
    {
        public int QuizId { get; set; }
        public int GroupId { get; set; }
    }

    public class ChangeReportNameRequest
    {
        public int ReportId { get; set; }
        public string NewReportName { get; set; } = string.Empty;
    }

    public class ViewReportDTO
    {
        public int quizId { get; set; }
        public int qgId { get; set; }
        public int groupId { get; set; }
    }
}