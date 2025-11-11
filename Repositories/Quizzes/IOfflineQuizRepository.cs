using Capstone.DTOs;
using Capstone.Model;
using static Capstone.ENUMs.OfflineQuizzEnum;
namespace Capstone.Repositories.Quizzes
{
    public interface IOfflineQuizRepository
    {
        Task<CheckStartOfflineQuizz> StartOfflineQuiz(StartOfflineQuizDTO dto);
        Task<OfflineResultViewDTO> SubmitOfflineQuiz(FinishOfflineQuizDTO dto, int accountId , string IpAddress);
        Task<OfflineResultDetailViewDTO?> GetOfflineResult(int studentId, int quizId , int? qgId);
        Task<bool> ProcessStudentAnswer(StudentAnswerSubmissionDTO dto);
    }
}
