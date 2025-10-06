using Capstone.Database;
using Capstone.DTOs.Admin;
using Capstone.Repositories.Admin;
using Google.Apis.Upload;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace Capstone.Services
{
    public class AdminService : IAdminRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AdminService> _logger;
        public AdminService (AppDbContext appDbContext , ILogger<AdminService> logger)
        {
            _dbContext = appDbContext;
            _logger = logger;
        }
        public async Task<bool> DeleteAccount(int accountId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var account = await _dbContext.authModels.FirstOrDefaultAsync(a => a.AccountId == accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {accountId} not found.", accountId);
                    return false;
                }

                if (account.Role == "Student")
                {
                    // xoá dữ liệu liên quan đến Student
                    await _dbContext.offlineResults.Where(r => r.StudentId == accountId).ExecuteDeleteAsync();
                    await _dbContext.onlineResults.Where(r => r.StudentName == account.Email).ExecuteDeleteAsync();
                    await _dbContext.quizzFavourites.Where(f => f.AccountId == accountId).ExecuteDeleteAsync();
                    await _dbContext.studentGroups.Where(sg => sg.StudentId == accountId).ExecuteDeleteAsync();
                    await _dbContext.studentProfiles.Where(p => p.StudentId == accountId).ExecuteDeleteAsync();
                }
                else if (account.Role == "Teacher")
                {
                    // lấy tất cả QuizId của Teacher
                    var quizIds = await _dbContext.quizzes
                        .Where(q => q.TeacherId == accountId)
                        .Select(q => q.QuizId)
                        .ToListAsync();

                    if (quizIds.Any())
                    {
                        // xoá Options liên quan tới Question trong Quiz
                        var questionIds = await _dbContext.questions
                            .Where(q => quizIds.Contains(q.QuizId))
                            .Select(q => q.QuestionId)
                            .ToListAsync();

                        if (questionIds.Any())
                        {
                            await _dbContext.options
                                .Where(o => questionIds.Contains(o.QuestionId))
                                .ExecuteDeleteAsync();
                        }

                        // xoá OnlineResults và OfflineResults liên quan tới các Quiz
                        await _dbContext.onlineResults
                            .Where(r => quizIds.Contains(r.QuizId))
                            .ExecuteDeleteAsync();

                        await _dbContext.offlineResults
                            .Where(r => quizIds.Contains(r.QuizId))
                            .ExecuteDeleteAsync();

                        // xoá Questions, QuestionStats, Quiz_Group và Quizzes
                        await _dbContext.questions
                            .Where(q => quizIds.Contains(q.QuizId))
                            .ExecuteDeleteAsync();

                        await _dbContext.questionStats
                            .Where(qs => quizIds.Contains(qs.QuizId))
                            .ExecuteDeleteAsync();

                        await _dbContext.quizzGroups
                            .Where(qg => quizIds.Contains(qg.QuizId))
                            .ExecuteDeleteAsync();

                        await _dbContext.quizzes
                            .Where(q => quizIds.Contains(q.QuizId))
                            .ExecuteDeleteAsync();
                    }
  

                    // lấy tất cả GroupId của Teacher
                    var groupIds = await _dbContext.groups
                        .Where(g => g.TeacherId == accountId)
                        .Select(g => g.GroupId)
                        .ToListAsync();

                    if (groupIds.Any())
                    {
                        await _dbContext.studentGroups.Where(sg => groupIds.Contains(sg.GroupId)).ExecuteDeleteAsync();
                        await _dbContext.quizzGroups.Where(qg => groupIds.Contains(qg.GroupId)).ExecuteDeleteAsync();
                        await _dbContext.groups.Where(g => groupIds.Contains(g.GroupId)).ExecuteDeleteAsync();
                    }

                    // xoá Folder và TeacherProfile
                    await _dbContext.quizzFolders.Where(f => f.TeacherId == accountId).ExecuteDeleteAsync();
                    await _dbContext.teacherProfiles.Where(tp => tp.TeacherId == accountId).ExecuteDeleteAsync();
                }

                // xoá Account
                await _dbContext.authModels.Where(a => a.AccountId == accountId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Deleted account {accountId} with role {role}", accountId, account.Role);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting account {accountId}", accountId);
                return false;
            }
        }



        public async Task<List<AllAccountByRoleDTO>> GetAllAccountByRole(int page, int pageSize)
        {
            try
            {
                return await _dbContext.authModels
                    .OrderBy(acc => acc.AccountId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(acc => new AllAccountByRoleDTO
                    {
                        Email = acc.Email,
                        Role = acc.Role,
                        CreateAt = acc.CreateAt,
                        UpdateAt = (DateTime)acc.UpdateAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
               
                return new List<AllAccountByRoleDTO>();
            }
        }

        public async Task<int> GetNumberOfCreatedAccount()
        {
            try
            {
                int totalAccount = await _dbContext.authModels.CountAsync();
                return totalAccount;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting total Account create");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedAccountByMonth(int month, int year)
        {
            try
            {
                int totalAccountByMonth = await _dbContext.authModels
                    .Where(acc => acc.CreateAt.Month == month && acc.CreateAt.Year == year)
                    .CountAsync();
                return totalAccountByMonth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account create by Month ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedQuizzes()
        {
            try
            {
                int totalQuizzes = await _dbContext.quizzes.CountAsync(); 
                return totalQuizzes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Quizzes create ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedQuizzesByMonth(int month, int year)
        {
            try
            {
                int totalQuizzesByMotnh = await _dbContext.quizzes
                    .Where(quizzes => quizzes.CreateAt.Month == month 
                        && quizzes.CreateAt.Year == year)
                    .CountAsync();
                return totalQuizzesByMotnh;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Quizzes create by Month ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedStudentAcount()
        {
            try
            {
                int totalAccountStudent = await _dbContext.authModels
                    .Where(accStudent => accStudent.Role == "Student")
                    .CountAsync();
                return totalAccountStudent; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Student create");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedStudentAcountByMonth(int month, int year) 
        {
            try
            {
                int totalAccountStudentByMonth = await _dbContext.authModels
                    .Where(accStudent => accStudent.Role == "Student" 
                        && accStudent.CreateAt.Month == month 
                        && accStudent.CreateAt.Year == year)
                    .CountAsync();
                return totalAccountStudentByMonth;
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Student create by Month ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedTeacherAccount()
        {
            try
            {
                int totalAccountTeacher = await _dbContext.authModels.Where(accTeacher => accTeacher.Role == "Teacher").CountAsync();
                return totalAccountTeacher;
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Teacher create");
                return 0;
            }
            
        }

        public async Task<int> GetNumberOfCreatedTeacherAccountByMonth(int month, int year)
        {
            try
            {
                int toltalAccountTeacherByMonth = await _dbContext.authModels
                    .Where(accTeacher => accTeacher.Role == "Teacher"
                        && accTeacher.CreateAt.Month == month
                        && accTeacher.CreateAt.Year == year)
                    .CountAsync();
                return toltalAccountTeacherByMonth;                
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Teacher create by Month ");
                return 0;
            }
        }
    }
}
