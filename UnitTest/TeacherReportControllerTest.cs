using Capstone.Controllers;
using Capstone.DTOs.Reports.Teacher;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
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

        public TeacherReportControllerTest()
        {
            _mockRepo = new Mock<ITeacherReportRepository>();
            _mockLogger = new Mock<ILogger<TeacherReportController>>();
            _controller = new TeacherReportController(_mockLogger.Object, _mockRepo.Object);
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
        public async Task GetOfflineQuestionReport_InvalidParams_BadRequest()
        {
            var res = await _controller.GetOfflineQuestionReport(new OfflineQuestionReportRequest { QuizId = 0, QGId = 1, GroupId = 1 });
            Assert.IsType<BadRequestObjectResult>(res);
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
        #endregion

        #region PUT
        [Fact]
        public async Task ChangeExpiredTime_InvalidIds_BadRequest()
        {
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest { QuizId = 0, QGId = 1, NewExpiredTime = DateTime.UtcNow });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Theory]
        [InlineData(ExpiredEnum.Success, 200)]
        [InlineData(ExpiredEnum.QuizGroupNotFound, 404)]
        [InlineData(ExpiredEnum.InvalidExpiredTime, 400)]
        [InlineData(ExpiredEnum.UpdateFailed, 400)]
        [InlineData(ExpiredEnum.Error, 500)]
        public async Task ChangeExpiredTime_ResultMapping(ExpiredEnum repoResult, int expectedStatus)
        {
            _mockRepo.Setup(r => r.ChangeExpiredTime(1, 2, It.IsAny<DateTime>())).ReturnsAsync(repoResult);
            var res = await _controller.ChangeExpiredTime(new ChangeExpiredTimeRequest { QuizId = 2, QGId = 1, NewExpiredTime = DateTime.UtcNow.AddDays(1) });
            var obj = Assert.IsType<ObjectResult>(res);
            Assert.Equal(expectedStatus, obj.StatusCode);
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
        public async Task ChangeOfflineReportName_OkOrNotFound()
        {
            _mockRepo.Setup(r => r.ChangeOfflineReport(1, "A")).ReturnsAsync(true);
            var ok = await _controller.ChangeOfflineReportName(new ChangeOfflineReportNameRequest { OfflineReportId = 1, NewReportName = "A" });
            Assert.IsType<OkObjectResult>(ok);

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
        public async Task ChangeOnlineReportName_OkOrNotFound()
        {
            _mockRepo.Setup(r => r.ChangeOnlineReportName(1, "A")).ReturnsAsync(true);
            var ok = await _controller.ChangeOnlineReportName(new ChangeOnlineReportNameRequest { OnlineReportId = 1, NewReportName = "A" });
            Assert.IsType<OkObjectResult>(ok);

            _mockRepo.Setup(r => r.ChangeOnlineReportName(1, "B")).ReturnsAsync(false);
            var nf = await _controller.ChangeOnlineReportName(new ChangeOnlineReportNameRequest { OnlineReportId = 1, NewReportName = "B" });
            Assert.IsType<NotFoundObjectResult>(nf);
        }
        #endregion
    }
}
