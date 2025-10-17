using Capstone.DTOs;
using Capstone.Model;

namespace Capstone.Repositories.Quizzes
{
    public interface IOfflineQuizRepository
    {
        Task<bool> StartOfflineQuiz(StartOfflineQuizDTO dto);
        Task<OfflineResultViewDTO> SubmitOfflineQuiz(FinishOfflineQuizDTO dto);
        Task<OfflineResultViewDTO?> GetOfflineResult(int studentId, int quizId);
        Task<bool> ProcessStudentAnswer(StudentAnswerSubmissionDTO dto);
    }
}
