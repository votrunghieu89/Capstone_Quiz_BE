
namespace Capstone.Repositories.Favourite
{
    public interface IFavouriteRepository
    {
        public Task<bool> InsertFavouriteQuizzes(int studentId, int quizzId);
        public Task<bool> RemoveFavouriteQuizzes(int quizzFID);
       

    }
}
