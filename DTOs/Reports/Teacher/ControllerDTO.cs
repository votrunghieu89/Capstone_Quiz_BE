namespace Capstone.DTOs.Reports.Teacher
{
    // Common Request DTOs
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

    public class ViewReportDTO
    {
        public int quizId { get; set; }
        public int qgId { get; set; }
        public int groupId { get; set; }
    }

    // Offline Report Request DTOs
    public class ChangeOfflineReportNameRequest
    {
        public int OfflineReportId { get; set; }
        public string NewReportName { get; set; } = string.Empty;
    }

    public class OfflineDetailReportRequest
    {
        public int OfflineReportId { get; set; }
        public int QuizId { get; set; }
    }

    public class OfflineStudentReportRequest
    {
        public int QuizId { get; set; }
        public int QGId { get; set; }
        public int GroupId { get; set; }
    }

    public class OfflineQuestionReportRequest
    {
        public int QuizId { get; set; }
        public int QGId { get; set; }
        public int GroupId { get; set; }
    }

    // Online Report Request DTOs
    public class ChangeOnlineReportNameRequest
    {
        public int OnlineReportId { get; set; }
        public string NewReportName { get; set; } = string.Empty;
    }

    public class OnlineDetailReportRequest
    {
        public int QuizId { get; set; }
        public int OnlineReportId { get; set; }
    }

    public class OnlineStudentReportRequest
    {
        public int QuizId { get; set; }
        public int OnlineReportId { get; set; }
    }

    public class OnlineQuestionReportRequest
    {
        public int QuizId { get; set; }
        public int OnlineReportId { get; set; }
    }
}