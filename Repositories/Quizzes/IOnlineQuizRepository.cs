using Capstone.DTOs.Quizzes.QuizzOnline;

namespace Capstone.Repositories.Quizzes
{
    public interface IOnlineQuizRepository
    {
        public Task<bool> InsertOnlineReport(InsertOnlineReportDTO insertOnlineReportDTO, int accountId, string ipAddress);
        public Task<bool> updateLeaderBoard(string roomCode);
        public Task<bool> UpdateNumberOfParticipants(int quizId, int totalParticipant);



    }
}
