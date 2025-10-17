using Capstone.Database;
using Capstone.DTOs.Group;
using Capstone.DTOs.Notification;
using Capstone.Model;
using Capstone.Repositories;
using Capstone.Repositories.Groups;
using Capstone.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Capstone.ENUMs.GroupEnum;

namespace Capstone.Services
{
    public class GroupService : IGroupRepository
    {
        private readonly ILogger<GroupService> _logger;
        private readonly Redis _redis;
        private readonly AppDbContext _appDbContext;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        public GroupService(ILogger<GroupService> logger, Redis redis, AppDbContext appDbContext,
            IHubContext<NotificationHub> hubContext, INotificationRepository notificationRepository)
        {
            _logger = logger;
            _redis = redis;
            _appDbContext = appDbContext;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
        }

        public async Task<GroupModel> CreateGroup(GroupModel groupModel)
        {
            _logger.LogInformation("CreateGroup: Start - TeacherId={TeacherId}, GroupName={GroupName}", groupModel?.TeacherId, groupModel?.GroupName);
            try
            {
                await _appDbContext.groups.AddAsync(groupModel);
                int result = await _appDbContext.SaveChangesAsync();
                if (result > 0)
                {
                    _logger.LogInformation("CreateGroup: Success - GroupId={GroupId}", groupModel.GroupId);
                    return groupModel;
                }
                else
                {
                    _logger.LogWarning("CreateGroup: No rows affected when creating group for TeacherId={TeacherId}", groupModel.TeacherId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateGroup: Error creating group for TeacherId={TeacherId}, GroupName={GroupName}", groupModel?.TeacherId, groupModel?.GroupName);
                return null;
            }
        }

        public async Task<bool> DeleteGroup(int groupId)
        {
            _logger.LogInformation("DeleteGroup: Start - GroupId={GroupId}", groupId);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                List<int> QgIdList = await _appDbContext.quizzGroups
                    .Where(gq => gq.GroupId == groupId)
                    .Select(gq => gq.QGId)
                    .ToListAsync();


                if (QgIdList.Any())
                {
                    int deletedReports = await _appDbContext.offlinereports
                        .Where(r => QgIdList.Contains(r.QGId))
                        .ExecuteDeleteAsync();

                    _logger.LogInformation("DeleteGroup: Deleted {Count} reports related to GroupId={GroupId}", deletedReports, groupId);
                }
                int deletedQuizzGroups = await _appDbContext.quizzGroups
                    .Where(gq => gq.GroupId == groupId)
                    .ExecuteDeleteAsync();
                _logger.LogInformation("DeleteGroup: Deleted {Count} quizz-group relations related to GroupId={GroupId}", deletedQuizzGroups, groupId);


                int deletedStudentGroups = await _appDbContext.studentGroups
                    .Where(sg => sg.GroupId == groupId)
                    .ExecuteDeleteAsync();
                _logger.LogInformation("DeleteGroup: Deleted {Count} student-group relations related to GroupId={GroupId}", deletedStudentGroups, groupId);


                int isDelete = await _appDbContext.groups
                    .Where(g => g.GroupId == groupId)
                    .ExecuteDeleteAsync();

                if (isDelete > 0)
                {
                    await transaction.CommitAsync();
                    _logger.LogInformation("DeleteGroup: Successfully deleted GroupId={GroupId}", groupId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("DeleteGroup: No group found with ID {GroupId} to delete", groupId);
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "DeleteGroup: Error deleting group - GroupId={GroupId}", groupId);
                return false;
            }
        }


        public async Task<List<AllGroupDTO>> GetAllGroupsByStudentId(int studentId)
        {
            _logger.LogInformation("GetAllGroupsByStudentId: Start - StudentId={StudentId}", studentId);
            try
            {
                var groups = await (from sg in _appDbContext.studentGroups
                                    join g in _appDbContext.groups on sg.GroupId equals g.GroupId
                                    where sg.StudentId == studentId
                                    orderby sg.CreateAt descending
                                    select new AllGroupDTO
                                    {
                                        GroupId = g.GroupId,
                                        GroupName = g.GroupName
                                    }).ToListAsync();
                if (groups.Any())
                {
                    _logger.LogInformation("GetAllGroupsByStudentId: Retrieved {Count} groups for StudentId={StudentId}", groups.Count, studentId);
                    return groups;
                }
                else
                {
                    _logger.LogInformation("GetAllGroupsByStudentId: No groups found for StudentId={StudentId}", studentId);
                    return new List<AllGroupDTO>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllGroupsByStudentId: Error retrieving groups for StudentId={StudentId}", studentId);
                return new List<AllGroupDTO>();
            }
        }

        public async Task<List<AllGroupDTO>> GetAllGroupsbyTeacherId(int TeacherId)
        {
            _logger.LogInformation("GetAllGroupsbyTeacherId: Start - TeacherId={TeacherId}", TeacherId);
            try
            {
                var groups = await _appDbContext.groups
                    .Where(g => g.TeacherId == TeacherId)
                    .OrderByDescending(g => g.CreateAt)
                    .Select(g => new AllGroupDTO
                    {
                        GroupId = g.GroupId,
                        GroupName = g.GroupName
                    })
                    .ToListAsync();
                if (groups.Any())
                {
                    _logger.LogInformation("GetAllGroupsbyTeacherId: Retrieved {Count} groups for TeacherId={TeacherId}", groups.Count, TeacherId);
                    return groups;
                }
                else
                {
                    _logger.LogInformation("GetAllGroupsbyTeacherId: No groups found for TeacherId={TeacherId}", TeacherId);
                    return new List<AllGroupDTO>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllGroupsbyTeacherId: Error retrieving groups for TeacherId={TeacherId}", TeacherId);
                return new List<AllGroupDTO>();
            }
        }

        public async Task<List<ViewQuizDTO>> GetAllDeliveredQuizzesByGroupId(int groupId)
        {
            _logger.LogInformation("GetAllQuizzesByGroupId: Start - GroupId={GroupId}", groupId);
            try
            {
                var quizzes = await (from gq in _appDbContext.quizzGroups
                                     join q in _appDbContext.quizzes on gq.QuizId equals q.QuizId
                                     join t in _appDbContext.teacherProfiles on q.TeacherId equals t.TeacherId
                                     join r in _appDbContext.offlinereports on gq.QGId equals r.QGId
                                     where gq.GroupId == groupId
                                     orderby gq.CreateAt descending
                                     select new
                                     {
                                         QGId = gq.QGId,
                                         quizId = q.QuizId,
                                         Title = r.ReportName,
                                         TeacherName = t.FullName,
                                         DateCreated = gq.CreateAt,
                                         ExpiredDate = gq.ExpiredTime,
                                         Message = gq.Message
                                     }).ToListAsync();
                List<ViewQuizDTO> result = new List<ViewQuizDTO>();
                foreach (var quiz in quizzes)
                {
                    //var totalQuestion = await _appDbContext.questions.Where(q => q.QuizId == quiz.quizId && q.IsDeleted == false).CountAsync();
                    var deliveredQuizz = await _appDbContext.quizzes.Where(q => q.QuizId == quiz.quizId).
                                                    Select(q => new DeliveredQuizz
                                                    {
                                                        QuizId = q.QuizId,
                                                        AvatarURL = q.AvatarURL,
                                                        TotalQuestions = _appDbContext.questions.Count(ques => ques.QuizId == q.QuizId && !ques.IsDeleted)
                                                    }).FirstOrDefaultAsync();
                    ViewQuizDTO newViewQuizDTO = new ViewQuizDTO()
                    {
                        QGId = quiz.QGId,
                        DeliveredQuiz = deliveredQuizz,
                        Title = quiz.Title,
                        TeacherName = quiz.TeacherName,
                        DateCreated = quiz.DateCreated,
                        ExpiredDate = quiz.ExpiredDate,
                        Message = quiz.Message
                    };
                    result.Add(newViewQuizDTO);
                }
                if (quizzes.Any())
                {
                    _logger.LogInformation("GetAllQuizzesByGroupId: Retrieved {Count} quizzes for GroupId={GroupId}", quizzes.Count, groupId);
                    return result;
                }
                else
                {
                    _logger.LogInformation("GetAllQuizzesByGroupId: No quizzes found for GroupId={GroupId}", groupId);
                    return new List<ViewQuizDTO>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllQuizzesByGroupId: Error retrieving quizzes for GroupId={GroupId}", groupId);
                return new List<ViewQuizDTO>();
            }
        }

        public async Task<List<ViewStudentDTO>> GetAllStudentsByGroupId(int groupId)
        {
            _logger.LogInformation("GetAllStudentsByGroupId: Start - GroupId={GroupId}", groupId);
            try
            {
                var students = await (from sg in _appDbContext.studentGroups
                                      join sp in _appDbContext.studentProfiles on sg.StudentId equals sp.StudentId
                                      join a in _appDbContext.authModels on sp.StudentId equals a.AccountId
                                      where sg.GroupId == groupId
                                      select new ViewStudentDTO
                                      {
                                          StudentId = sp.StudentId,
                                          FullName = sp.FullName,
                                          Email = a.Email,
                                          Avatar = sp.AvatarURL,
                                          DateJoined = sg.CreateAt,
                                          Permission = "Student"
                                      }).ToListAsync();
                if (students.Any())
                {
                    _logger.LogInformation("GetAllStudentsByGroupId: Retrieved {Count} students for GroupId={GroupId}", students.Count, groupId);
                    return students;
                }
                else
                {
                    _logger.LogInformation("GetAllStudentsByGroupId: No students found for GroupId={GroupId}", groupId);
                    return new List<ViewStudentDTO>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStudentsByGroupId: Error retrieving students for GroupId={GroupId}", groupId);
                return new List<ViewStudentDTO>();
            }
        }

        public async Task<GroupModel> GetGroupDetailById(int groupId)
        {
            _logger.LogInformation("GetGroupDetailById: Start - GroupId={GroupId}", groupId);
            try
            {
                var group = await _appDbContext.groups
                    .Where(g => g.GroupId == groupId)
                    .FirstOrDefaultAsync();
                if (group != null)
                {
                    _logger.LogInformation("GetGroupDetailById: Success - GroupId={GroupId}", groupId);
                    return group;
                }
                else
                {
                    _logger.LogWarning("GetGroupDetailById: No group found - GroupId={GroupId}", groupId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGroupDetailById: Error retrieving group details for GroupId={GroupId}", groupId);
                return null;
            }
        }

        public async Task<InsertQuiz> InsertQuizToGroup(InsertQuiz insertQuiz)
        {
            _logger.LogInformation("InsertQuizToGroup: Start - QuizId={QuizId}, GroupId={GroupId}", insertQuiz?.QuizId, insertQuiz?.GroupId);
            try
            {
                using (var transaction = await _appDbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1️⃣ Tạo bản ghi QuizzGroup
                        var quizzGroupModel = new QuizzGroupModel
                        {
                            QuizId = insertQuiz.QuizId,
                            GroupId = insertQuiz.GroupId,
                            Message = insertQuiz.Message,
                            Status = "Pending",
                            ExpiredTime = insertQuiz.ExpiredTime,
                            MaxAttempts = insertQuiz.MaxAttempts,
                            CreateAt = DateTime.Now
                        };
                        Console.WriteLine("step1");
                        await _appDbContext.quizzGroups.AddAsync(quizzGroupModel);
                        await _appDbContext.SaveChangesAsync();

                        // 2️⃣ Lấy quiz title
                        var quizTitle = await _appDbContext.quizzes
                            .Where(q => q.QuizId == insertQuiz.QuizId)
                            .Select(q => q.Title)
                            .FirstOrDefaultAsync();
                        Console.WriteLine("step2");
                        // 3️⃣ Tạo report
                        var newReport = new OfflineReportsModel
                        {
                            QGId = quizzGroupModel.QGId,
                            QuizId = insertQuiz.QuizId,
                            ReportName = quizTitle,
                            HighestScore = 0,
                            LowestScore = 0,
                            AverageScore = 0,
                            TotalParticipants = 0,
                            CreateAt = DateTime.Now
                        };
                        Console.WriteLine("step3");
                        await _appDbContext.offlinereports.AddAsync(newReport);
                        await _appDbContext.SaveChangesAsync();
                        // Insert Notification
                        List<int> studentId = await _appDbContext.studentGroups.Where(sg => sg.GroupId == insertQuiz.GroupId)
                            .Select(sg => sg.StudentId)
                            .ToListAsync();
                        var groupName = await _appDbContext.groups.Where(g => g.GroupId == insertQuiz.GroupId).Select(g => g.GroupName).FirstOrDefaultAsync();
                        string message = $"A new quiz has been created in {groupName} group";
                        foreach (var student in studentId)
                        {
                            Console.WriteLine("step4");
                            InsertNewNotificationDTO newNotifcation = new InsertNewNotificationDTO()
                            {
                                SenderId = insertQuiz.TeacherId,
                                ReceiverId = student,
                                Message = message
                            };
                            bool isInsert = await _notificationRepository.InsertNewNotification(newNotifcation);
                            Console.WriteLine("step5");
                        }
                        await transaction.CommitAsync();
                        // gửi tin realtime 
                        foreach (var student in studentId)
                        {

                            await _hubContext.Clients.User(student.ToString())
                                .SendAsync("InsertQuizToGroupNotification", message);
                            Console.WriteLine("step6");
                        }
                        _logger.LogInformation(
                            "InsertQuizToGroup: Successfully inserted QuizId={QuizId} into GroupId={GroupId} with ReportId={ReportId}",
                            insertQuiz.QuizId, insertQuiz.GroupId, newReport.OfflineReportId
                        );

                        return insertQuiz;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "InsertQuizToGroup: Transaction error inserting QuizId={QuizId} into GroupId={GroupId}", insertQuiz?.QuizId, insertQuiz?.GroupId);
                        return null;
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertQuizToGroup: Error inserting QuizId={QuizId} into GroupId={GroupId}", insertQuiz?.QuizId, insertQuiz?.GroupId);
                return null;
            }
        }

        public async Task<JoinGroupResult> InsertStudentToGroup(int groupId, string IdUnique)
        {
            try
            {
                var StudentId = await _appDbContext.studentProfiles
                    .Where(s => s.IdUnique == IdUnique)
                    .Select(s => s.StudentId)
                    .FirstOrDefaultAsync();
                if (StudentId == 0)
                {
                    _logger.LogWarning("InsertStudentToGroup: Student with IdUnique={IdUnique} not found", IdUnique);
                    return JoinGroupResult.Fail;
                }
                bool isStudentInGroup = await _appDbContext.studentGroups
                    .AnyAsync(sg => sg.GroupId == groupId && sg.StudentId == StudentId);
                if (isStudentInGroup == true)
                {
                    _logger.LogInformation("InsertStudentToGroup: Student with IdUnique={IdUnique} already in GroupId={GroupId}", IdUnique, groupId);
                    return JoinGroupResult.AlreadyInGroup;
                }
                await _appDbContext.studentGroups.AddAsync(new StudentGroupModel
                {
                    StudentId = StudentId,
                    GroupId = groupId,
                    CreateAt = DateTime.Now
                });
                int result = await _appDbContext.SaveChangesAsync();
                return JoinGroupResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertStudentToGroup: Error inserting student with IdUnique={IdUnique} into GroupId={GroupId}", IdUnique, groupId);
                return JoinGroupResult.Error;
            }
        }

        public async Task<JoinGroupResult> JoinGroupByInvite(string inviteCode, int studentId)
        {
            _logger.LogInformation("JoinGroupByInvite: Start - InviteCode={InviteCode}, StudentId={StudentId}", inviteCode, studentId);
            try
            {

                var group = await _appDbContext.groups
                    .FirstOrDefaultAsync(g => g.IdUnique == inviteCode);

                if (group == null)
                {
                    _logger.LogWarning("JoinGroupByInvite: Invite code not found - InviteCode={InviteCode}", inviteCode);
                    return JoinGroupResult.Fail;
                }

                // Check student đã trong group chưa
                bool alreadyInGroup = await _appDbContext.studentGroups
                    .AnyAsync(sg => sg.GroupId == group.GroupId && sg.StudentId == studentId);

                if (alreadyInGroup)
                {
                    _logger.LogInformation("JoinGroupByInvite: Student already in group - StudentId={StudentId}, GroupId={GroupId}", studentId, group.GroupId);
                    return JoinGroupResult.AlreadyInGroup;
                }


                var studentGroup = new StudentGroupModel
                {
                    StudentId = studentId,
                    GroupId = group.GroupId,
                    CreateAt = DateTime.Now
                };

                _appDbContext.studentGroups.Add(studentGroup);
                await _appDbContext.SaveChangesAsync();

                _logger.LogInformation("JoinGroupByInvite: Student joined group - StudentId={StudentId}, GroupId={GroupId}", studentId, group.GroupId);
                return JoinGroupResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinGroupByInvite: Error joining group by invite code={InviteCode}", inviteCode);
                return JoinGroupResult.Error;
            }
        }

        public async Task<bool> LeaveGroup(int groupId, int studentId, int teacherId)
        {

            _logger.LogInformation("LeaveGroup: Start - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);

            try
            {

                string? groupName = await _appDbContext.groups
                    .Where(g => g.GroupId == groupId)
                    .Select(g => g.GroupName)
                    .FirstOrDefaultAsync();

                if (groupName == null)
                {

                    _logger.LogWarning("LeaveGroup: Group not found. GroupId={GroupId}", groupId);
                    return false;
                }

                using (var transaction = await _appDbContext.Database.BeginTransactionAsync())
                {
                    try
                    {

                        int rowsDeleted = await _appDbContext.studentGroups
                            .Where(sg => sg.GroupId == groupId && sg.StudentId == studentId)
                            .ExecuteDeleteAsync();


                        if (rowsDeleted > 0)
                        {
                            string message = $"Student ID {studentId} has left the group '{groupName}'.";

                            InsertNewNotificationDTO newNotificationDTO = new InsertNewNotificationDTO
                            {

                                SenderId = studentId,
                                ReceiverId = teacherId,
                                Message = message
                            };

                            bool isInsertSuccess = await _notificationRepository.InsertNewNotification(newNotificationDTO);

                            if (!isInsertSuccess)
                            {
                                // FIX 3: Change Log Message
                                _logger.LogError("LeaveGroup: Failed to insert notification for TeacherId={TeacherId}", teacherId);
                                await transaction.RollbackAsync();
                                return false;
                            }

                            // 5. Commit transaction
                            await transaction.CommitAsync();


                            await _hubContext.Clients.User(teacherId.ToString()).SendAsync("StudentLeftGroup", message);

                            _logger.LogInformation("LeaveGroup: Success - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                            return true;
                        }
                        else
                        {

                            _logger.LogWarning("LeaveGroup: Student not found in group or already left. StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "LeaveGroup: Transaction failed for StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveStudentFromGroup: Repository operation failed for StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                return false;
            }
        }
        public async Task<bool> RemoveQuizFromGroup(int groupId, int quizId)
        {
            _logger.LogInformation("RemoveQuizFromGroup: Start - QuizId={QuizId}, GroupId={GroupId}", quizId, groupId);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            try
            {
                var QgId = await _appDbContext.quizzGroups
                    .Where(gq => gq.GroupId == groupId && gq.QuizId == quizId)
                    .Select(gq => gq.QGId)
                    .FirstOrDefaultAsync();

                if (QgId == 0)
                {
                    _logger.LogWarning("RemoveQuizFromGroup: Not found - QuizId={QuizId}, GroupId={GroupId}", quizId, groupId);
                    return false;
                }

                // Xóa report trước
                int isDeleteReport = await _appDbContext.offlinereports
                    .Where(r => r.QGId == QgId)
                    .ExecuteDeleteAsync();

                // Xóa quan hệ quiz-group
                int isDeleteQuizzGroup = await _appDbContext.quizzGroups
                    .Where(gq => gq.GroupId == groupId && gq.QuizId == quizId)
                    .ExecuteDeleteAsync();

                if (isDeleteQuizzGroup > 0)
                {
                    await transaction.CommitAsync();
                    _logger.LogInformation("RemoveQuizFromGroup: Success - QuizId={QuizId} removed from GroupId={GroupId}", quizId, groupId);
                    return true;
                }

                await transaction.RollbackAsync();
                _logger.LogWarning("RemoveQuizFromGroup: Nothing deleted - QuizId={QuizId}, GroupId={GroupId}", quizId, groupId);
                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "RemoveQuizFromGroup: Error removing QuizId={QuizId} from GroupId={GroupId}", quizId, groupId);
                return false;
            }
        }
        public async Task<bool> RemoveStudentFromGroup(int groupId, int studentId, int teacherId)
        {
            _logger.LogInformation("RemoveStudentFromGroup: Start - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);

            try
            {
                
                string? groupName = await _appDbContext.groups
                    .Where(g => g.GroupId == groupId)
                    .Select(g => g.GroupName)
                    .FirstOrDefaultAsync();

                if (groupName == null)
                {
                    _logger.LogWarning("RemoveStudentFromGroup: Group not found. GroupId={GroupId}", groupId);
                    return false;
                }

                using (var transaction = await _appDbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        int rowsDeleted = await _appDbContext.studentGroups
                            .Where(sg => sg.GroupId == groupId && sg.StudentId == studentId)
                            .ExecuteDeleteAsync();

                      
                        if (rowsDeleted > 0)
                        {
                            
                            string message = $"You have been removed from the group '{groupName}'.";

                            InsertNewNotificationDTO newNotificationDTO = new InsertNewNotificationDTO
                            {
                                SenderId = teacherId,
                                ReceiverId = studentId,
                                Message = message
                            };

                            bool isInsertSuccess = await _notificationRepository.InsertNewNotification(newNotificationDTO);

                            if (!isInsertSuccess)
                            {
                                _logger.LogError("RemoveStudentFromGroup: Failed to insert notification for StudentId={StudentId}", studentId);
                                await transaction.RollbackAsync();
                                return false;
                            }

                          
                            await transaction.CommitAsync();

                        
                            await _hubContext.Clients.User(studentId.ToString()).SendAsync("RemoveStudentFromGroup", message);

                            _logger.LogInformation("RemoveStudentFromGroup: Success - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                            return true;
                        }
                        else
                        {
                           
                            _logger.LogWarning("RemoveStudentFromGroup: Student not found in group or already removed. StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "RemoveStudentFromGroup: Transaction failed for StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveStudentFromGroup: Repository operation failed for StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                return false;
            }
        }
        
        public async Task<UpdateGroupDTO> updateGroup(UpdateGroupDTO groupModel)
        {
            _logger.LogInformation("updateGroup: Start - GroupId={GroupId}, GroupName={GroupName}", groupModel?.GroupId, groupModel?.GroupName);
            try
            {
                int isUpdate = await _appDbContext.groups
                    .Where(g => g.GroupId == groupModel.GroupId)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(g => g.GroupName, groupModel.GroupName)
                        .SetProperty(g => g.GroupDescription, groupModel.GroupDescription));
                if (isUpdate > 0)
                {
                    _logger.LogInformation("updateGroup: Success - GroupId={GroupId}", groupModel.GroupId);
                    return groupModel;
                }
                else
                {
                    _logger.LogWarning("updateGroup: No group found with ID {GroupId} to update", groupModel.GroupId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateGroup: Error updating group GroupId={GroupId}", groupModel?.GroupId);
                return null;
            }
        }
    }
}
