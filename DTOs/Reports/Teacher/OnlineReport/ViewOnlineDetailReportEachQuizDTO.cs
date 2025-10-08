namespace Capstone.DTOs.Reports.Teacher.OnlineReport
{
    public class ViewOnlineDetailReportEachQuizDTO
    {
        public int TotalStudent { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalQuestion { get; set; }
        public DateTime CreateAt { get; set; }
        public string  ReportName { get; set; } = string.Empty;
        public string CreateBy  { get; set; } = string.Empty;
    }
}
