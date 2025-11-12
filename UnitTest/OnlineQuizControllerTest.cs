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
using System.Security.Claims;

namespace Capstone.UnitTest
{
    public class OnlineQuizControllerTest
    {
        private readonly OnlineQuizController _controller;
        private readonly Mock<IOnlineQuizRepository> _mockOnlineRepo;
        private readonly Mock<IQuizRepository> _mockQuizRepo;
        private readonly Mock<ILogger<OnlineQuizController>> _mockLogger;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<IHubContext<QuizHub>> _mockHub;

        public OnlineQuizControllerTest()
        {
            _mockOnlineRepo = new Mock<IOnlineQuizRepository>();
            _mockQuizRepo = new Mock<IQuizRepository>();
            _mockLogger = new Mock<ILogger<OnlineQuizController>>();
            _mockRedis = new Mock<IRedis>();
            _mockHub = new Mock<IHubContext<QuizHub>>();

            _controller = new OnlineQuizController(_mockOnlineRepo.Object, _mockLogger.Object, _mockRedis.Object, _mockQuizRepo.Object, _mockHub.Object);
            
            // Setup HttpContext with Claims for AccountId
            var claims = new List<Claim>
            {
                new Claim("AccountId", "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        [Fact]
        public async Task InsertOnlineReport_Success_ReturnsOk()
        {
            // Arrange
            int roomCode = 123;
            var room = new CreateRoomRedisDTO { QuizId = 1, TeacherId = 2, TeacherConnectionId = "t" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}"))
                .ReturnsAsync(JsonConvert.SerializeObject(room));
            _mockRedis.Setup(r => r.SMembersAsync($"quiz:room:{roomCode}:student"))
                .ReturnsAsync(new List<string> { "room:123:student:10" });
            
            var student = new CreateStudentRedisDTO 
            { 
                StudentName = "A", 
                TotalQuestions = 10,
                WrongAnswerRedisDTOs = new List<InsertWrongAnswerDTO>()
            };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}:student:10"))
                .ReturnsAsync(JsonConvert.SerializeObject(student));
            _mockRedis.Setup(r => r.HGetAllAsync($"quiz:room:{roomCode}:student:10:detail"))
                .ReturnsAsync(new Dictionary<string, string> 
                { 
                    { "Score", "100" }, 
                    { "CorrectCount", "10" }, 
                    { "WrongCount", "0" }, 
                    { "Rank", "1" } 
                });
            _mockOnlineRepo.Setup(r => r.InsertOnlineReport(It.IsAny<InsertOnlineReportDTO>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.InsertOnlineReport(roomCode);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task InsertOnlineReport_RepoFalse_ReturnsBadRequest()
        {
            // Arrange
            int roomCode = 123;
            var room = new CreateRoomRedisDTO { QuizId = 1, TeacherId = 2 };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}"))
                .ReturnsAsync(JsonConvert.SerializeObject(room));
            _mockRedis.Setup(r => r.SMembersAsync($"quiz:room:{roomCode}:student"))
                .ReturnsAsync(new List<string> { "room:123:student:10" });
            
            var student = new CreateStudentRedisDTO 
            { 
                StudentName = "A", 
                TotalQuestions = 10,
                WrongAnswerRedisDTOs = new List<InsertWrongAnswerDTO>()
            };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}:student:10"))
                .ReturnsAsync(JsonConvert.SerializeObject(student));
            _mockRedis.Setup(r => r.HGetAllAsync($"quiz:room:{roomCode}:student:10:detail"))
                .ReturnsAsync(new Dictionary<string, string> 
                { 
                    { "Score", "100" }, 
                    { "CorrectCount", "10" }, 
                    { "WrongCount", "0" }, 
                    { "Rank", "1" } 
                });
            _mockOnlineRepo.Setup(r => r.InsertOnlineReport(It.IsAny<InsertOnlineReportDTO>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.InsertOnlineReport(roomCode);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task InsertOnlineReport_OnException_Returns500()
        {
            // Arrange
            int roomCode = 123;
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{roomCode}"))
                .ThrowsAsync(new Exception("e"));

            // Act
            var result = await _controller.InsertOnlineReport(roomCode);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task CheckOnlineAnswer_Correct_IncrementsCounters_ReturnsOkTrue()
        {
            // Arrange
            var dto = new OnlineAnswerDTO 
            { 
                roomCode = "1", 
                studentId = "10", 
                quizId = 2, 
                questionId = 3, 
                optionId = 4 
            };
            
            // Setup: Correct answer không có trong cache
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}: question_{dto.questionId}: option_{dto.optionId}"))
                .ReturnsAsync((string)null);
            _mockQuizRepo.Setup(r => r.checkAnswer(It.IsAny<CheckAnswerDTO>()))
                .ReturnsAsync(true);
            
            // Setup: Score value
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}:question_{dto.questionId}_Score"))
                .ReturnsAsync("10");
            
            // Setup: HashIncrementAsync calls
            _mockRedis.Setup(r => r.HashIncrementAsync(It.IsAny<string>(), "Score", 10))
                .ReturnsAsync(10);
            _mockRedis.Setup(r => r.HashIncrementAsync(It.IsAny<string>(), "CorrectCount", 1))
                .ReturnsAsync(1);
            _mockRedis.Setup(r => r.ZIncrByAsync(It.IsAny<string>(), dto.studentId, 10))
                .ReturnsAsync(10);
            
            // Setup: Room data for leaderboard update
            var roomData = new CreateRoomRedisDTO { TeacherConnectionId = "conn123" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{dto.roomCode}"))
                .ReturnsAsync(JsonConvert.SerializeObject(roomData));
            _mockOnlineRepo.Setup(r => r.updateLeaderBoard(dto.roomCode))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckOnlineAnswer(dto);

            // Assert
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
            // Arrange
            var dto = new OnlineAnswerDTO 
            { 
                roomCode = "1", 
                studentId = "10", 
                quizId = 2, 
                questionId = 3, 
                optionId = 4 
            };
            
            // Setup: Wrong answer
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}: question_{dto.questionId}: option_{dto.optionId}"))
                .ReturnsAsync((string)null);
            _mockQuizRepo.Setup(r => r.checkAnswer(It.IsAny<CheckAnswerDTO>()))
                .ReturnsAsync(false);
            
            // Setup: Correct answer from cache/repository
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}:question_{dto.questionId}:correctAnswer"))
                .ReturnsAsync((string)null);
            _mockQuizRepo.Setup(r => r.getCorrectAnswer(It.IsAny<GetCorrectAnswer>()))
                .ReturnsAsync(new RightAnswerDTO { OptionId = 6, OptionContent = "c" });
            
            // Setup: Student data
            var studentData = new CreateStudentRedisDTO 
            { 
                StudentName = "Test Student",
                TotalQuestions = 10,
                WrongAnswerRedisDTOs = new List<InsertWrongAnswerDTO>()
            };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{dto.roomCode}:student:{dto.studentId}"))
                .ReturnsAsync(JsonConvert.SerializeObject(studentData));
            
            // Setup: WrongCount increment
            _mockRedis.Setup(r => r.HashIncrementAsync(It.IsAny<string>(), "WrongCount", 1))
                .ReturnsAsync(1);
            
            // Setup: SetStringAsync for updated student data
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            
            // Setup: Room data for leaderboard update
            var roomData = new CreateRoomRedisDTO { TeacherConnectionId = "conn123" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{dto.roomCode}"))
                .ReturnsAsync(JsonConvert.SerializeObject(roomData));
            _mockOnlineRepo.Setup(r => r.updateLeaderBoard(dto.roomCode))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckOnlineAnswer(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic val = ok.Value;
            Assert.False((bool)val.isCorrect);
            
            _mockRedis.Verify(r => r.HashIncrementAsync(It.IsAny<string>(), "WrongCount", 1), Times.Once);
            _mockRedis.Verify(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task CheckOnlineAnswer_OnException_Returns500()
        {
            // Arrange
            var dto = new OnlineAnswerDTO 
            { 
                roomCode = "1", 
                studentId = "10", 
                quizId = 2, 
                questionId = 3, 
                optionId = 4 
            };
            _mockRedis.Setup(r => r.GetStringAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("e"));

            // Act
            var result = await _controller.CheckOnlineAnswer(dto);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task CheckOnlineAnswer_CorrectFromCache_ReturnsOkTrue()
        {
            // Arrange
            var dto = new OnlineAnswerDTO 
            { 
                roomCode = "1", 
                studentId = "10", 
                quizId = 2, 
                questionId = 3, 
                optionId = 4 
            };
            
            // Setup: Correct answer from cache
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}: question_{dto.questionId}: option_{dto.optionId}"))
                .ReturnsAsync("true");
            
            // Setup: Score value
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}:question_{dto.questionId}_Score"))
                .ReturnsAsync("10");
            
            // Setup: HashIncrementAsync calls
            _mockRedis.Setup(r => r.HashIncrementAsync(It.IsAny<string>(), "Score", 10))
                .ReturnsAsync(10);
            _mockRedis.Setup(r => r.HashIncrementAsync(It.IsAny<string>(), "CorrectCount", 1))
                .ReturnsAsync(1);
            _mockRedis.Setup(r => r.ZIncrByAsync(It.IsAny<string>(), dto.studentId, 10))
                .ReturnsAsync(10);
            
            // Setup: Room data
            var roomData = new CreateRoomRedisDTO { TeacherConnectionId = "conn123" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{dto.roomCode}"))
                .ReturnsAsync(JsonConvert.SerializeObject(roomData));
            _mockOnlineRepo.Setup(r => r.updateLeaderBoard(dto.roomCode))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckOnlineAnswer(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic val = ok.Value;
            Assert.True((bool)val.isCorrect);
        }

        [Fact]
        public async Task CheckOnlineAnswer_CorrectAnswerFromRepositoryCache_ReturnsOkFalse()
        {
            // Arrange
            var dto = new OnlineAnswerDTO 
            { 
                roomCode = "1", 
                studentId = "10", 
                quizId = 2, 
                questionId = 3, 
                optionId = 4 
            };
            
            // Setup: Wrong answer
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}: question_{dto.questionId}: option_{dto.optionId}"))
                .ReturnsAsync("false");
            
            // Setup: Correct answer from cache
            var correctAnswer = new RightAnswerDTO { OptionId = 6, OptionContent = "c" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz_questions_{dto.quizId}:question_{dto.questionId}:correctAnswer"))
                .ReturnsAsync(JsonConvert.SerializeObject(correctAnswer));
            
            // Setup: Student data
            var studentData = new CreateStudentRedisDTO 
            { 
                StudentName = "Test Student",
                TotalQuestions = 10,
                WrongAnswerRedisDTOs = new List<InsertWrongAnswerDTO>()
            };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{dto.roomCode}:student:{dto.studentId}"))
                .ReturnsAsync(JsonConvert.SerializeObject(studentData));
            
            // Setup: WrongCount increment
            _mockRedis.Setup(r => r.HashIncrementAsync(It.IsAny<string>(), "WrongCount", 1))
                .ReturnsAsync(1);
            
            // Setup: SetStringAsync
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            
            // Setup: Room data
            var roomData = new CreateRoomRedisDTO { TeacherConnectionId = "conn123" };
            _mockRedis.Setup(r => r.GetStringAsync($"quiz:room:{dto.roomCode}"))
                .ReturnsAsync(JsonConvert.SerializeObject(roomData));
            _mockOnlineRepo.Setup(r => r.updateLeaderBoard(dto.roomCode))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckOnlineAnswer(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic val = ok.Value;
            Assert.False((bool)val.isCorrect);
        }
    }
}
