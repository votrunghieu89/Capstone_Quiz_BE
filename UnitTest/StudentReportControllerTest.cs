using Capstone.Controllers;
using Capstone.DTOs.Reports.Student;
using Capstone.Repositories;
using Capstone.Repositories.Histories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Capstone.UnitTest
{
    public class StudentReportControllerTest
    {
        private readonly StudentReportController _controller;
        private readonly Mock<IStudentReportRepository> _mockRepo;
        private readonly Mock<ILogger<StudentReportController>> _mockLogger;
        private readonly Mock<IAWS> _mockAWS;

        public StudentReportControllerTest()
        {
            _mockRepo = new Mock<IStudentReportRepository>();
            _mockLogger = new Mock<ILogger<StudentReportController>>();
            _mockAWS = new Mock<IAWS>();

            _controller = new StudentReportController(
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

        [Fact]
        public async Task GetAllCompletedPublicQuizzes_InvalidId_BadRequest()
        {
            var res = await _controller.GetAllCompletedPublicQuizzes(0);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetAllCompletedPublicQuizzes_Ok_TransformsUrls()
        {
            var list = new List<GetAllCompletedPublicQuizzesDTO> { new GetAllCompletedPublicQuizzesDTO { AvatarURL = "quiz/B.jpg" } };
            _mockRepo.Setup(r => r.GetAllCompletedPublicQuizzes(1)).ReturnsAsync(list);
            _mockAWS.Setup(a => a.ReadImage("quiz/B.jpg")).ReturnsAsync("https://bucket.s3.ap-southeast-2.amazonaws.com/quiz/B.jpg");
            
            var res = await _controller.GetAllCompletedPublicQuizzes(1);
            
            var ok = Assert.IsType<OkObjectResult>(res);
            var val = Assert.IsType<List<GetAllCompletedPublicQuizzesDTO>>(ok.Value);
            Assert.StartsWith("https://", val[0].AvatarURL);
            Assert.Contains("s3.ap-southeast-2.amazonaws.com", val[0].AvatarURL);
        }

        [Fact]
        public async Task GetAllCompletedPrivateQuizzes_InvalidId_BadRequest()
        {
            var res = await _controller.GetAllCompletedPrivateQuizzes(0);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetAllCompletedPrivateQuizzes_Ok_TransformsUrls()
        {
            var list = new List<GetAllCompletedPrivateQuizzesDTO> { new GetAllCompletedPrivateQuizzesDTO { AvatarURL = "quiz/B.jpg" } };
            _mockRepo.Setup(r => r.GetAllCompletedPrivateQuizzes(1)).ReturnsAsync(list);
            _mockAWS.Setup(a => a.ReadImage("quiz/B.jpg")).ReturnsAsync("https://bucket.s3.ap-southeast-2.amazonaws.com/quiz/B.jpg");
            
            var res = await _controller.GetAllCompletedPrivateQuizzes(1);
            
            var ok = Assert.IsType<OkObjectResult>(res);
            var val = Assert.IsType<List<GetAllCompletedPrivateQuizzesDTO>>(ok.Value);
            Assert.StartsWith("https://", val[0].AvatarURL);
            Assert.Contains("s3.ap-southeast-2.amazonaws.com", val[0].AvatarURL);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuiz_NullRequest_BadRequest()
        {
            var res = await _controller.GetDetailOfCompletedQuiz(null);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuiz_InvalidParams_BadRequest()
        {
            var res = await _controller.GetDetailOfCompletedQuiz(new DetailOfCompletedQuizRequest { StudentId = 0, QuizId = 1, CreateAt = DateTime.UtcNow });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuiz_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DetailOfCompletedQuiz(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>()))
                     .ReturnsAsync((ViewDetailOfCompletedQuizDTO)null);
            var res = await _controller.GetDetailOfCompletedQuiz(new DetailOfCompletedQuizRequest { StudentId = 1, QuizId = 2, CreateAt = DateTime.UtcNow });
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuiz_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.DetailOfCompletedQuiz(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>()))
                     .ReturnsAsync(new ViewDetailOfCompletedQuizDTO());
            var res = await _controller.GetDetailOfCompletedQuiz(new DetailOfCompletedQuizRequest { StudentId = 1, QuizId = 2, CreateAt = DateTime.UtcNow });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuiz_RepoThrowsLocalizedMessage_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DetailOfCompletedQuiz(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>()))
                     .ThrowsAsync(new Exception("Không tìm thấy kết quả quiz này"));
            var res = await _controller.GetDetailOfCompletedQuiz(new DetailOfCompletedQuizRequest { StudentId = 1, QuizId = 2, CreateAt = DateTime.UtcNow });
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuizByPath_InvalidParams_BadRequest()
        {
            var res = await _controller.GetDetailOfCompletedQuizByPath(0, 1, DateTime.UtcNow);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuizByPath_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DetailOfCompletedQuiz(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>()))
                     .ReturnsAsync((ViewDetailOfCompletedQuizDTO)null);
            var res = await _controller.GetDetailOfCompletedQuizByPath(1, 2, DateTime.UtcNow);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetDetailOfCompletedQuizByPath_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.DetailOfCompletedQuiz(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>()))
                     .ReturnsAsync(new ViewDetailOfCompletedQuizDTO());
            var res = await _controller.GetDetailOfCompletedQuizByPath(1, 2, DateTime.UtcNow);
            Assert.IsType<OkObjectResult>(res);
        }
    }
}
