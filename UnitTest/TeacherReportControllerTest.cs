using Capstone.Controllers;
using Capstone.DTOs.Reports.Teacher;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.Repositories;
using Capstone.Repositories.Histories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.UnitTest
{
    public class TeacherReportControllerTest
    {
        private readonly TeacherReportController _controller;
        private readonly Mock<ITeacherReportRepository> _mockRepo;
        private readonly Mock<ILogger<TeacherReportController>> _mockLogger;
        private readonly Mock<IAWS> _mockAWS;

        public TeacherReportControllerTest()
        {
            _mockRepo = new Mock<ITeacherReportRepository>();
            _mockLogger = new Mock<ILogger<TeacherReportController>>();
            _mockAWS = new Mock<IAWS>();
            
            _controller = new TeacherReportController(
                _mockLogger.Object, 
                _mockRepo.Object, 
                _mockAWS.Object
            );
        }

        #region GET Offline Reports
        [Fact]
        public async Task GetOfflineQuizReports_InvalidId_BadRequest()
        {
            var res = await _controller.GetOfflineQuizReports(0);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineQuizReports_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.GetOfflineQuizz(1)).ReturnsAsync(new List<ViewAllOfflineReportDTO>());
            var res = await _controller.GetOfflineQuizReports(1);
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineDetailReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOfflineDetailReport(new OfflineDetailReportRequest { OfflineReportId = 0, QuizId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineDetailReport_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.OfflineDetailReportEachQuiz(1, 1)).ReturnsAsync((ViewOfflineDetailReportEachQuizDTO)null);
            var res = await _controller.GetOfflineDetailReport(new OfflineDetailReportRequest { OfflineReportId = 1, QuizId = 1 });
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineDetailReport_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.OfflineDetailReportEachQuiz(1, 1)).ReturnsAsync(new ViewOfflineDetailReportEachQuizDTO());
            var res = await _controller.GetOfflineDetailReport(new OfflineDetailReportRequest { OfflineReportId = 1, QuizId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineStudentReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOfflineStudentReport(new OfflineStudentReportRequest { QuizId = 0, QGId = 1, GroupId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineStudentReport_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.OfflineStudentReportEachQuiz(1, 1, 1))
                     .ReturnsAsync(new List<ViewOfflineStudentReportEachQuizDTO>());
            var res = await _controller.GetOfflineStudentReport(new OfflineStudentReportRequest { QuizId = 1, QGId = 1, GroupId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineQuestionReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOfflineQuestionReport(new OfflineQuestionReportRequest { QuizId = 0, QGId = 1, GroupId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOfflineQuestionReport_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.OfflineQuestionReportEachQuiz(1, 1, 1))
                     .ReturnsAsync(new List<ViewOfflineQuestionReportEachQuizDTO>());
            var res = await _controller.GetOfflineQuestionReport(new OfflineQuestionReportRequest { QuizId = 1, QGId = 1, GroupId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region GET Online Reports
        [Fact]
        public async Task GetOnlineQuizReports_InvalidId_BadRequest()
        {
            var res = await _controller.GetOnlineQuizReports(0);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineQuizReports_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.GetOnlineQuiz(1)).ReturnsAsync(new List<ViewAllOnlineReportDTO>());
            var res = await _controller.GetOnlineQuizReports(1);
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineDetailReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOnlineDetailReport(new OnlineDetailReportRequest { QuizId = 0, OnlineReportId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineDetailReport_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.OnlineDetailReportEachQuiz(1, 1)).ReturnsAsync((ViewOnlineDetailReportEachQuizDTO)null);
            var res = await _controller.GetOnlineDetailReport(new OnlineDetailReportRequest { QuizId = 1, OnlineReportId = 1 });
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineDetailReport_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.OnlineDetailReportEachQuiz(1, 1)).ReturnsAsync(new ViewOnlineDetailReportEachQuizDTO());
            var res = await _controller.GetOnlineDetailReport(new OnlineDetailReportRequest { QuizId = 1, OnlineReportId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineStudentReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOnlineStudentReport(new OnlineStudentReportRequest { QuizId = 0, OnlineReportId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineStudentReport_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.OnlineStudentReportEachQuiz(1, 1))
                     .ReturnsAsync(new List<ViewOnlineStudentReportEachQuizDTO>());
            var res = await _controller.GetOnlineStudentReport(new OnlineStudentReportRequest { QuizId = 1, OnlineReportId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineQuestionReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOnlineQuestionReport(new OnlineQuestionReportRequest { QuizId = 0, OnlineReportId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetOnlineQuestionReport_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.OnlineQuestionReportEachQuiz(1, 1))
                     .ReturnsAsync(new List<ViewOnlineQuestionReportEachQuizDTO>());
            var res = await _controller.GetOnlineQuestionReport(new OnlineQuestionReportRequest { QuizId = 1, OnlineReportId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task ViewDetailOfQuestion_InvalidId_BadRequest()
        {
            var res = await _controller.ViewDetailOfQuestion(0);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ViewDetailOfQuestion_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.ViewDetailOfQuestion(1)).ReturnsAsync((DetailOfQuestionDTO)null);
            var res = await _controller.ViewDetailOfQuestion(1);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task ViewDetailOfQuestion_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.ViewDetailOfQuestion(1)).ReturnsAsync(new DetailOfQuestionDTO());
            var res = await _controller.ViewDetailOfQuestion(1);
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region POST
        [Fact]
        public async Task CheckExpiredTime_InvalidParams_BadRequest()
        {
            var res = await _controller.CheckExpiredTime(new CheckExpiredTimeRequest { QuizId = 0, QGId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task CheckExpiredTime_Ok_ReturnsOk()
        {
            _mockRepo.Setup(r => r.checkExpiredTime(1, 2)).ReturnsAsync(true);
            var res = await _controller.CheckExpiredTime(new CheckExpiredTimeRequest { QuizId = 1, QGId = 2 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task EndNow_InvalidParams_BadRequest()
        {
            var res = await _controller.EndNow(new EndNowRequest { QuizId = 0, GroupId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task EndNow_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.EndNow(1, 2)).ReturnsAsync(true);
            var res = await _controller.EndNow(new EndNowRequest { QuizId = 2, GroupId = 1 });
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task EndNow_Failed_ReturnsBadRequest()
        {
            _mockRepo.Setup(r => r.EndNow(1, 2)).ReturnsAsync(false);
            var res = await _controller.EndNow(new EndNowRequest { QuizId = 2, GroupId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
        }
        #endregion

        #region PUT
        [Fact]
        public async Task ChangeExpiredTime_InvalidIds_BadRequest()
        {
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest { QuizId = 0, QGId = 1, NewExpiredTime = DateTime.UtcNow });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeExpiredTime_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.ChangeExpiredTime(1, 2, It.IsAny<DateTime>()))
                     .ReturnsAsync(ExpiredEnum.Success);

            // Act
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest 
            { 
                QuizId = 2, 
                QGId = 1, 
                NewExpiredTime = DateTime.UtcNow.AddDays(1) 
            });

            // Assert
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task ChangeExpiredTime_QuizGroupNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.ChangeExpiredTime(1, 2, It.IsAny<DateTime>()))
                     .ReturnsAsync(ExpiredEnum.QuizGroupNotFound);

            // Act
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest 
            { 
                QuizId = 2, 
                QGId = 1, 
                NewExpiredTime = DateTime.UtcNow.AddDays(1) 
            });

            // Assert
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task ChangeExpiredTime_InvalidExpiredTime_ReturnsBadRequest()
        {
            // Arrange
            _mockRepo.Setup(r => r.ChangeExpiredTime(1, 2, It.IsAny<DateTime>()))
                     .ReturnsAsync(ExpiredEnum.InvalidExpiredTime);

            // Act
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest 
            { 
                QuizId = 2, 
                QGId = 1, 
                NewExpiredTime = DateTime.UtcNow.AddDays(1) 
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeExpiredTime_UpdateFailed_ReturnsBadRequest()
        {
            // Arrange
            _mockRepo.Setup(r => r.ChangeExpiredTime(1, 2, It.IsAny<DateTime>()))
                     .ReturnsAsync(ExpiredEnum.UpdateFailed);

            // Act
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest 
            { 
                QuizId = 2, 
                QGId = 1, 
                NewExpiredTime = DateTime.UtcNow.AddDays(1) 
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeExpiredTime_Error_Returns500()
        {
            // Arrange
            _mockRepo.Setup(r => r.ChangeExpiredTime(1, 2, It.IsAny<DateTime>()))
                     .ReturnsAsync(ExpiredEnum.Error);

            // Act
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest 
            { 
                QuizId = 2, 
                QGId = 1, 
                NewExpiredTime = DateTime.UtcNow.AddDays(1) 
            });

            // Assert
            var obj = Assert.IsType<ObjectResult>(res);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task ChangeOfflineReportName_InvalidRequest_BadRequest()
        {
            var res = await _controller.ChangeOfflineReportName(new ChangeOfflineReportNameRequest { OfflineReportId = 0, NewReportName = "x" });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeOfflineReportName_EmptyName_BadRequest()
        {
            var res = await _controller.ChangeOfflineReportName(new ChangeOfflineReportNameRequest { OfflineReportId = 1, NewReportName = "" });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeOfflineReportName_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.ChangeOfflineReport(1, "A")).ReturnsAsync(true);
            var ok = await _controller.ChangeOfflineReportName(new ChangeOfflineReportNameRequest { OfflineReportId = 1, NewReportName = "A" });
            Assert.IsType<OkObjectResult>(ok);
        }

        [Fact]
        public async Task ChangeOfflineReportName_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.ChangeOfflineReport(1, "B")).ReturnsAsync(false);
            var nf = await _controller.ChangeOfflineReportName(new ChangeOfflineReportNameRequest { OfflineReportId = 1, NewReportName = "B" });
            Assert.IsType<NotFoundObjectResult>(nf);
        }

        [Fact]
        public async Task ChangeOnlineReportName_InvalidRequest_BadRequest()
        {
            var res = await _controller.ChangeOnlineReportName(new ChangeOnlineReportNameRequest { OnlineReportId = 0, NewReportName = "A" });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeOnlineReportName_EmptyName_BadRequest()
        {
            var res = await _controller.ChangeOnlineReportName(new ChangeOnlineReportNameRequest { OnlineReportId = 1, NewReportName = "" });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task ChangeOnlineReportName_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.ChangeOnlineReportName(1, "A")).ReturnsAsync(true);
            var ok = await _controller.ChangeOnlineReportName(new ChangeOnlineReportNameRequest { OnlineReportId = 1, NewReportName = "A" });
            Assert.IsType<OkObjectResult>(ok);
        }

        [Fact]
        public async Task ChangeOnlineReportName_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.ChangeOnlineReportName(1, "B")).ReturnsAsync(false);
            var nf = await _controller.ChangeOnlineReportName(new ChangeOnlineReportNameRequest { OnlineReportId = 1, NewReportName = "B" });
            Assert.IsType<NotFoundObjectResult>(nf);
        }
        #endregion
    }
}
