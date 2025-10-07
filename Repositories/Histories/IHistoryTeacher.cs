using Capstone.DTOs.Reports.Teacher;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Repositories.Histories
{
    public interface IHistoryTeacher
    {
        public Task<List<DeliveredQuizzDTO>> DeliveredQuizz(int teacherId); // trả về Tên quiz, quizId, số người làm, thời gian  kết thúc và status
        public Task<bool> checkExpiredTime(int quizzId, int groupId); // check xem quizz đã hết hạn chưa    
        public Task<DetailOfQuizDTO> ReportDetail(int groupId, int quizzId); // trả về Tên quiz, số người làm, ngày bắt đầy, ngày \kết thúc và status, ai tổ chức, status, highest score, lowest score, average score
        public Task<ExpiredEnum> ChangeExpiredTime(int groupId, int quizzId, DateTime newExpiredTime);
        public Task<bool> EndNow(int groupId, int quizzId); // update Status = Completed, ko cho hs làm nx
        public Task<List<ViewStudentHistoryDTO>> GetOfflineResult(int quizzId); // Trả về rank, % câu đúng, % câu sai, Điểm số cuối và tên hs, Tổng số câu 
        public Task<List<ViewQuestionHistoryDTO>> ViewQuestionHistory(int quizzId); // Trả về nội dung câu hỏi, tổng số câu trả lời, số câu đúng, số câu sai, tỉ lệ % câu đúng
        public Task<DetailOfQuestionDTO> ViewDetailOfQuestion(int questionId); // Trả về nội dung câu hỏi, thời gian làm, các lựa chọn, đáp án đúng
        public Task<bool> ChangeReportName(int reportId, string newReportName); // đổi tên báo cáo
        // Check xem quizz hết hạn chưa
    }
}
