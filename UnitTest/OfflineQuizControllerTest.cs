using Capstone.Controllers;
using Capstone.DTOs;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static Capstone.ENUMs.OfflineQuizzEnum;
namespace Capstone.UnitTest
{
    public class OfflineQuizControllerTest
    {
        private readonly OfflineQuizController _controller;
        private readonly Mock<IOfflineQuizRepository> _mockRepo;
        private readonly Mock<ILogger<OfflineQuizController>> _mockLogger;

        public OfflineQuizControllerTest()
        {
            _mockRepo = new Mock<IOfflineQuizRepository>();
            _mockLogger = new Mock<ILogger<OfflineQuizController>>();
            _controller = new OfflineQuizController(_mockRepo.Object, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region POST start
        [Fact]
        public async Task StartQuiz_ReturnsOk_WhenRepoDoesNotThrow()
        {
            var dto = new StartOfflineQuizDTO { StudentId = 1, QGId = 10 };
            _mockRepo.Setup(r => r.StartOfflineQuiz(dto)).ReturnsAsync(CheckStartOfflineQuizz.Success);

            var result = await _controller.StartQuiz(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task StartQuiz_ReturnsBadRequest_OnException()
        {
            var dto = new StartOfflineQuizDTO { StudentId = 1, QGId = 10 };
            _mockRepo.Setup(r => r.StartOfflineQuiz(dto)).ThrowsAsync(new Exception("err"));

            var result = await _controller.StartQuiz(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region POST answer
        [Fact]
        public async Task SubmitAnswer_WhenSuccess_ReturnsOk()
        {
            var dto = new StudentAnswerSubmissionDTO { StudentId = 1, QuizId = 2, QGId = 3, QuestionId = 4, SelectedOptionId = 5 };
            _mockRepo.Setup(r => r.ProcessStudentAnswer(dto)).ReturnsAsync(true);

            var result = await _controller.SubmitAnswer(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitAnswer_WhenRepoReturnsFalse_ReturnsBadRequest()
        {
            var dto = new StudentAnswerSubmissionDTO { StudentId = 1, QuizId = 2, QGId = 3, QuestionId = 4, SelectedOptionId = 5 };
            _mockRepo.Setup(r => r.ProcessStudentAnswer(dto)).ReturnsAsync(false);

            var result = await _controller.SubmitAnswer(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitAnswer_OnException_Returns500()
        {
            var dto = new StudentAnswerSubmissionDTO { StudentId = 1, QuizId = 2, QGId = 3, QuestionId = 4, SelectedOptionId = 5 };
            _mockRepo.Setup(r => r.ProcessStudentAnswer(dto)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.SubmitAnswer(dto);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region POST submit
        [Fact]
        public async Task SubmitQuiz_WhenSuccess_ReturnsOk()
        {
            var dto = new FinishOfflineQuizDTO { StudentId = 1, QGId = 2, QuizId = 3 };
            var view = new OfflineResultViewDTO { QuizId = 3, Score = 90 };
            _mockRepo.Setup(r => r.SubmitOfflineQuiz(dto, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(view);

            var result = await _controller.SubmitQuiz(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<OfflineResultViewDTO>(ok.Value);
        }

        [Fact]
        public async Task SubmitQuiz_WhenRepoReturnsNull_ReturnsBadRequest()
        {
            var dto = new FinishOfflineQuizDTO { StudentId = 1, QGId = 2, QuizId = 3 };
            _mockRepo.Setup(r => r.SubmitOfflineQuiz(dto, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((OfflineResultViewDTO)null);

            var result = await _controller.SubmitQuiz(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitQuiz_OnException_ReturnsBadRequest()
        {
            var dto = new FinishOfflineQuizDTO { StudentId = 1, QGId = 2, QuizId = 3 };
            _mockRepo.Setup(r => r.SubmitOfflineQuiz(dto, It.IsAny<int>(), It.IsAny<string>())).ThrowsAsync(new Exception("e"));

            var result = await _controller.SubmitQuiz(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region GET result
        [Fact]
        public async Task GetResult_WhenFound_ReturnsOk()
        {
            //_mockRepo.Setup(r => r.GetOfflineResult(1, 2)).ReturnsAsync(new OfflineResultViewDTO { QuizId = 2 });

            var result = await _controller.GetResult(1, 2, null);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetResult_WhenNotFound_ReturnsNotFound()
        {
            //_mockRepo.Setup(r => r.GetOfflineResult(1, 2)).ReturnsAsync((OfflineResultViewDTO)null);

            var result = await _controller.GetResult(1, 2 , null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetResult_OnException_ReturnsBadRequest()
        {
            _mockRepo.Setup(r => r.GetOfflineResult(1, 2, null)).ThrowsAsync(new Exception("e"));

            var result = await _controller.GetResult(1, 2, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion
    }
}
