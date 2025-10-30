
using Capstone.Database;
using Capstone.DTOs;
using Capstone.Model;
using Capstone.Repositories.Favourite;
using Google.Apis.Upload;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using static Capstone.ENUMs.FavouriteEnum;
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

        public async Task<List<ViewFavouriteDTO>> GetAllFavouriteQuizzes(int accountId)
        {
            try
            {
                var favQuizzes = await (
                    from qf in _context.quizzFavourites
                    join q in _context.quizzes on qf.QuizId equals q.QuizId
                    join pc in _context.authModels on qf.AccountId equals pc.AccountId
                    join tp in _context.topics on q.TopicId equals tp.TopicId
                    where pc.AccountId == accountId
                    select new ViewFavouriteDTO
                    {
                        QuizId = q.QuizId,
                        Title = q.Title,
                        AvatarURL = q.AvatarURL,
                        CreatedBy = pc.Email,
                        TotalQuestions = _context.questions
                                                  .Count(ques => ques.QuizId == q.QuizId && ques.IsDeleted == false),
                        TopicName = tp.TopicName,
                        TotalParticipants = q.TotalParticipants
                    }
                ).ToListAsync();

                return favQuizzes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get all Favourite Quizzes");
                return new List<ViewFavouriteDTO>();
            }
        }

        public async Task<InsertEnum> InsertFavouriteQuizzes(int accoutId, int quizzId)
        {
            try
            {
                var checkQuiz = await _context.quizzes
                    .AnyAsync(q => q.QuizId == quizzId);
                if (checkQuiz == false)
                {
                    _logger.BeginScope("QuizId {quizzId} does not exist", quizzId);
                    return InsertEnum.QuizNull;
                }
                var checkAccount = await _context.authModels
                    .AnyAsync(a => a.AccountId == accoutId);
                if (checkAccount == false)
                {
                    _logger.BeginScope("AccountId {accoutId} does not exist", accoutId);
                    return InsertEnum.AccountNull;
                }
                var checkExist = await _context.quizzFavourites
                    .AnyAsync(qf => qf.AccountId == accoutId && qf.QuizId == quizzId);
                if (checkExist == true)
                {
                    _logger.BeginScope("QuizId {quizzId} already exists in favourites for AccountId {accoutId}", quizzId, accoutId);
                    return InsertEnum.AlreadyExist;
                }
                await _context.quizzFavourites.AddAsync(new QuizzFavouriteModel
                {
                    AccountId = accoutId,
                    QuizId = quizzId,
                    CreateAt = DateTime.Now
                });
                int checkInsertQuizzF = await _context.SaveChangesAsync();
                if (checkInsertQuizzF > 0)
                {
                    _logger.LogInformation("Insert Quizz Favoirite succesfully");
                    return InsertEnum.Success;
                }
                return InsertEnum.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not insert Quizz Favoirite ");
                return InsertEnum.Failed;
            }
        }

        public async Task<bool> IsFavouriteExists(int accountId, int quizzId)
        {
            try
            {
                bool isExist = await _context.quizzFavourites
                    .AnyAsync(qf => qf.AccountId == accountId && qf.QuizId == quizzId);
                if(isExist)
                {
                    _logger.LogInformation("Favourite exists for AccountId {accountId} and QuizId {quizzId}", accountId, quizzId);
                    return true;
                }
                else
                {
                    _logger.LogInformation("Favourite does not exist for AccountId {accountId} and QuizId {quizzId}", accountId, quizzId);
                    return false;
                }
            }   
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot check Favourite Quiz existence");
                return false;
            }
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
