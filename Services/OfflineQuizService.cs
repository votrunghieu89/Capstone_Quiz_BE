using Capstone.Database;
using Capstone.DTOs;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Capstone.Services
{
    public class OfflineQuizService : IOfflineQuizRepository
    {
        private readonly AppDbContext _context;
        private readonly Redis _redis;
        private readonly ILogger<OfflineQuizService> _logger;
        public OfflineQuizService(AppDbContext context, Redis redis, ILogger<OfflineQuizService> logger)
        {
            _context = context;
            _redis = redis;
            _logger = logger;
        }

        public async Task<bool> StartOfflineQuiz(StartOfflineQuizDTO dto)
        {
            try
            {
                var qg = await _context.quizzGroups
                .FirstOrDefaultAsync(x => x.QGId == dto.QGId);

                if (qg == null)
                {
                    _logger.LogError("Quiz group does not exist");
                    return false;
                }

                var result = await _context.offlineResults
                    .FirstOrDefaultAsync(r => r.StudentId == dto.StudentId && r.QGId == dto.QGId);

                if (result != null && result.CountAttempts >= qg.MaxAttempts)
                {
                    _logger.LogError("Exceed the number of times done");
                    return false;
                }


                var quiz = await _context.quizzes
                            .Include(q => q.Questions)
                        .FirstOrDefaultAsync(q => q.QuizId == qg.QuizId);

                if (quiz == null)
                {
                    _logger.LogError("No quiz found");
                    return false;
                }


                // Tạo dữ liệu Redis
                var cache = new OfflineQuizCacheDTO
                {
                    QuizId = quiz.QuizId,
                    StudentId = dto.StudentId,
                    NumberOfCorrectAnswer = 0,
                    NumberOfWrongAnswer = 0,
                    TotalQuestion = quiz.Questions.Count,
                    StartTime = DateTime.UtcNow
                };

                await _redis.SetStringAsync(
                    $"offline_quiz:{dto.StudentId}:{quiz.QuizId}",
                    JsonSerializer.Serialize(cache),
                    TimeSpan.FromHours(2)
                );

                _logger.LogInformation($"Offline quiz started: Student {dto.StudentId} - Quiz {quiz.QuizId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting offline quiz.");
                return false;
            }
        }
            

        public async Task<OfflineResultViewDTO> SubmitOfflineQuiz(FinishOfflineQuizDTO dto)
        {
            try
            {
                var key = $"offline_quiz:{dto.StudentId}:{dto.QuizId}";
                var json = await _redis.GetStringAsync(key);
                if (json == null)
                {
                    _logger.LogError("Quiz data not found in Redis");
                    return null;
                }

                var cache = JsonSerializer.Deserialize<OfflineQuizCacheDTO>(json);
                cache.EndTime = DateTime.UtcNow;
                cache.Duration = (int)(cache.EndTime.Value - cache.StartTime).TotalSeconds;

                var qg = await _context.quizzGroups.FirstOrDefaultAsync(x => x.QGId == dto.QGId);
                if (qg == null)
                {
                    _logger.LogError("Quiz group does not exist.");
                    return null;
                }

                var existing = await _context.offlineResults
                    .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.QGId == dto.QGId);

                int score = (int)Math.Round((double)cache.NumberOfCorrectAnswer / cache.TotalQuestion * 100, 2);

                if (existing == null)
                {
                    var newResult = new OfflineResultModel
                    {
                        QGId = dto.QGId,
                        GroupId = qg.GroupId,
                        StudentId = dto.StudentId,
                        QuizId = dto.QuizId,
                        CorrecCount = cache.NumberOfCorrectAnswer,
                        WrongCount = cache.NumberOfWrongAnswer,
                        TotalQuestion = cache.TotalQuestion,
                        StartDate = cache.StartTime,
                        EndDate = cache.EndTime.Value,
                        Duration = cache.Duration,
                        Score = score,
                        CountAttempts = 1
                    };
                    _context.offlineResults.Add(newResult);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (existing.CountAttempts >= qg.MaxAttempts)
                    {
                        _logger.LogError("Exceed the number of times");
                        return null;
                    }

                    existing.CorrecCount = cache.NumberOfCorrectAnswer;
                    existing.WrongCount = cache.NumberOfWrongAnswer;
                    existing.TotalQuestion = cache.TotalQuestion;
                    existing.StartDate = cache.StartTime;
                    existing.EndDate = cache.EndTime.Value;
                    existing.Duration = cache.Duration;
                    existing.Score = score;
                    existing.CountAttempts += 1;
                    _context.offlineResults.Update(existing);
                    await _context.SaveChangesAsync();
                }

                await _redis.DeleteKeyAsync(key);

                return new OfflineResultViewDTO
                {
                    QuizId = dto.QuizId,
                    QuizTitle = _context.quizzes.FirstOrDefault(q => q.QuizId == dto.QuizId)?.Title ?? "N/A",
                    CorrectCount = cache.NumberOfCorrectAnswer,
                    WrongCount = cache.NumberOfWrongAnswer,
                    TotalQuestion = cache.TotalQuestion,
                    CountAttempts = existing?.CountAttempts ?? 1,
                    MaxAttempts = qg.MaxAttempts,
                    Score = score,
                    StartDate = cache.StartTime,
                    EndDate = cache.EndTime,
                    Duration = cache.Duration
                };
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "Error submitting offline quiz.");
                return null;
            }

        }

        public async Task<OfflineResultViewDTO?> GetOfflineResult(int studentId, int quizId)
        {
            try
            {
                var result = await (
                from r in _context.offlineResults
                join q in _context.quizzes on r.QuizId equals q.QuizId
                join g in _context.quizzGroups on r.QGId equals g.QGId
                where r.StudentId == studentId && r.QuizId == quizId
                select new OfflineResultViewDTO
                {
                    QuizId = r.QuizId,
                    QuizTitle = q.Title,
                    CountAttempts = r.CountAttempts,
                    MaxAttempts = g.MaxAttempts,
                    CorrectCount = r.CorrecCount,
                    WrongCount = r.WrongCount,
                    TotalQuestion = r.TotalQuestion,
                    Score = r.Score,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    Duration = r.Duration
                }).FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {             
                _logger.LogError(ex, "Error getting offline quiz result.");
                return null;                
            }
        }
    }
}
    

