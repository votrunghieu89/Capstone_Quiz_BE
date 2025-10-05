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
        public async Task<IActionResult> InsertFavouriteQuizzes(int studentId, int quizzId)
        {
            var insertFavouriteQuizzes = await _repository.InsertFavouriteQuizzes(studentId, quizzId);
            if (insertFavouriteQuizzes == null)
            {
                return StatusCode(500, "An error occurred while insert the Favourite Quiz.");
            }
            return Ok(insertFavouriteQuizzes);
        }

        [HttpDelete("removeFavouriteQuiz")]
        public async Task<IActionResult> RemoveFavouriteQuizzes(int quizzFID)
        {
            var removeFavouriteQuizzes = await _repository.RemoveFavouriteQuizzes(quizzFID);
            if (removeFavouriteQuizzes == null)
            {
                return StatusCode(500, "An error occurred while remove the Favourite Quiz.");
            }
            return Ok(removeFavouriteQuizzes);
        }
    }
}