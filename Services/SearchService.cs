using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Admin;
using Capstone.DTOs.Group;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.Repositories.Filter_Search;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Capstone.Services
{
    public class SearchService : ISearchRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SearchService> _logger;
        public SearchService(AppDbContext dbContext, ILogger<SearchService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<AllAccountByRoleDTO>> FilterByRole(string role, int page, int pageSize)
        {
            try
            {
                var query = _dbContext.authModels.Where(a => a.Role == role);
                var accountAll =  await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AllAccountByRoleDTO
                    {
                        AccountId = a.AccountId,
                        Email = a.Email,
                        Role = a.Role,
                        IsActive = a.IsActive,
                        CreateAt = a.CreateAt,
                    })
                    .ToListAsync();
                return accountAll;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when filtering accounts by Role: {Role}", role);
                return new List<AllAccountByRoleDTO>();
            }
        }

        public async Task<List<ViewAllQuizDTO>> FilterByTopic(int topic, int page, int pageSize)
        {
            try
            {
               
                var topicName = _dbContext.topics.Where(q => q.TopicId == topic)
                    .Select(q => q.TopicName)
                    .FirstOrDefault();

                var query = _dbContext.quizzes.Where(q => q.TopicId == topic && q.IsPrivate == false).OrderByDescending(q=>q.CreateAt);
                var topicAll = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)               
                    .Select(a => new ViewAllQuizDTO
                    {
                        QuizId = a.QuizId,
                        Title = a.Title,
                        AvatarURL = a.AvatarURL ,
                        TotalQuestions = a.Questions.Count(),
                        TopicName = topicName,
                        TotalParticipants = a.TotalParticipants,

                    }).ToListAsync();
                return topicAll;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when filtering Quiz by Topic: {TopicId}", topic);
                return new List<ViewAllQuizDTO>();
            }
        }

        public async Task<AllAccountByRoleDTO> SearchAccountByEmail(string email)
        {
            try
            {
                // Trim spaces + convert to lowercase
                var normalizedEmail = email?.Trim().ToLower();

                var account = await _dbContext.authModels
                    .Where(a => a.Email.ToLower() == normalizedEmail)
                    .Select(a => new AllAccountByRoleDTO
                    {
                        AccountId = a.AccountId,
                        Email = a.Email,
                        Role = a.Role,
                        IsActive = a.IsActive,
                        CreateAt = a.CreateAt,
                    })
                    .FirstOrDefaultAsync();

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when searching for account by email : {Email}", email);
                return null;
            }
        }

        public async Task<List<ViewStudentDTO>> SearchParticipantInGroup(string name, int groupId)
        {
            _logger.LogInformation("SearchParticipantInGroup: Start - GroupId={GroupId}, Name={Name}", groupId, name);

            try
            {
                var normalizedName = name.ToLower();

                var students = await (from sg in _dbContext.studentGroups
                                      join sp in _dbContext.studentProfiles on sg.StudentId equals sp.StudentId
                                      join a in _dbContext.authModels on sp.StudentId equals a.AccountId
                                      where sg.GroupId == groupId &&
                                            sp.FullName.ToLower().Contains(normalizedName)
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
                    _logger.LogInformation("SearchParticipantInGroup: Found {Count} students matching '{Name}' in GroupId={GroupId}", students.Count, name, groupId);
                    return students;
                }
                else
                {
                    _logger.LogInformation("SearchParticipantInGroup: No students found matching '{Name}' in GroupId={GroupId}", name, groupId);
                    return new List<ViewStudentDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchParticipantInGroup: Error while searching for students with name '{Name}' in GroupId={GroupId}", name, groupId);
                return new List<ViewStudentDTO>();
            }
        }


        public async Task<List<SearchStudentInOfflineReportDTO>> SearchStudentInOfflineReport(string Name, int reportId)
        {
            try
            {
                var normalName = Name.ToLower();

                var report = await _dbContext.offlinereports
                                             .FirstOrDefaultAsync(r => r.OfflineReportId == reportId);
                if (report == null)
                {
                    _logger.LogWarning("OfflineReport not found: {reportId}", reportId);
                    return new List<SearchStudentInOfflineReportDTO>();
                }

                var qgId = report.QGId;
                var quizzGroup = await _dbContext.quizzGroups
                                                 .FirstOrDefaultAsync(q => q.QGId == qgId);
                if (quizzGroup == null)
                {
                    _logger.LogWarning("Quizz_Group not found: {qgId}", qgId);
                    return new List<SearchStudentInOfflineReportDTO>();
                }

                int quizId = quizzGroup.QuizId;
                int totalQuestions = await _dbContext.questions
                                                     .CountAsync(q => q.QuizId == quizId && !q.IsDeleted);

                
                var allResults = await _dbContext.offlineResults
                    .Where(r => r.QGId == qgId)
                    .ToListAsync();

                var summaries = allResults
                    .GroupBy(r => r.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        CountAttempts = g.Count(),
                        BestAttempt = g.OrderByDescending(r => r.Score).First()
                    })
                    .ToList();

                var studentProfiles = await _dbContext.studentProfiles.ToListAsync();

                var result = (from s in summaries
                              join sp in studentProfiles on s.StudentId equals sp.StudentId
                              where sp.FullName.ToLower().Contains(normalName)
                              select new SearchStudentInOfflineReportDTO
                              {
                                  Fullname = sp.FullName,
                                  Rank = s.BestAttempt.RANK,
                                  NumberOfCorrectAnswers = s.BestAttempt.CorrecCount,
                                  NumberOfWrongAnswers = s.BestAttempt.WrongCount,
                                  FinalScore = s.BestAttempt.Score,
                                  TotalQuestions = totalQuestions,
                                  CountAttempts = s.CountAttempts
                              }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching student in report {reportId} with name {Name}", reportId, Name);
                return new List<SearchStudentInOfflineReportDTO>();
            }
        }


        public async Task<List<ViewOnlineStudentReportEachQuizDTO>> SearchStudentInOnlineReport(string Name, int reportId)
        {
            try
            {
                var nomalName = Name.ToLower();

                var firstResult = await _dbContext.onlineResults
                                                  .FirstOrDefaultAsync(r => r.OnlineReportId == reportId);

                if (firstResult == null)
                {
                    _logger.LogWarning("No results found for OnlineReportId: {reportId}", reportId);
                    return new List<ViewOnlineStudentReportEachQuizDTO>();
                }

                int? totalQuestions = firstResult.TotalQuestion;


                var query = _dbContext.onlineResults
                    .Where(result =>
                        result.OnlineReportId == reportId &&
                        result.StudentName.ToLower().Contains(nomalName))
                    .Select(result => new ViewOnlineStudentReportEachQuizDTO
                    {
                        StudentName = result.StudentName,
                        Score = result.Score,
                        CorrectCount = result.CorrecCount,
                        WrongCount = result.WrongCount,
                        TotalQuestion = totalQuestions,
                        Rank = result.Rank
                    });

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while searching for online report {reportId} with name {Name}", reportId, Name);
                return new List<ViewOnlineStudentReportEachQuizDTO>();
            }
        }
    }
    
}
