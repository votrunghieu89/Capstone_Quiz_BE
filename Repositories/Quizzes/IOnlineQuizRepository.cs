using Capstone.DTOs.Quizzes.QuizzOnline;

namespace Capstone.Repositories.Quizzes
{
    public interface IOnlineQuizRepository
    {
        public Task<bool> InsertOnlineReport(InsertOnlineReportDTO insertOnlineReportDTO);
    }
}
