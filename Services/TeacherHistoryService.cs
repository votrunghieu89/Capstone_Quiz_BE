using Capstone.Database;
using Capstone.DTOs.Reports.Teacher;
using Capstone.ENUMs;
using Capstone.Repositories.Histories;
using Microsoft.EntityFrameworkCore;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Services
{
    public class TeacherHistoryService : IHistoryTeacher
    {
        private readonly ILogger<TeacherHistoryService> _logger;
        private readonly AppDbContext _context;
        public TeacherHistoryService(ILogger<TeacherHistoryService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ExpiredEnum> ChangeExpiredTime(int groupId, int quizzId, DateTime newExpiredTime)
        {
            try
            {
                var quizzGroup = await _context.quizzGroups
                    .Where(qg => qg.QuizId == quizzId && qg.GroupId == groupId)
                    .FirstOrDefaultAsync();
                if (quizzGroup == null)
                    return ExpiredEnumDTO.ExpiredEnum.QuizGroupNotFound;
                if (newExpiredTime <= DateTime.Now)
                    return ExpiredEnumDTO.ExpiredEnum.InvalidExpiredTime;
                int isUpdate = await _context.quizzGroups
                    .Where(qg => qg.QuizId == quizzId && qg.GroupId == groupId)
                    .ExecuteUpdateAsync(u => u.SetProperty(qg => qg.ExpiredTime, newExpiredTime)
                                             .SetProperty(qg => qg.Status, "Pending"));
                if (isUpdate > 0)
                {
                    _logger.LogInformation("Expired time for QuizId {quizzId} in GroupId {groupId} updated to {newExpiredTime}", quizzId, groupId, newExpiredTime);
                    return ExpiredEnumDTO.ExpiredEnum.Success;
                }
                return ExpiredEnumDTO.ExpiredEnum.UpdateFailed;   

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ChangeExpiredTime");
              return ExpiredEnumDTO.ExpiredEnum.Error;
            }
        }

        public async Task<bool> ChangeReportName(int reportId, string newReportName)
        {
            try
            {
                int isUpdate = await _context.reports
                    .Where(r => r.ReportId == reportId)
                    .ExecuteUpdateAsync(u => u.SetProperty(r => r.ReportName, newReportName));
                if (isUpdate > 0)
                {
                    _logger.LogInformation("ReportId {reportId} name updated to {newReportName}", reportId, newReportName);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ChangeReportName");
                return false;
            }
        }

        public async Task<bool> checkExpiredTime(int quizzId, int groupId)
        {
            try
            {
                var quizzGroup = await _context.quizzGroups
                    .Where(qg => qg.QuizId == quizzId && qg.GroupId == groupId)
                    .FirstOrDefaultAsync();
                if (quizzGroup == null)
                    return false;
                if(quizzGroup.ExpiredTime <= DateTime.Now)
                {
                    int isUpdate = await _context.quizzGroups
                        .Where(qg => qg.QuizId == quizzId && qg.GroupId == groupId)
                        .ExecuteUpdateAsync(u => u.SetProperty(qg => qg.Status, "Completed"));
                    _logger.LogInformation("QuizId {quizzId} in GroupId {groupId} has expired and status updated to Completed", quizzId, groupId);
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in checkExpiredTime");
                return false;
            }
        }

        public async Task<List<DeliveredQuizzDTO>> DeliveredQuizz(int teacherId)
        {
            try
            {
                List<DeliveredQuizzDTO> quizList = new List<DeliveredQuizzDTO>();
                var listGroups = await _context.groups
                                  .Where(g => g.TeacherId == teacherId)
                                  .Select(s => new { s.GroupId, s.GroupName })
                                  .ToListAsync();
                foreach (var group in listGroups) { 
                    int groupId  = group.GroupId;
                    List<DeliveredQuizzDetailDTO> quizzes = await (from g in _context.groups
                                        join gq in _context.quizzGroups on g.GroupId equals gq.GroupId
                                        join r in _context.reports on gq.QGId equals r.QGId
                                        where g.GroupId == groupId
                                         select new DeliveredQuizzDetailDTO
                                        {
                                            QuizzId = gq.QuizId,
                                            QuizzName = r.ReportName,
                                            TotalParticipants = r.TotalParticipants,
                                            EndTime = gq.ExpiredTime,
                                            Status = gq.Status,
                                        }).ToListAsync();
                    var dto = new DeliveredQuizzDTO
                    {
                        GroupId = groupId,
                        GroupName = group.GroupName,
                        Quizzes = quizzes // ✅ gán list
                    };
                    quizList.Add(dto);
                }
                return quizList;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred in DeliveredQuizz");
                throw;
            }
        }
        public async Task<bool> EndNow(int groupId, int quizzId)
        {
            try
            {
               
                int isUpdate = await _context.quizzGroups
                    .Where(qg => qg.QuizId == quizzId && qg.GroupId == groupId)
                    .ExecuteUpdateAsync(u => u.SetProperty(qg => qg.Status, "Completed")
                                                .SetProperty(qg => qg.ExpiredTime, qg => qg.CreateAt));
                if(isUpdate > 0)
                    {
                    _logger.LogInformation("QuizId {quizzId} in GroupId {groupId} status updated to Completed", quizzId, groupId);
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in EndNow");
                return false;
            }
        }

        public async Task<DetailOfQuizDTO> ReportDetail(int groupId, int quizzId)
        {
            try
            {
                var totalQuestion = await _context.questions
                    .Where(q => q.QuizId == quizzId && q.IsDeleted == false)
                    .CountAsync();
                var CreateBy = await (from q in _context.quizzes
                                  join a in _context.authModels on q.TeacherId equals a.AccountId
                                  where q.QuizId == quizzId
                                  select a.Email).FirstOrDefaultAsync();
                var reportQuery = from g in _context.groups
                                  join gq in _context.quizzGroups on g.GroupId equals gq.GroupId
                                  join r in _context.reports on gq.QGId equals r.QGId
                                  where gq.GroupId == groupId && gq.QuizId == quizzId
                                  select new
                                  {
                                      g.GroupName,
                                      r.TotalParticipants,
                                      r.HighestScore,
                                      r.LowestScore,
                                      r.AverageScore,
                                      gq.Status,
                                      gq.CreateAt,
                                      gq.ExpiredTime
                                  };

                var report = await reportQuery.FirstOrDefaultAsync();
                if (report == null) return null;
                var result = new DetailOfQuizDTO
                {
                    GroupName = report.GroupName,
                    TotalParticipants = report.TotalParticipants,
                    HighestScore = report.HighestScore,
                    LowestScore = report.LowestScore,
                    AverageScore = report.AverageScore,
                    Status = report.Status,
                    StartDate = report.CreateAt,
                    EndDate = report.ExpiredTime,
                    TotalQuestions = totalQuestion,
                    CreateBy = CreateBy
                };
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ReportDetail");
                return null;
            }
        }
    }
}
