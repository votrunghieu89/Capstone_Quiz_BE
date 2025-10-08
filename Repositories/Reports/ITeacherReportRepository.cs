using Capstone.DTOs.Reports.Teacher;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Repositories.Histories
{
    public interface ITeacherReportRepository
    {
        // Offline Quiz
       
        public Task<bool> checkExpiredTime(int quizzId, int QGId); // check xem quizz đã hết hạn chưa    
        public Task<ExpiredEnum> ChangeExpiredTime(int QGId, int quizzId, DateTime newExpiredTime);
        public Task<bool> EndNow(int groupId, int quizzId); // update Status = Completed, ko cho hs làm nx

        // Offline Quiz
        public Task<List<ViewAllOfflineReportDTO>> GetOfflineQuizz(int teacherId); // trả về Tên quiz, quizId, số người làm, thời gian  kết thúc và status
        public Task<ViewOfflineDetailReportEachQuizDTO> OfflineDetailReportEachQuiz(int OfflineReportId, int quizzId); // trả về Tên quiz, số người làm, ngày bắt đầy, ngày \kết thúc và status, ai tổ chức, status, highest score, lowest score, average score
        public Task<List<ViewOfflineStudentReportEachQuizDTO>> OfflineStudentReportEachQuiz(int quizzId, int QGId, int groupId); // Trả về rank, % câu đúng, % câu sai, Điểm số cuối và tên hs, Tổng số câu 
        public Task<List<ViewOfflineQuestionReportEachQuizDTO>> OfflineQuestionReportEachQuiz(int quizId, int QGId, int groupId); // Trả về nội dung câu hỏi, tổng số câu trả lời, số câu đúng, số câu sai, tỉ lệ % câu đúng
        public Task<bool> ChangeOfflineReport(int OfflineReportId, string newReportName); // đổi tên báo cáo
        //Online Quiz
        public Task<List<ViewAllOnlineReportDTO>> GetOnlineQuiz(int teacherId); // trả về danh sách tên các quiz online của giáo viên //  lấy từ bảng OnlineReport /
        public Task<ViewOnlineDetailReportEachQuizDTO> OnlineDetailReportEachQuiz(int quizId, int OnlineReportId); // trả về tên quiz, số người làm, ngày bắt đầy, ai tổ chức, highest score, lowest score, average score
        public Task<List<ViewOnlineStudentReportEachQuizDTO>> OnlineStudentReportEachQuiz(int quizId, int OnlineReportId); // trả về nội dung câu hỏi, tổng số câu trả lời, số câu đúng, số câu sai, tỉ lệ % câu đúng
        public Task<List<ViewOnlineQuestionReportEachQuizDTO>> OnlineQuestionReportEachQuiz(int quizId, int OnlineReportId); // trả về rank, % câu đúng, % câu sai, Điểm số cuối và tên hs, Tổng số câu
        public Task<bool> ChangeOnlineReportName(int onlineReportId, string newReportName); // đổi tên báo cáo

        //
        public Task<DetailOfQuestionDTO> ViewDetailOfQuestion(int questionId); // Trả về nội dung câu hỏi, thời gian làm, các lựa chọn, đáp án đúng
    }
}
