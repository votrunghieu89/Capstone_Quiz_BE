namespace Capstone.DTOs.Reports.Teacher.OnlineReport
{
    public class ViewOnlineQuestionReportEachQuizDTO
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; } = string.Empty;
        public int TotalAnswers { get; set; }
        public int WrongCount { get; set; }
        public int CorrectCount { get; set; }
        public decimal PercentageCorrect { get; set; }
    }
}
