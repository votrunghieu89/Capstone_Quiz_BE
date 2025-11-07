namespace Capstone.DTOs
{
    public class OfflineResultViewDTO
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int CountAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestion { get; set; }
        public int Score { get; set; }
        public int? RANK { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Duration { get; set; }
    }

    public class StartOfflineQuizDTO
    {
        public int StudentId { get; set; }
        public int? QGId { get; set; } // Quizz_Group Id
        public int QuizId { get; set; }
        public DateTime StartTime { get; set; }
    }

    // DTO để nộp bài (Không đổi, thông tin chính lấy từ Cache)
    public class FinishOfflineQuizDTO
    {
        public int StudentId { get; set; }
        public int? QGId { get; set; }
        public int QuizId { get; set; }
        public DateTime EndTime { get; set; }
    }

    // gửi đáp án của TỪNG CÂU HỎI lên server
    public class StudentAnswerSubmissionDTO
    {
        public int StudentId { get; set; }
        public int QuizId { get; set; }
        public int? QGId { get; set; }
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
    }

    public class OfflineQuizCacheDTO
    {
        public int QuizId { get; set; }
        public int StudentId { get; set; }
        public int NumberOfCorrectAnswer { get; set; }
        public int NumberOfWrongAnswer { get; set; }
        public int TotalQuestion { get; set; }
        // Dùng HashSet để theo dõi câu hỏi đã được trả lời (tránh tính trùng)
        public HashSet<int> AnsweredQuestions { get; set; } = new HashSet<int>();
        // Danh sách các câu trả lời sai (được điền khi ProcessStudentAnswer)
        public List<WrongAnswerDTO> WrongAnswers { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Duration { get; set; }
        public int TotalScoreEarned { get; set; }  // tổng điểm học sinh đạt được
        public int TotalMaxScore { get; set; }
    }

    // DTO lưu chi tiết một câu trả lời sai
    public class WrongAnswerDTO
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public int? CorrectOptionId { get; set; }
    }
}