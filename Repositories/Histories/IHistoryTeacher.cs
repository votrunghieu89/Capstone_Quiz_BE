namespace Capstone.Repositories.Histories
{
    public interface IHistoryTeacher
    {
        public Task<bool> DeliveredQuizz(int teacherId); // trả về Tên quiz, quizId, số người làm, thời gian  kết thúc và status
        public Task<bool> ReportDetail(int quizzId); // trả về Tên quiz, số người làm, ngày bắt đầy, ngày \kết thúc và status, ai tổ chức, status, highest score, lowest score, average score
        public Task<bool> ChangeExpiredTime(int quizzId, DateTime newExpiredTime); 
        public Task<bool> EndNow(int quizzId); // update Status = Completed, ko cho hs làm nx
        public Task<bool> OfflineResult(int abc); // Trả về rank, % câu đúng, % câu sai, Điểm số cuối và tên hs, Tổng số câu 
        public Task<bool> ChangeReportName(int reportId, string newReportName); // đổi tên báo cáo
        // Check xem quizz hết hạn chưa
    }
}
