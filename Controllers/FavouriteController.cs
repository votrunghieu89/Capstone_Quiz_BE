using Capstone.Repositories.Favourite;
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
        public FavouriteController(ILogger<FavouriteController> logger, IFavouriteRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpPost("insertFavouriteQuiz")]
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

        [HttpDelete("removeFavouriteQuiz")]
        public async Task<IActionResult> RemoveFavouriteQuizzes(int quizzFID)
        {
            var removeFavouriteQuizzes = await _repository.RemoveFavouriteQuizzes(quizzFID);
            if (removeFavouriteQuizzes == false)
            {
                return StatusCode(500, "An error occurred while remove the Favourite Quiz.");
            }
            return Ok(removeFavouriteQuizzes);
        }
        [HttpGet("getAllFavouriteQuizzes/{accountId}")]
        public async Task<IActionResult> GetAllFavouriteQuizzes(int accountId)
        {
            var getAllFavouriteQuizzes = await _repository.GetAllFavouriteQuizzes(accountId);
            if (getAllFavouriteQuizzes == null)
            {
                return NotFound("No Favourite Quiz found for the given account ID.");
            }
            foreach (var quiz in getAllFavouriteQuizzes)
            {
               quiz.AvatarURL = quiz.AvatarURL = $"{Request.Scheme}://{Request.Host}/{quiz.AvatarURL.Replace("\\", "/")}";
            }
            return Ok(getAllFavouriteQuizzes);
        }
    }
}