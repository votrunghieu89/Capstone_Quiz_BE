using Capstone.Controllers;
using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Capstone.Repositories.Quizzes;
using Capstone.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Capstone.UnitTest
{
    public class OnlineQuizControllerTest
    {
        private readonly OnlineQuizController _controller;
        private readonly Mock<IOnlineQuizRepository> _mockOnlineRepo;
        private readonly Mock<IQuizRepository> _mockQuizRepo;
        private readonly Mock<ILogger<OnlineQuizController>> _mockLogger;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<IHubContext<QuizHub>> _mockHub; // Type reference only; not used directly

        public OnlineQuizControllerTest()
        {
            _mockOnlineRepo = new Mock<IOnlineQuizRepository>();
            _mockQuizRepo = new Mock<IQuizRepository>();
            _mockLogger = new Mock<ILogger<OnlineQuizController>>();
            _mockRedis = new Mock<IRedis>();
            _mockHub = new Mock<IHubContext<QuizHub>>();

            _controller = new OnlineQuizController(_mockOnlineRepo.Object, _mockLogger.Object, _mockRedis.Object, _mockQuizRepo.Object, _mockHub.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task InsertOnlineReport_Success_ReturnsOk()
        {
            int roomCode = 123;
            var room = new CreateRoomRedisDTO { QuizId = 1, TeacherId = 2, TeacherConnectionId = "t" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}")).ReturnsAsync(JsonConvert.SerializeObject(room));
            _mockRedis.Setup(r => r.SMembersAsync($"quiz:room:{roomCode}:student")).ReturnsAsync(new List<string> { "room:123:student:10" });
            var student = new CreateStudentRedisDTO { StudentName = "A", TotalQuestions = 10, Rank = 1 };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}:student:10")).ReturnsAsync(JsonConvert.SerializeObject(student));
            _mockRedis.Setup(r => r.HGetAllAsync($"quiz:room:{roomCode}:student:10:detail")).ReturnsAsync(new Dictionary<string, string> { { "Score", "100" }, { "CorrectCount", "10" }, { "WrongCount", "0" }, { "Rank", "1" } });
            _mockOnlineRepo.Setup(r => r.InsertOnlineReport(It.IsAny<InsertOnlineReportDTO>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);

            var result = await _controller.InsertOnlineReport(roomCode);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task InsertOnlineReport_RepoFalse_ReturnsBadRequest()
        {
            int roomCode = 123;
            var room = new CreateRoomRedisDTO { QuizId = 1, TeacherId = 2 };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}")).ReturnsAsync(JsonConvert.SerializeObject(room));
            _mockRedis.Setup(r => r.SMembersAsync($"quiz:room:{roomCode}:student")).ReturnsAsync(new List<string>());
            _mockOnlineRepo.Setup(r => r.InsertOnlineReport(It.IsAny<InsertOnlineReportDTO>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(false);

            var result = await _controller.InsertOnlineReport(roomCode);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task InsertOnlineReport_OnException_Returns500()
        {
            int roomCode = 123;
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}")).ThrowsAsync(new Exception("e"));

            var result = await _controller.InsertOnlineReport(roomCode);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task CheckOnlineAnswer_Correct_IncrementsCounters_ReturnsOkTrue()
        {
            var dto = new OnlineAnswerDTO { roomCode = "1", studentId = "10", quizId = 2, questionId = 3, optionId = 4 };
            _mockQuizRepo.Setup(r => r.checkAnswer(It.IsAny<CheckAnswerDTO>())).ReturnsAsync(true);

            var result = await _controller.CheckOnlineAnswer(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic val = ok.Value;
            Assert.True((bool)val.isCorrect);
            _mockRedis.Verify(r => r.HashIncrementAsync(It.IsAny<string>(), "Score", 10), Times.Once);
            _mockRedis.Verify(r => r.HashIncrementAsync(It.IsAny<string>(), "CorrectCount", 1), Times.Once);
            _mockRedis.Verify(r => r.ZIncrByAsync(It.IsAny<string>(), dto.studentId, 10), Times.Once);
        }

        [Fact]
        public async Task CheckOnlineAnswer_Wrong_AddsWrongAnswer_ReturnsOkFalse()
        {
            var dto = new OnlineAnswerDTO { roomCode = "1", studentId = "10", quizId = 2, questionId = 3, optionId = 4 };
            _mockQuizRepo.Setup(r => r.checkAnswer(It.IsAny<CheckAnswerDTO>())).ReturnsAsync(false);
            _mockQuizRepo.Setup(r => r.getCorrectAnswer(It.IsAny<GetCorrectAnswer>())).ReturnsAsync(new RightAnswerDTO { OptionId = 6, OptionContent = "c" });
            _mockRedis.Setup(r => r.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new CreateStudentRedisDTO())) ;

            var result = await _controller.CheckOnlineAnswer(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic val = ok.Value;
            Assert.False((bool)val.isCorrect);
            _mockRedis.Verify(r => r.HashIncrementAsync(It.IsAny<string>(), "WrongCount", 1), Times.Once);
            _mockRedis.Verify(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CheckOnlineAnswer_OnException_Returns500()
        {
            var dto = new OnlineAnswerDTO { roomCode = "1", studentId = "10", quizId = 2, questionId = 3, optionId = 4 };
            _mockQuizRepo.Setup(r => r.checkAnswer(It.IsAny<CheckAnswerDTO>())).ThrowsAsync(new Exception("e"));

            var result = await _controller.CheckOnlineAnswer(dto);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
    }
}
