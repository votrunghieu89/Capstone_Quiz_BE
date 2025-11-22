using Capstone.Controllers;
using Capstone.Database;
using Capstone.DTOs.Group;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.Repositories;
using Capstone.Repositories.Groups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Security.Claims;

namespace Capstone.UnitTest
{
    public class GroupControllerTest
    {
        private readonly GroupController _controller;
        private readonly Mock<IGroupRepository> _mockRepo;
        private readonly Mock<ILogger<GroupController>> _mockLogger;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IAWS> _mockAWS;

        public GroupControllerTest()
        {
            _mockRepo = new Mock<IGroupRepository>();
            _mockLogger = new Mock<ILogger<GroupController>>();
            _mockRedis = new Mock<IRedis>();
            _mockConfig = new Mock<IConfiguration>();
            _mockAWS = new Mock<IAWS>();
            
            _mockConfig.Setup(c => c["Frontend:BaseUrl"]).Returns("http://frontend.test");

            _controller = new GroupController(
                _mockLogger.Object, 
                _mockRepo.Object, 
                _mockRedis.Object, 
                _mockConfig.Object, 
                _mockAWS.Object
            );
            
            // Setup HttpContext with User Claims for AccountId
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

        #region GET: GetGroupsByTeacherId
        [Fact]
        public async Task GetGroupsByTeacherId_ReturnsOk()
        {
            // Arrange
            int teacherId = 1;
            _mockRepo.Setup(r => r.GetAllGroupsbyTeacherId(teacherId))
                     .ReturnsAsync(new List<AllGroupDTO> { new AllGroupDTO { GroupId = 1, GroupName = "G1" } });

            // Act
            var result = await _controller.GetGroupsByTeacherId(teacherId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AllGroupDTO>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetGroupsByTeacherId_EmptyList_ReturnsOk()
        {
            // Arrange
            int teacherId = 1;
            _mockRepo.Setup(r => r.GetAllGroupsbyTeacherId(teacherId))
                     .ReturnsAsync(new List<AllGroupDTO>());

            // Act
            var result = await _controller.GetGroupsByTeacherId(teacherId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AllGroupDTO>>(ok.Value);
            Assert.Empty(list);
        }
        #endregion

        #region GET: GetAllStudentsByGroup
        [Fact]
        public async Task GetAllStudentsByGroup_ReturnsOk()
        {
            // Arrange
            int groupId = 2;
            _mockRepo.Setup(r => r.GetAllStudentsByGroupId(groupId))
                     .ReturnsAsync(new List<ViewStudentDTO> { new ViewStudentDTO { StudentId = 10, FullName = "A" } });

            // Act
            var result = await _controller.GetAllStudentsByGroup(groupId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<ViewStudentDTO>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetAllStudentsByGroup_EmptyList_ReturnsOk()
        {
            // Arrange
            int groupId = 2;
            _mockRepo.Setup(r => r.GetAllStudentsByGroupId(groupId))
                     .ReturnsAsync(new List<ViewStudentDTO>());

            // Act
            var result = await _controller.GetAllStudentsByGroup(groupId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<ViewStudentDTO>>(ok.Value);
            Assert.Empty(list);
        }
        #endregion

        #region GET: GetAllGroupsByStudentId
        [Fact]
        public async Task GetAllGroupsByStudentId_ReturnsOk()
        {
            // Arrange
            int studentId = 3;
            _mockRepo.Setup(r => r.GetAllGroupsByStudentId(studentId))
                     .ReturnsAsync(new List<AllGroupDTO> { new AllGroupDTO { GroupId = 2, GroupName = "G2" } });

            // Act
            var result = await _controller.GetGroupsByStudentId(studentId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AllGroupDTO>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetAllGroupsByStudentId_EmptyList_ReturnsOk()
        {
            // Arrange
            int studentId = 3;
            _mockRepo.Setup(r => r.GetAllGroupsByStudentId(studentId))
                     .ReturnsAsync(new List<AllGroupDTO>());

            // Act
            var result = await _controller.GetGroupsByStudentId(studentId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AllGroupDTO>>(ok.Value);
            Assert.Empty(list);
        }
        #endregion

        #region GET: GetGroupDetail
        [Fact]
        public async Task GetGroupDetail_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            int groupId = 9;
            _mockRepo.Setup(r => r.GetGroupDetailById(groupId)).ReturnsAsync((GroupModel)null);

            // Act
            var result = await _controller.GetGroupDetail(groupId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetGroupDetail_WhenFound_ReturnsOk()
        {
            // Arrange
            int groupId = 9;
            var group = new GroupModel 
            { 
                GroupId = groupId, 
                GroupName = "G9", 
                TeacherId = 1, 
                IdUnique = "INV123",
                CreateAt = DateTime.Now
            };
            _mockRepo.Setup(r => r.GetGroupDetailById(groupId)).ReturnsAsync(group);
            _mockRepo.Setup(r => r.GetAllDeliveredQuizzesByGroupId(groupId))
                     .ReturnsAsync(new List<ViewQuizDTO>());

            // Act
            var result = await _controller.GetGroupDetail(groupId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }
        #endregion

        #region POST: CreateGroup
        [Fact]
        public async Task CreateGroup_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CreateGroup(null);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateGroup_Success_ReturnsOk()
        {
            // Arrange
            var req = new CreateGroupDTO { TeacherId = 1, GroupName = "G" };
            var saved = new GroupModel 
            { 
                GroupId = 100, 
                TeacherId = 1, 
                GroupName = "G", 
                CreateAt = DateTime.Now,
                IdUnique = "ABC123"
            };
            _mockRepo.Setup(r => r.CreateGroup(It.IsAny<GroupModel>(), It.IsAny<string>()))
                     .ReturnsAsync(saved);

            // Act
            var result = await _controller.CreateGroup(req);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<GroupModel>(ok.Value);
            Assert.Equal(100, model.GroupId);
        }

        [Fact]
        public async Task CreateGroup_RepoReturnsNull_Returns500()
        {
            // Arrange
            var req = new CreateGroupDTO { TeacherId = 1, GroupName = "G" };
            _mockRepo.Setup(r => r.CreateGroup(It.IsAny<GroupModel>(), It.IsAny<string>()))
                     .ReturnsAsync((GroupModel)null);

            // Act
            var result = await _controller.CreateGroup(req);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region POST: InsertStudentToGroup
        [Fact]
        public async Task InsertStudentToGroup_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.InsertStudentToGroup(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.Success);

            // Act
            var result = await _controller.InsertStudentToGroup(1, "UUU");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task InsertStudentToGroup_AlreadyInGroup_ReturnsBadRequest()
        {
            // Arrange
            _mockRepo.Setup(r => r.InsertStudentToGroup(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.AlreadyInGroup);

            // Act
            var result = await _controller.InsertStudentToGroup(1, "UUU");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task InsertStudentToGroup_Fail_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.InsertStudentToGroup(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.Fail);

            // Act
            var result = await _controller.InsertStudentToGroup(1, "UUU");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task InsertStudentToGroup_Error_Returns500()
        {
            // Arrange
            _mockRepo.Setup(r => r.InsertStudentToGroup(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.Error);

            // Act
            var result = await _controller.InsertStudentToGroup(1, "UUU");

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region POST: InsertQuizToGroup
        [Fact]
        public async Task InsertQuizToGroup_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.InsertQuizToGroup(null);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task InsertQuizToGroup_Success_Returns200()
        {
            // Arrange
            var req = new InsertQuiz { QuizId = 1, GroupId = 2, MaxAttempts = 1, ExpiredTime = DateTime.UtcNow };
            _mockRepo.Setup(r => r.InsertQuizToGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(req);

            // Act
            var result = await _controller.InsertQuizToGroup(req);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, obj.StatusCode);
        }

        [Fact]
        public async Task InsertQuizToGroup_Fail_Returns500()
        {
            // Arrange
            var req = new InsertQuiz { QuizId = 1, GroupId = 2 };
            _mockRepo.Setup(r => r.InsertQuizToGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync((InsertQuiz)null);

            // Act
            var result = await _controller.InsertQuizToGroup(req);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region POST: JoinGroupByInvite
        [Fact]
        public async Task JoinGroupByInvite_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.JoinGroupByInvite(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.Success);

            // Act
            var result = await _controller.JoinGroupByInvite("INV", 10);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task JoinGroupByInvite_AlreadyInGroup_ReturnsBadRequest()
        {
            // Arrange
            _mockRepo.Setup(r => r.JoinGroupByInvite(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.AlreadyInGroup);

            // Act
            var result = await _controller.JoinGroupByInvite("INV", 10);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task JoinGroupByInvite_Fail_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.JoinGroupByInvite(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.Fail);

            // Act
            var result = await _controller.JoinGroupByInvite("INV", 10);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task JoinGroupByInvite_Error_Returns500()
        {
            // Arrange
            _mockRepo.Setup(r => r.JoinGroupByInvite(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(GroupEnum.JoinGroupResult.Error);

            // Act
            var result = await _controller.JoinGroupByInvite("INV", 10);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region PUT: UpdateGroup
        [Fact]
        public async Task UpdateGroup_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.UpdateGroup(null);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateGroup_NotFound_ReturnsNotFound()
        {
            // Arrange
            var req = new UpdateGroupDTO { GroupId = 5, GroupName = "X" };
            _mockRepo.Setup(r => r.updateGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync((UpdateGroupDTO)null);

            // Act
            var result = await _controller.UpdateGroup(req);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateGroup_Success_ReturnsOk()
        {
            // Arrange
            var req = new UpdateGroupDTO { GroupId = 5, GroupName = "X" };
            _mockRepo.Setup(r => r.updateGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(req);

            // Act
            var result = await _controller.UpdateGroup(req);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UpdateGroupDTO>(ok.Value);
            Assert.Equal(5, dto.GroupId);
        }
        #endregion

        #region DELETE: DeleteGroup
        [Fact]
        public async Task DeleteGroup_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.DeleteGroup(8, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteGroup(8);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task DeleteGroup_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.DeleteGroup(8, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteGroup(8);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region DELETE: LeaveGroup
        [Fact]
        public async Task LeaveGroup_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.LeaveGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(true);
            
            // Act
            var result = await _controller.LeaveGroup(1, 2, 3);
            
            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task LeaveGroup_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.LeaveGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(false);
            
            // Act
            var result = await _controller.LeaveGroup(1, 2, 3);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region DELETE: RemoveStudentFromGroup
        [Fact]
        public async Task RemoveStudentFromGroup_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.RemoveStudentFromGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(true);
            
            // Act
            var result = await _controller.RemoveStudentFromGroup(1, 2, 3);
            
            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RemoveStudentFromGroup_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.RemoveStudentFromGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(false);
            
            // Act
            var result = await _controller.RemoveStudentFromGroup(1, 2, 3);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region DELETE: RemoveQuizFromGroup
        [Fact]
        public async Task RemoveQuizFromGroup_Success_ReturnsOk()
        {
            // Arrange
            _mockRepo.Setup(r => r.RemoveQuizFromGroup(1, 2, 3, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
            
            // Act
            var result = await _controller.RemoveQuizFromGroup(1, 2, 3);
            
            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RemoveQuizFromGroup_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.RemoveQuizFromGroup(1, 2, 3, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(false);
            
            // Act
            var result = await _controller.RemoveQuizFromGroup(1, 2, 3);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
