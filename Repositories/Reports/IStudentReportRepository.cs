namespace Capstone.Repositories.Histories
{
    public interface IStudentReportRepository
    {
        // Hiển thị các quizz đã làm lấy từ bảng offline result
        // click vào Detail thì hiển thị Tên quizz, số câu đúng, số câu sai, điểm số, thời gian làm bài, hiển thị chi tiết đáp án
        public Task<List<string>> GetAllCompletedQuizzes(int studentId); // trả về CreateAt nữa để xét thêm time
        public Task<string> DetailOfCompletedQuiz(int studentId, int quizId, DateTime CreateAt);
    }
}
