using Capstone.DTOs;
using Capstone.Model;

namespace Capstone.Repositories.Quizzes
{
    public interface IOfflineQuizRepository
    {
        Task<bool> StartOfflineQuiz(StartOfflineQuizDTO dto);
        Task<OfflineResultViewDTO> SubmitOfflineQuiz(FinishOfflineQuizDTO dto, int accountId , string IpAddress);
        Task<OfflineResultViewDTO?> GetOfflineResult(int studentId, int quizId);
        Task<bool> ProcessStudentAnswer(StudentAnswerSubmissionDTO dto);
    }
}
