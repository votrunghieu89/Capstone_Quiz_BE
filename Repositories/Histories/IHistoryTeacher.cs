using Capstone.DTOs.Reports.Teacher;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Repositories.Histories
{
    public interface IHistoryTeacher
    {
        // Offline Quiz
        public Task<List<DeliveredQuizzDTO>> DeliveredQuizz(int teacherId); // trả về Tên quiz, quizId, số người làm, thời gian  kết thúc và status
        public Task<bool> checkExpiredTime(int quizzId, int QGId); // check xem quizz đã hết hạn chưa    
        public Task<DetailOfQuizDTO> ReportDetailOffline(int QGId, int quizzId); // trả về Tên quiz, số người làm, ngày bắt đầy, ngày \kết thúc và status, ai tổ chức, status, highest score, lowest score, average score
        public Task<ExpiredEnum> ChangeExpiredTime(int QGId, int quizzId, DateTime newExpiredTime);
        public Task<bool> EndNow(int groupId, int quizzId); // update Status = Completed, ko cho hs làm nx
        public Task<List<ViewStudentHistoryDTO>> GetOfflineResult(int quizzId, int QGId, int groupId); // Trả về rank, % câu đúng, % câu sai, Điểm số cuối và tên hs, Tổng số câu 
        public Task<List<ViewQuestionHistoryDTO>> ViewQuestionHistory(int quizId, int QGId, int groupId); // Trả về nội dung câu hỏi, tổng số câu trả lời, số câu đúng, số câu sai, tỉ lệ % câu đúng
        public Task<DetailOfQuestionDTO> ViewDetailOfQuestion(int questionId); // Trả về nội dung câu hỏi, thời gian làm, các lựa chọn, đáp án đúng
        public Task<bool> ChangeReportName(int reportId, string newReportName); // đổi tên báo cáo

        //Online Quiz
        //public Task<List<string>> GetOnlineQuiz(int teacherId); // trả về danh sách tên các quiz online của giáo viên //  lấy từ bảng OnlineReport /
        //public Task<string> ReportDetailOnline(int quiizId); // trả về tên quiz, số người làm, ngày bắt đầy, ngày kết thúc, ai tổ chức, highest score, lowest score, average score
        //public Task<string> ViewOnlineQuestionHistory(int quizId); // trả về nội dung câu hỏi, tổng số câu trả lời, số câu đúng, số câu sai, tỉ lệ % câu đúng
        // public Task<string> ViewOnlineResult(int quiizId, 
        //public Task<List<string>> GetOnlineResult(int quizId,int OnlReportId) 

    }
}
