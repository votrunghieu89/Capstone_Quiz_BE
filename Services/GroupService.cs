using Capstone.Database;
using Capstone.DTOs.Group;
using Capstone.Model;
using Capstone.Repositories.Groups;
using Microsoft.EntityFrameworkCore;
using static Capstone.ENUMs.GroupEnum;

namespace Capstone.Services
{
    public class GroupService : IGroupRepository
    {
        private  readonly ILogger<GroupService> _logger;
        private readonly Redis _redis;
        private readonly AppDbContext _appDbContext; 

        public GroupService(ILogger<GroupService> logger, Redis redis, AppDbContext appDbContext)
        {
            _logger = logger;
            _redis = redis;
            _appDbContext = appDbContext;
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
            try
            {
                int isDelete = await _appDbContext.groups
                    .Where(g => g.GroupId == groupId)
                    .ExecuteDeleteAsync();
                if (isDelete > 0) { 
                    _logger.LogInformation("DeleteGroup: Success - GroupId={GroupId}", groupId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("DeleteGroup: No group found to delete - GroupId={GroupId}", groupId);
                    return false;
                }
            }
            catch (Exception ex)
            {
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
                                     join r in _appDbContext.reports on gq.QGId equals r.QGId 
                                     where gq.GroupId == groupId
                                     select new ViewQuizDTO
                                     {
                                        quizId = q.QuizId,
                                        Title = r.ReportName,
                                        TeacherName = t.FullName,
                                        DateCreated = gq.CreateAt.ToString("yyyy-MM-dd"),
                                        Message = gq.Message
                                     }).ToListAsync();
                if (quizzes.Any())
                {
                    _logger.LogInformation("GetAllQuizzesByGroupId: Retrieved {Count} quizzes for GroupId={GroupId}", quizzes.Count, groupId);
                    return quizzes;
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
                                    DateJoined = sg.CreateAt.ToString("yyyy-MM-dd"),
                                    Permission = "Member"
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
            catch( Exception ex)
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
            catch( Exception ex)
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
                            CreateAt = DateTime.Now
                        };

                        await _appDbContext.quizzGroups.AddAsync(quizzGroupModel);
                        await _appDbContext.SaveChangesAsync();

                        // 2️⃣ Lấy quiz title
                        var quizTitle = await _appDbContext.quizzes
                            .Where(q => q.QuizId == insertQuiz.QuizId)
                            .Select(q => q.Title)
                            .FirstOrDefaultAsync();

                        // 3️⃣ Tạo report
                        var newReport = new ReportModel
                        {
                            QGId = quizzGroupModel.QGId,
                            QuizId = insertQuiz.QuizId,
                            ReportName = quizTitle,
                            HighestScore = 0,
                            LowestScore = 0,
                            AverageScore = 0,
                            TotalParticipants = 0,
                            CreatedAt = DateTime.Now
                        };

                        await _appDbContext.reports.AddAsync(newReport);
                        await _appDbContext.SaveChangesAsync();

                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "InsertQuizToGroup: Successfully inserted QuizId={QuizId} into GroupId={GroupId} with ReportId={ReportId}",
                            insertQuiz.QuizId, insertQuiz.GroupId, newReport.ReportId
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
            catch( Exception ex)
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
                    return JoinGroupResult.Fail ;
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

        public async Task<bool> LeaveGroup(int groupId, int studentId)
        {
            _logger.LogInformation("LeaveGroup: Start - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
            try
            {
                int isDelete = await _appDbContext.studentGroups
                    .Where(sg => sg.GroupId == groupId && sg.StudentId == studentId)
                    .ExecuteDeleteAsync();
                if (isDelete > 0)
                {
                    _logger.LogInformation("LeaveGroup: Success - StudentId={StudentId} left GroupId={GroupId}", studentId, groupId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("LeaveGroup: Student not found in group - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                    return false;
                }

            }
            catch (Exception ex) { 
                _logger.LogError(ex, "LeaveGroup: Error student leaving group - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                return false;
            }
            
            
        }

        public async Task<bool> RemoveQuizFromGroup(int groupId, int quizId)
        {
            _logger.LogInformation("RemoveQuizFromGroup: Start - QuizId={QuizId}, GroupId={GroupId}", quizId, groupId);
            try
            {
                int isDelete = await _appDbContext.quizzGroups
                    .Where(gq => gq.GroupId == groupId && gq.QuizId == quizId)
                    .ExecuteDeleteAsync();
                if (isDelete > 0)
                {
                    _logger.LogInformation("RemoveQuizFromGroup: Success - QuizId={QuizId} removed from GroupId={GroupId}", quizId, groupId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("RemoveQuizFromGroup: No matching quiz in group to remove - QuizId={QuizId}, GroupId={GroupId}", quizId, groupId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveQuizFromGroup: Error removing QuizId={QuizId} from GroupId={GroupId}", quizId, groupId);
                return false;
            }
        }

        public async Task<bool> RemoveStudentFromGroup(int groupId, int studentId)
        {
            _logger.LogInformation("RemoveStudentFromGroup: Start - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
            try
            {
                int isDelete = await _appDbContext.studentGroups
                    .Where(sg => sg.GroupId == groupId && sg.StudentId == studentId)
                    .ExecuteDeleteAsync();
                if (isDelete > 0)
                {
                    _logger.LogInformation("RemoveStudentFromGroup: Success - StudentId={StudentId} removed from GroupId={GroupId}", studentId, groupId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("RemoveStudentFromGroup: No student found in group to remove - StudentId={StudentId}, GroupId={GroupId}", studentId, groupId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveStudentFromGroup: Error removing StudentId={StudentId} from GroupId={GroupId}", studentId, groupId);
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
