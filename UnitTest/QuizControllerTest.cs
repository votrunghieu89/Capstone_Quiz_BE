using Capstone.Controllers;
using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using Xunit;

namespace Capstone.UnitTest
{
    public class QuizControllerTest
    {
        private readonly QuizController _controller;
        private readonly Mock<IQuizRepository> _mockRepo;
        private readonly Mock<ILogger<QuizController>> _mockLogger;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IWebHostEnvironment> _mockEnv;

        public QuizControllerTest()
        {
            _mockRepo = new Mock<IQuizRepository>();
            _mockLogger = new Mock<ILogger<QuizController>>();
            _mockRedis = new Mock<IRedis>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            _controller = new QuizController(_mockLogger.Object, _mockRepo.Object, _mockRedis.Object, _mockConfig.Object, _mockEnv.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        }

        #region GetQuizById
        [Fact]
        public async Task GetQuizById_WhenCacheNull_CallsRepo_ReturnsOk()
        {
            _mockRedis.Setup(r => r.GetStringAsync("quiz_questions_1")).ReturnsAsync((string)null);
            _mockRepo.Setup(r => r.GetAllQuestionEachQuiz(1)).ReturnsAsync(new List<getQuizQuestionWithoutAnswerDTO> { new getQuizQuestionWithoutAnswerDTO() });

            var result = await _controller.GetQuizById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetQuizById_WhenCachePresent_Deserializes_ReturnsOk()
        {
            var items = new List<GetQuizQuestionsDTO> { new GetQuizQuestionsDTO() };
            _mockRedis.Setup(r => r.GetStringAsync("quiz_questions_1")).ReturnsAsync(JsonSerializer.Serialize(items));

            var result = await _controller.GetQuizById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetQuizById_InvalidCache_Returns500()
        {
            _mockRedis.Setup(r => r.GetStringAsync("quiz_questions_1")).ReturnsAsync("not-json");

            var result = await _controller.GetQuizById(1);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region getDetailOfAQuiz
        [Fact]
        public async Task GetDetailOfAQuiz_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.getDetailOfAQuiz(1)).ReturnsAsync((ViewDetailDTO)null);
            var result = await _controller.getDetailOfAQuiz(1);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetDetailOfAQuiz_Found_ReturnsOk_WithTransformedUrl()
        {
            var dto = new ViewDetailDTO { QuizId = 1, Title = "T", AvatarURL = "QuizImage\\abc.jpg" };
            _mockRepo.Setup(r => r.getDetailOfAQuiz(1)).ReturnsAsync(dto);

            var result = await _controller.getDetailOfAQuiz(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<ViewDetailDTO>(ok.Value);
            Assert.StartsWith("http://localhost/", val.AvatarURL);
            Assert.DoesNotContain("\\", val.AvatarURL);
        }
        #endregion

        #region GetAllQuizzes
        [Fact]
        public async Task GetAllQuizzes_InvalidPagination_ReturnsBadRequest()
        {
            var res = await _controller.GetAllQuizzes(new PaginationDTO { page = 0, pageSize = 10 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetAllQuizzes_FromCache_ReturnsOk()
        {
            var list = new List<ViewAllQuizDTO> { new ViewAllQuizDTO { AvatarURL = "A/B.jpg" } };
            _mockRedis.Setup(r => r.GetStringAsync("all_quizzes_page_1_size_10")).ReturnsAsync(JsonSerializer.Serialize(list));

            var res = await _controller.GetAllQuizzes(new PaginationDTO { page = 1, pageSize = 10 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetAllQuizzes_FromRepo_ReturnsNotFoundWhenEmpty()
        {
            _mockRedis.Setup(r => r.GetStringAsync("all_quizzes_page_1_size_10")).ReturnsAsync((string)null);
            _mockRepo.Setup(r => r.getAllQuizzes(1, 10)).ReturnsAsync(new List<ViewAllQuizDTO>());

            var res = await _controller.GetAllQuizzes(new PaginationDTO { page = 1, pageSize = 10 });
            Assert.IsType<NotFoundObjectResult>(res);
        }
        #endregion

        #region CheckQuizAnswers
        [Fact]
        public async Task CheckQuizAnswers_NullOrFalse_ReturnsBadRequest()
        {
            var dto = new CheckAnswerDTO();
            _mockRepo.Setup(r => r.checkAnswer(dto)).ReturnsAsync(false);
            var res = await _controller.CheckQuizAnswers(dto);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task CheckQuizAnswers_True_ReturnsOk()
        {
            var dto = new CheckAnswerDTO();
            _mockRepo.Setup(r => r.checkAnswer(dto)).ReturnsAsync(true);
            var res = await _controller.CheckQuizAnswers(dto);
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region GetCorrectAnswers
        [Fact]
        public async Task GetCorrectAnswers_Null_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.getCorrectAnswer(It.IsAny<GetCorrectAnswer>())).ReturnsAsync((RightAnswerDTO)null);
            var res = await _controller.GetCorrectAnswers(new GetCorrectAnswer());
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetCorrectAnswers_Found_ReturnsOk()
        {
            _mockRepo.Setup(r => r.getCorrectAnswer(It.IsAny<GetCorrectAnswer>())).ReturnsAsync(new RightAnswerDTO());
            var res = await _controller.GetCorrectAnswers(new GetCorrectAnswer());
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region UpdateImage
        [Fact]
        public async Task UpdateImage_NoNewFile_ReturnsOldImage()
        {
            _mockRepo.Setup(r => r.getOrlAvatarURL(1)).ReturnsAsync("QuizImage/old.jpg");
            var res = await _controller.UpdateImage(new QuizUpdateImageDTO { QuizId = 1, AvatarURL = null });
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Contains("QuizImage/old.jpg", ok.Value.ToString());
        }
        #endregion

        #region UpdateQuiz
        [Fact]
        public async Task UpdateQuiz_NullRepoReturn_Returns500()
        {
            _mockRepo.Setup(r => r.UpdateQuiz(It.IsAny<QuizUpdateDTO>(), It.IsAny<string>(), It.IsAny<int>()))
                     .ReturnsAsync((QuizUpdateDTO)null);
            var res = await _controller.UpdateQuiz(new QuizzUpdateControllerDTO());
            var obj = Assert.IsType<ObjectResult>(res);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region DeleteQuestion
        [Fact]
        public async Task DeleteQuestion_NotDeleted_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DeleteQuestion(1)).ReturnsAsync(false);
            var res = await _controller.DeleteQuestion(1);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task DeleteQuestion_Deleted_ReturnsOk()
        {
            _mockRepo.Setup(r => r.DeleteQuestion(1)).ReturnsAsync(true);
            var res = await _controller.DeleteQuestion(1);
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region ClearQuizCache
        [Fact]
        public async Task ClearQuizCache_Success_ReturnsOk()
        {
            var res = await _controller.ClearQuizCache(1);
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion
    }
}
