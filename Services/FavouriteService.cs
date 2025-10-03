using Capstone.DTOs.Favourite;
using Capstone.Repositories.Favourite;

using Capstone.Database;
using Microsoft.EntityFrameworkCore;
using Capstone.Model;
using Google.Apis.Upload;
using System.Numerics;
namespace Capstone.Services
{
    public class FavouriteService : IFavouriteRepository
    {
        public readonly AppDbContext _context;
        public readonly ILogger<FavouriteService> _logger;

        public FavouriteService(AppDbContext context, ILogger<FavouriteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GetAllFavouriteQuizzesDTO>> GetAllFavouriteQuizzes()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> InsertFavouriteQuizzes(int studentId, int quizzId)
        {
            try
            {
                var existsPC = await _context.studentProfiles
                                                           .AnyAsync(ps => ps.StudentId == studentId);
                var existsQ = await _context.quizzes
                                                            .AnyAsync(q => q.QuizId == quizzId);
                if (!existsPC)
                {
                    _logger.LogWarning("StudentID {studentId} does not exist in ProfileStudent ", studentId);
                    return false;
                }
                if (!existsQ)
                {
                    _logger.LogWarning("QuizzID {quizzID} does not exist in Quizzes ", quizzId);
                    return false;
                }

                QuizzFavouriteModel quizzFavouriteModel = new QuizzFavouriteModel() { 
                    StudentId = studentId,
                    QuizId = quizzId                    
                };
                await _context.quizzFavourites.AddAsync(quizzFavouriteModel);
                int check = await _context.SaveChangesAsync();
                if (check > 0)
                {
                    _logger.LogInformation("Insert Quizz Favoirite succesfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not insert Quizz Favoirite ");
                return false;
            }
            return false;
        }

        public async Task<bool> RemoveFavouriteQuizzes(int quizzFID)
        {
            try
            {
                int checkDeleteQuizzF = await _context.quizzFavourites.
                    Where(qf => qf.FavouriteId == quizzFID).ExecuteDeleteAsync();
                if (checkDeleteQuizzF > 0)
                {
                    _logger.LogInformation("Delete Quizz Favoirite succesfully");
                    return true;
                }
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Can not insert Quizz Favoirite ");
                return false;
            }
            return false;
        }
    }
}
