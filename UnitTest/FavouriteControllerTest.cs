using Capstone.Controllers;
using Capstone.DTOs;
using Capstone.ENUMs;
using Capstone.Repositories;
using Capstone.Repositories.Favourite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Capstone.UnitTest
{
    public class FavouriteControllerTest
    {
        private readonly FavouriteController _controller;
        private readonly Mock<IFavouriteRepository> _mockRepo;
        private readonly Mock<ILogger<FavouriteController>> _mockLogger;
        private readonly Mock<IAWS> _mockAWS;

        public FavouriteControllerTest()
        {
            _mockRepo = new Mock<IFavouriteRepository>();
            _mockLogger = new Mock<ILogger<FavouriteController>>();
            _mockAWS = new Mock<IAWS>();

            _controller = new FavouriteController(
                _mockLogger.Object, 
                _mockRepo.Object, 
                _mockAWS.Object
            );
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        }

        #region GetAllFavouriteQuizzes
        [Fact]
        public async Task GetAllFavouriteQuizzes_WhenDataExists_ReturnsOkWithTransformedUrls()
        {
            // Arrange
            int accountId = 1;
            var data = new List<ViewFavouriteDTO>
            {
                new ViewFavouriteDTO { QuizId = 10, Title = "Quiz 1", AvatarURL = "quiz/img1.jpg", CreatedBy = "a@a.com", TotalQuestions = 5 },
                new ViewFavouriteDTO { QuizId = 20, Title = "Quiz 2", AvatarURL = "quiz/img2.jpg", CreatedBy = "b@b.com", TotalQuestions = 7 }
            };
            _mockRepo.Setup(r => r.GetAllFavouriteQuizzes(accountId)).ReturnsAsync(data);
            _mockAWS.Setup(a => a.ReadImage("quiz/img1.jpg")).ReturnsAsync("https://bucket.s3.ap-southeast-2.amazonaws.com/quiz/img1.jpg");
            _mockAWS.Setup(a => a.ReadImage("quiz/img2.jpg")).ReturnsAsync("https://bucket.s3.ap-southeast-2.amazonaws.com/quiz/img2.jpg");

            // Act
            var result = await _controller.GetAllFavouriteQuizzes(accountId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<ViewFavouriteDTO>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.StartsWith("https://", list[0].AvatarURL);
            Assert.Contains("s3.ap-southeast-2.amazonaws.com", list[0].AvatarURL);
            Assert.StartsWith("https://", list[1].AvatarURL);
        }

        [Fact]
        public async Task GetAllFavouriteQuizzes_WhenNull_ReturnsNotFound()
        {
            // Arrange
            int accountId = 99;
            _mockRepo.Setup(r => r.GetAllFavouriteQuizzes(accountId)).ReturnsAsync((List<ViewFavouriteDTO>)null);

            // Act
            var result = await _controller.GetAllFavouriteQuizzes(accountId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region IsFavouriteExists
        [Fact]
        public async Task IsFavouriteExists_WhenTrue_ReturnsOkTrue()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.IsFavouriteExists(accountId, quizId)).ReturnsAsync(true);

            // Act
            var result = await _controller.IsFavouriteExists(accountId, quizId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Assert.IsType<bool>(ok.Value));
        }

        [Fact]
        public async Task IsFavouriteExists_WhenFalse_ReturnsOkFalse()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.IsFavouriteExists(accountId, quizId)).ReturnsAsync(false);

            // Act
            var result = await _controller.IsFavouriteExists(accountId, quizId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.False(Assert.IsType<bool>(ok.Value));
        }
        #endregion

        #region InsertFavouriteQuizzes
        [Fact]
        public async Task InsertFavouriteQuizzes_OnSuccess_ReturnsOk()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.InsertFavouriteQuizzes(accountId, quizId))
                     .ReturnsAsync(FavouriteEnum.InsertEnum.Success);

            // Act
            var result = await _controller.InsertFavouriteQuizzes(accountId, quizId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task InsertFavouriteQuizzes_OnAlreadyExist_ReturnsConflict()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.InsertFavouriteQuizzes(accountId, quizId))
                     .ReturnsAsync(FavouriteEnum.InsertEnum.AlreadyExist);

            // Act
            var result = await _controller.InsertFavouriteQuizzes(accountId, quizId);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task InsertFavouriteQuizzes_OnFailed_ReturnsNotFound()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.InsertFavouriteQuizzes(accountId, quizId))
                     .ReturnsAsync(FavouriteEnum.InsertEnum.Failed);

            // Act
            var result = await _controller.InsertFavouriteQuizzes(accountId, quizId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task InsertFavouriteQuizzes_OnAccountNull_ReturnsNotFound()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.InsertFavouriteQuizzes(accountId, quizId))
                     .ReturnsAsync(FavouriteEnum.InsertEnum.AccountNull);

            // Act
            var result = await _controller.InsertFavouriteQuizzes(accountId, quizId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task InsertFavouriteQuizzes_OnQuizNull_ReturnsNotFound()
        {
            // Arrange
            int accountId = 1, quizId = 2;
            _mockRepo.Setup(r => r.InsertFavouriteQuizzes(accountId, quizId))
                     .ReturnsAsync(FavouriteEnum.InsertEnum.QuizNull);

            // Act
            var result = await _controller.InsertFavouriteQuizzes(accountId, quizId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region RemoveFavouriteQuizzes
        [Fact]
        public async Task RemoveFavouriteQuizzes_WhenSuccess_ReturnsOkTrue()
        {
            // Arrange
            int favId = 100;
            _mockRepo.Setup(r => r.RemoveFavouriteQuizzes(favId)).ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveFavouriteQuizzes(favId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Assert.IsType<bool>(ok.Value));
        }

        [Fact]
        public async Task RemoveFavouriteQuizzes_WhenFailed_ReturnsServerError()
        {
            // Arrange
            int favId = 200;
            _mockRepo.Setup(r => r.RemoveFavouriteQuizzes(favId)).ReturnsAsync(false);

            // Act
            var result = await _controller.RemoveFavouriteQuizzes(favId);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion
    }
}
