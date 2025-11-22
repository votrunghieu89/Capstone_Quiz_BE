using Capstone.Repositories;
using Capstone.Repositories.Favourite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavouriteController : ControllerBase
    {
        public readonly ILogger<FavouriteController> _logger;
        public readonly IFavouriteRepository _repository;
        private readonly IAWS _S3;
        public FavouriteController(ILogger<FavouriteController> logger, IFavouriteRepository repository, IAWS S3)
        {
            _logger = logger;
            _repository = repository;
            _S3 = S3;
        }

        // ===== GET METHODS =====
        [HttpGet("getAllFavouriteQuizzes/{accountId}")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> GetAllFavouriteQuizzes(int accountId)
        {
            var getAllFavouriteQuizzes = await _repository.GetAllFavouriteQuizzes(accountId);
            if (getAllFavouriteQuizzes == null)
            {
                return NotFound("No Favourite Quiz found for the given account ID.");
            }
            foreach (var quiz in getAllFavouriteQuizzes)
            {
                if (quiz.AvatarURL != null) {
                   quiz.AvatarURL = await _S3.ReadImage(quiz.AvatarURL);
                }
            }
            return Ok(getAllFavouriteQuizzes);
        }

     
        [HttpGet("isFavouriteExists")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> IsFavouriteExists(int accountId, int quizzId)
        {
            var isFavouriteExists = await _repository.IsFavouriteExists(accountId, quizzId);
            return Ok(isFavouriteExists);
            
        }

        // ===== POST METHODS =====
        [HttpPost("insertFavouriteQuiz")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> InsertFavouriteQuizzes(int accountId, int quizzId)
        {
            var insertFavouriteQuizzes = await _repository.InsertFavouriteQuizzes(accountId, quizzId);
            switch (insertFavouriteQuizzes) { 
                case ENUMs.FavouriteEnum.InsertEnum.Success:
                    return Ok("Insert Favourite Quiz successfully.");
                case ENUMs.FavouriteEnum.InsertEnum.AlreadyExist:
                    return Conflict("Favourite Quiz already exists.");
                case ENUMs.FavouriteEnum.InsertEnum.Failed:
                    return NotFound("Fail to add a quiz.");
                case ENUMs.FavouriteEnum.InsertEnum.AccountNull:
                    return NotFound("AccountId does not exist.");
                case ENUMs.FavouriteEnum.InsertEnum.QuizNull:
                    return NotFound("QuizId does not exist.");
                default:
                    return StatusCode(500, "An error occurred while inserting the Favourite Quiz.");
            }
        }

        // ===== DELETE METHODS =====
        [HttpDelete("removeFavouriteQuiz")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> RemoveFavouriteQuizzes(int quizzFID)
        {
            var removeFavouriteQuizzes = await _repository.RemoveFavouriteQuizzes(quizzFID);
            if (removeFavouriteQuizzes == false)
            {
                return StatusCode(500, "An error occurred while remove the Favourite Quiz.");
            }
            return Ok(removeFavouriteQuizzes);
        }
        [HttpDelete("removeFavouriteQuizInDetail")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> RemoveFavouriteQuizzesInDetail(int quizzID, int accountId )
        {
            var removeFavouriteQuizzes = await _repository.RemoveFavouriteQuizzesinDetail(quizzID, accountId);
            if (removeFavouriteQuizzes == false)
            {
                return StatusCode(500, "An error occurred while remove the Favourite Quiz.");
            }
            return Ok(removeFavouriteQuizzes);
        }
    }
}