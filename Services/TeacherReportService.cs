using Capstone.Database;
using Capstone.DTOs.Reports.Teacher;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.ENUMs;
using Capstone.Repositories.Histories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Services
{
    public class TeacherReportService : ITeacherReportRepository
    {
        private readonly ILogger<TeacherReportService> _logger;
        private readonly AppDbContext _context;
        private readonly string _connectionString;
        public TeacherReportService(ILogger<TeacherReportService> logger, AppDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }



        public async Task<bool> checkExpiredTime(int QGId, int QuizId)
        {
            try
            {
                var quizzGroup = await _context.quizzGroups
                    .Where(qg => qg.QGId == QGId && qg.QuizId == QuizId)
                    .FirstOrDefaultAsync();

                if (quizzGroup == null)
                {
                    _logger.LogWarning("QuizzGroup not found with QGId={QGId}, QuizId={QuizId}", QGId, QuizId);
                    return false;
                }

                _logger.LogWarning(
                    "Debug ExpiredTime: DB={dbTime:o} (Kind={kind}), UtcNow={utc:o}, LocalNow={local:o}",
                    quizzGroup.ExpiredTime,
                    quizzGroup.ExpiredTime.Kind,
                    DateTime.UtcNow,
                    DateTime.Now
                );

                if (quizzGroup.ExpiredTime <= DateTime.UtcNow) // Dùng UTC để đúng timezone
                {
                    int isUpdate = await _context.quizzGroups
                        .Where(qg => qg.QGId == QGId && qg.QuizId == QuizId)
                        .ExecuteUpdateAsync(u => u.SetProperty(qg => qg.Status, "Completed"));

                    _logger.LogInformation("QuizId={QuizId}, QGId={QGId} expired → Updated to Completed", QuizId, QGId);
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

        public async Task<bool> EndNow(int groupId, int quizzId)
        {
            try
            {

                int isUpdate = await _context.quizzGroups
                    .Where(qg => qg.QuizId == quizzId && qg.GroupId == groupId)
                    .ExecuteUpdateAsync(u => u.SetProperty(qg => qg.Status, "Completed")
                                                .SetProperty(qg => qg.ExpiredTime, qg => qg.CreateAt));
                if (isUpdate > 0)
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
        public async Task<DetailOfQuestionDTO> ViewDetailOfQuestion(int questionId)
        {
            try
            {
                List<OptionsDTO> options = await (from o in _context.options
                                                  where o.QuestionId == questionId
                                                  select new OptionsDTO
                                                  {
                                                      OptionId = o.OptionId,
                                                      OptionContent = o.OptionContent,
                                                      IsCorrect = o.IsCorrect
                                                  }).ToListAsync();
                var question = await _context.questions.Where(q => q.QuestionId == questionId)
                                    .Select(q => new
                                    {
                                        q.QuestionContent,
                                        q.Time
                                    }).FirstOrDefaultAsync();
                if (question == null)
                {
                    _logger.LogWarning("Question with ID {questionId} not found", questionId);
                    return null;
                }
                DetailOfQuestionDTO result = new DetailOfQuestionDTO
                {
                    QuestionContent = question?.QuestionContent ?? string.Empty,
                    Time = question?.Time ?? 0,
                    options = options
                };
                Console.WriteLine(result.options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ViewDetailOfQuestion");
                return null;
            }
        }


        //  Offline Quiz
        public async Task<ExpiredEnum> ChangeExpiredTime(int QGId, int quizzId, DateTime newExpiredTime)
        {
            try
            {
                int isUpdate = await _context.quizzGroups
                    .Where(qg => qg.QuizId == quizzId && qg.QGId == QGId)
                    .ExecuteUpdateAsync(u => u.SetProperty(qg => qg.ExpiredTime, newExpiredTime)
                                             .SetProperty(qg => qg.Status, "Pending"));
                if (isUpdate > 0)
                {
                    _logger.LogInformation("Expired time for QuizId {quizzId} in GroupId {groupId} updated to {newExpiredTime}", quizzId, QGId, newExpiredTime);
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
        public async Task<List<ViewAllOfflineReportDTO>> GetOfflineQuizz(int teacherId)
        {
            try
            {
                List<ViewAllOfflineReportDTO> quizList = new List<ViewAllOfflineReportDTO>();
                var listGroups = await _context.groups
                                  .Where(g => g.TeacherId == teacherId)
                                  .Select(s => new { s.GroupId, s.GroupName })
                                  .ToListAsync();
                foreach (var group in listGroups)
                {
                    int groupId = group.GroupId;
                    List<DeliveredQuizzDetailDTO> quizzes = await (from g in _context.groups
                                                                   join gq in _context.quizzGroups on g.GroupId equals gq.GroupId
                                                                   join r in _context.offlinereports on gq.QGId equals r.QGId
                                                                   where g.GroupId == groupId
                                                                   orderby gq.CreateAt descending
                                                                   select new DeliveredQuizzDetailDTO
                                                                   {
                                                                       OfflineReportId = r.OfflineReportId,
                                                                       QuizzId = gq.QuizId,
                                                                       QGId = gq.QGId,
                                                                       ReportName = r.ReportName,
                                                                       TotalParticipants = r.TotalParticipants,
                                                                       EndTime = gq.ExpiredTime,
                                                                       Status = gq.Status,
                                                                   }).ToListAsync();
                    var dto = new ViewAllOfflineReportDTO
                    {
                        GroupId = groupId,
                        GroupName = group.GroupName,
                        Quizzes = quizzes // ✅ gán list
                    };
                    quizList.Add(dto);
                }
                return quizList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in DeliveredQuizz");
                throw;
            }
        }
        public async Task<ViewOfflineDetailReportEachQuizDTO> OfflineDetailReportEachQuiz(int OfflineReportId, int quizzId)
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
                var reportQuery = from r in _context.offlinereports 
                                  join gq in _context.quizzGroups on r.QGId equals gq.QGId
                                  join g in _context.groups on gq.GroupId equals g.GroupId
                                  where r.OfflineReportId == OfflineReportId && r.QuizId == quizzId
                                  
                                  select new
                                  {
                                      gq.QGId,
                                      g.GroupName,
                                      g.GroupId,
                                      r.ReportName,
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
                var result = new ViewOfflineDetailReportEachQuizDTO
                {
                    QGId = report.QGId,
                    GroupId = report.GroupId,
                    GroupName = report.GroupName,
                    ReportName = report.ReportName,
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
        public async Task<List<ViewOfflineStudentReportEachQuizDTO>> OfflineStudentReportEachQuiz(int quizzId, int QGId, int groupId)
        {
            try
            {
                List<ViewOfflineStudentReportEachQuizDTO> result = await (from or in _context.offlineResults
                                                            join a in _context.studentProfiles on or.StudentId equals a.StudentId
                                                            where or.QuizId == quizzId && or.QGId == QGId && or.GroupId == groupId
                                                            orderby or.RANK ascending
                                                            select new ViewOfflineStudentReportEachQuizDTO
                                                            {
                                                                Fullname = a.FullName,
                                                                Rank = or.RANK,
                                                                NumberOfCorrectAnswers = or.CorrecCount,
                                                                NumberOfWrongAnswers = or.WrongCount,
                                                                TotalQuestions = or.TotalQuestion,
                                                                FinalScore = or.Score,
                                                                CountAttempts = or.CountAttempts
                                                            }).ToListAsync();
                if (result == null || result.Count == 0)
                {
                    _logger.LogInformation("No offline results found for QuizId {quizzId}", quizzId);
                    return new List<ViewOfflineStudentReportEachQuizDTO>(); ;
                }
                _logger.BeginScope("Retrieved {count} offline results for QuizId {quizzId}", result.Count, quizzId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GetOfflineResult");
                return new List<ViewOfflineStudentReportEachQuizDTO>();
            }
        }
        public async Task<List<ViewOfflineQuestionReportEachQuizDTO>> OfflineQuestionReportEachQuiz(int quizId, int QGId, int groupId)
        {
            var result = new List<ViewOfflineQuestionReportEachQuizDTO>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT 
                    q.QuestionId,
                    q.QuestionContent,
                    COUNT(DISTINCT r.OffResultId) AS Total_Answers,
                    COUNT(DISTINCT w.OffWrongId) AS Wrong_Count,
                    COUNT(DISTINCT r.OffResultId) - COUNT(DISTINCT w.OffWrongId) AS Correct_Count,
                    CAST(
                        (COUNT(DISTINCT r.OffResultId) - COUNT(DISTINCT w.OffWrongId)) * 100.0 / 
                        NULLIF(COUNT(DISTINCT r.OffResultId), 0)
                    AS DECIMAL(5,2)) AS Percentage_Correct
                    FROM Questions q
                    JOIN OfflineResults r ON r.QuizId = q.QuizId
                    LEFT JOIN OfflineWrongAnswers w 
                        ON w.QuestionId = q.QuestionId AND w.OffResultId = r.OffResultId
                    WHERE q.QuizId = @quizId
                          AND r.QGId = @QGId
                          AND r.GroupId = @groupId
                    GROUP BY q.QuestionId, q.QuestionContent
                    ORDER BY q.QuestionId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@quizId", quizId);
                        command.Parameters.AddWithValue("@QGId", QGId);
                        command.Parameters.AddWithValue("@groupId", groupId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var dto = new ViewOfflineQuestionReportEachQuizDTO
                                {
                                    QuestionId = reader.GetInt32(reader.GetOrdinal("QuestionId")),
                                    QuestionContent = reader.GetString(reader.GetOrdinal("QuestionContent")),
                                    TotalAnswers = reader.GetInt32(reader.GetOrdinal("Total_Answers")),
                                    WrongCount = reader.GetInt32(reader.GetOrdinal("Wrong_Count")),
                                    CorrectCount = reader.GetInt32(reader.GetOrdinal("Correct_Count")),
                                    PercentageCorrect = reader.GetDecimal(reader.GetOrdinal("Percentage_Correct"))
                                };
                                result.Add(dto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ViewQuestionHistory");
            }

            return result;
        }
        public async Task<bool> ChangeOfflineReport(int OfflineReportId, string newReportName)
        {
            try
            {
                int isUpdate = await _context.offlinereports
                    .Where(r => r.OfflineReportId == OfflineReportId)
                    .ExecuteUpdateAsync(u => u.SetProperty(r => r.ReportName, newReportName));
                if (isUpdate > 0)
                {
                    _logger.LogInformation("ReportId {reportId} name updated to {newReportName}", OfflineReportId, newReportName);
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

        // Online Quiz
        public async Task<List<ViewAllOnlineReportDTO>> GetOnlineQuiz(int teacherId)
        {
            try
            {
                List<ViewAllOnlineReportDTO> result = await _context.onlinereports
                                                            .Where(or => or.TeacherId == teacherId)
                                                            .OrderByDescending(or => or.CreateAt)
                                                            .Select(or => new ViewAllOnlineReportDTO
                                                            {
                                                                OnlineReportId = or.OnlineReportId,
                                                                quizId = or.QuizId,
                                                                ReportName = or.ReportName,
                                                                TotalParticipants = or.TotalParticipants,
                                                                CreatedAt = or.CreateAt
                                                            })
                                                            .ToListAsync();
                if (result == null || result.Count == 0)
                {
                    _logger.BeginScope("No online reports found for TeacherId {teacherId}", teacherId);
                    return new List<ViewAllOnlineReportDTO>();
                }
                _logger.BeginScope("Retrieved {count} online reports for TeacherId {teacherId}", result.Count, teacherId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GetOnlineQuiz");
                return new List<ViewAllOnlineReportDTO>();
            }
        }
        public async Task<ViewOnlineDetailReportEachQuizDTO> OnlineDetailReportEachQuiz(int quizId, int OnlineReportId)
        {
            try
            {
                var totalQuestion = await _context.questions
                     .Where(q => q.QuizId == quizId && q.IsDeleted == false)
                     .CountAsync();
                var CreateBy = await (from q in _context.quizzes
                                      join a in _context.authModels on q.TeacherId equals a.AccountId
                                      where q.QuizId == quizId
                                      select a.Email).FirstOrDefaultAsync();
                var reportQuery = from or in _context.onlinereports
                                  where or.OnlineReportId == OnlineReportId && or.QuizId == quizId
                                  select new
                                  {
                                      or.TotalParticipants,
                                      or.HighestScore,
                                      or.LowestScore,
                                      or.AverageScore,
                                      or.CreateAt,
                                      or.ReportName
                                  };
                var report = await reportQuery.FirstOrDefaultAsync();
                if (report == null) return null;
                var result = new ViewOnlineDetailReportEachQuizDTO
                {
                    TotalStudent = report.TotalParticipants,
                    HighestScore = report.HighestScore,
                    LowestScore = report.LowestScore,
                    AverageScore = report.AverageScore,
                    TotalQuestion = totalQuestion,
                    CreateAt = report.CreateAt,
                    ReportName = report.ReportName,
                    CreateBy = CreateBy
                };
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in DetailReportEachQuiz");
                return null;
            }
        }
        public async Task<List<ViewOnlineStudentReportEachQuizDTO>> OnlineStudentReportEachQuiz(int quizId, int OnlineReportId)
        {
            try
            {
                var result = await _context.onlineResults
                           .Where(or => or.QuizId == quizId && or.OnlineReportId == OnlineReportId)
                           .OrderBy(s => s.Rank)
                           .Select(s => new ViewOnlineStudentReportEachQuizDTO

                           {
                               StudentName = s.StudentName,
                               Score = s.Score,
                               CorrectCount = s.CorrecCount ?? 0,
                               WrongCount = s.WrongCount ?? 0,
                               TotalQuestion = s.TotalQuestion ?? 0,
                               Rank = s.Rank ?? 0
                           })
                           .ToListAsync();
                if (result == null || result.Count == 0)
                {
                    _logger.BeginScope("No online student reports found for QuizId {quizId} and OnlineReportId {OnlineReportId}", quizId, OnlineReportId);
                    return new List<ViewOnlineStudentReportEachQuizDTO>();
                }
                _logger.BeginScope("Retrieved {count} online student reports for QuizId {quizId} and OnlineReportId {OnlineReportId}", result.Count, quizId, OnlineReportId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in StudentReportEachQuiz");
                return null;
            }
        }
        public async Task<List<ViewOnlineQuestionReportEachQuizDTO>> OnlineQuestionReportEachQuiz(int quizId, int onlineReportId)
        {
            var result = new List<ViewOnlineQuestionReportEachQuizDTO>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT 
                    q.QuestionId,
                    q.QuestionContent,
                    COUNT(DISTINCT r.OnlResultId) AS Total_Answers,
                    COUNT(DISTINCT w.OnlWrongId) AS Wrong_Count,
                    COUNT(DISTINCT r.OnlResultId) - COUNT(DISTINCT w.OnlWrongId) AS Correct_Count,
                    CAST(
                        (COUNT(DISTINCT r.OnlResultId) - COUNT(DISTINCT w.OnlWrongId)) * 100.0 / 
                        NULLIF(COUNT(DISTINCT r.OnlResultId), 0)
                    AS DECIMAL(5,2)) AS Percentage_Correct
                    FROM Questions q
                    JOIN OnlineResults r ON r.QuizId = q.QuizId
                    LEFT JOIN OnlineWrongAnswer w 
                        ON w.QuestionId = q.QuestionId AND w.OnlResultId = r.OnlResultId
                    WHERE q.QuizId = @quizId
                      AND r.OnlineReportId = @onlineReportId
                    GROUP BY q.QuestionId, q.QuestionContent
                    ORDER BY q.QuestionId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@quizId", quizId);
                        command.Parameters.AddWithValue("@onlineReportId", onlineReportId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var dto = new ViewOnlineQuestionReportEachQuizDTO
                                {
                                    QuestionId = reader.GetInt32(reader.GetOrdinal("QuestionId")),
                                    QuestionContent = reader.GetString(reader.GetOrdinal("QuestionContent")),
                                    TotalAnswers = reader.GetInt32(reader.GetOrdinal("Total_Answers")),
                                    WrongCount = reader.GetInt32(reader.GetOrdinal("Wrong_Count")),
                                    CorrectCount = reader.GetInt32(reader.GetOrdinal("Correct_Count")),
                                    PercentageCorrect = reader.GetDecimal(reader.GetOrdinal("Percentage_Correct"))
                                };
                                result.Add(dto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in OnlineQuestionReportEachQuiz");
                return null;
            }

            return result;
        }
        public async Task<bool> ChangeOnlineReportName(int onlineReportId, string newReportName)
        {
            try
            {
                int isUpdate = await _context.onlinereports
                    .Where(r => r.OnlineReportId == onlineReportId)
                    .ExecuteUpdateAsync(u => u.SetProperty(r => r.ReportName, newReportName));
                if (isUpdate > 0)
                {
                    _logger.LogInformation("OnlineReportId {onlineReportId} name updated to {newReportName}", onlineReportId, newReportName);
                    return true;
                }    
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ChangeOnlineReportName");
                return false;
            }
        }
    }
}