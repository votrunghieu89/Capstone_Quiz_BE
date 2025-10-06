

using Capstone.DTOs;
using static Capstone.ENUMs.FavouriteEnum;

namespace Capstone.Repositories.Favourite
{
    public interface IFavouriteRepository
    {
        public Task<InsertEnum> InsertFavouriteQuizzes(int studentId, int quizzId);
        public Task<bool> RemoveFavouriteQuizzes(int quizzFID);
        public Task<List<ViewFavouriteDTO>> GetAllFavouriteQuizzes(int accountId);
        public Task<bool> IsFavouriteExists(int accountId, int quizzId);

    }
}
