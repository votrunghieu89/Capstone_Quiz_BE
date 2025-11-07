using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Capstone.Model;
using Capstone.RabbitMQ;
using Capstone.Repositories.Quizzes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Capstone.Services
{
    public class OfflineQuizService : IOfflineQuizRepository
    {
        private readonly AppDbContext _context;
        private readonly Redis _redis;
        private readonly ILogger<OfflineQuizService> _logger;
        private readonly IQuizRepository _quizRepository;
        private readonly IRabbitMQProducer _rabbitMQ;

        public OfflineQuizService(AppDbContext context, Redis redis, ILogger<OfflineQuizService> logger, IQuizRepository quizRepository, IRabbitMQProducer rabbitMQ)
        {
            _context = context;
            _redis = redis;
            _logger = logger;
            _quizRepository = quizRepository;
            _rabbitMQ = rabbitMQ;
        }

        // START QUIZ 
        public async Task<bool> StartOfflineQuiz(StartOfflineQuizDTO dto)
        {
            try
            {
                QuizzGroupModel qg = null;
                int quizIdToLoad = dto.QuizId; // lấy QuizId từ DTO

                // kiểm tra QGId nếu được cung cấp
                if (dto.QGId != null && dto.QGId > 0)
                {
                    qg = await _context.quizzGroups.FirstOrDefaultAsync(x => x.QGId == dto.QGId);
                    if (qg == null)
                    {
                        _logger.LogError("Quiz group does not exist (QGId: {QGId})", dto.QGId);
                        return false;
                    }

                    quizIdToLoad = qg.QuizId; // Lấy QuizId từ QG nếu tồn tại

                    // Kiểm tra số lần làm bài, áp dụng khi có QGId
                    var result = await _context.offlineResults
                        .FirstOrDefaultAsync(r => r.StudentId == dto.StudentId && r.QGId == dto.QGId);

                    if (result != null && result.CountAttempts >= qg.MaxAttempts)
                    {
                        _logger.LogError("Exceed the number of times done for QGId: {QGId}", dto.QGId);
                        return false;
                    }
                }

                // tải Quiz dựa trên quizIdToLoad
                var quiz = await _context.quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.QuizId == quizIdToLoad);
                if (quiz == null)
                {
                    _logger.LogError("No quiz found for QuizId: {QuizId}", quizIdToLoad);
                    return false;
                }

                var redisKey = $"offline_quiz:{dto.StudentId}:{quiz.QuizId}";
                var existingCacheJson = await _redis.GetStringAsync(redisKey);
                OfflineQuizCacheDTO cache;

                if (existingCacheJson != null)
                {
                    cache = JsonSerializer.Deserialize<OfflineQuizCacheDTO>(existingCacheJson)!;
                }
                else
                {
                    cache = new OfflineQuizCacheDTO
                    {
                        QuizId = quiz.QuizId,
                        StudentId = dto.StudentId,
                        NumberOfCorrectAnswer = 0,
                        NumberOfWrongAnswer = 0,
                        TotalQuestion = quiz.Questions.Count(q => q.IsDeleted == false),
                        StartTime = dto.StartTime,
                        WrongAnswers = new(),
                        AnsweredQuestions = new(),
                        TotalMaxScore = quiz.Questions.Where(q => !q.IsDeleted).Sum(q => q.Score),
                        TotalScoreEarned = 0,
                    };
                }

                await _redis.SetStringAsync(
                    redisKey,
                    JsonSerializer.Serialize(cache),
                    TimeSpan.FromHours(2)
                );

                _logger.LogInformation($"Offline quiz started/resumed: Student {dto.StudentId} - Quiz {quiz.QuizId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting offline quiz.");
                return false;
            }
        }

        // PROCESS STUDENT ANSWER (Không thay đổi logic)
        public async Task<bool> ProcessStudentAnswer(StudentAnswerSubmissionDTO dto)
        {
            var redisKey = $"offline_quiz:{dto.StudentId}:{dto.QuizId}";
            var json = await _redis.GetStringAsync(redisKey);

            if (json == null)
            {
                _logger.LogWarning("Session not found or expired for key: {Key}", redisKey);
                throw new Exception("Session not found or expired.");
            }

            var cache = JsonSerializer.Deserialize<OfflineQuizCacheDTO>(json)!;

            if (cache.AnsweredQuestions.Contains(dto.QuestionId))
            {
                _logger.LogInformation($"Question {dto.QuestionId} already answered. Skipping re-submission.");
                return true;
            }

            var answerCheckKey = $"quiz_questions_{dto.QuizId}:question_{dto.QuestionId}:option_{dto.SelectedOptionId}";
            var isCorrectJson = await _redis.GetStringAsync(answerCheckKey);
            bool isCorrect = false;

            if (isCorrectJson != null)
            {
                isCorrect = isCorrectJson.ToLower() == "true";
            }
            else
            {
                var checkDto = new CheckAnswerDTO
                {
                    QuizId = dto.QuizId,
                    QuestionId = dto.QuestionId,
                    OptionId = (int)dto.SelectedOptionId
                };
                isCorrect = await _quizRepository.checkAnswer(checkDto);
            }
            var questionScore = await _context.questions
              .Where(q => q.QuestionId == dto.QuestionId)
              .Select(q => q.Score)
              .FirstOrDefaultAsync();

            if (isCorrect)
            {
                cache.NumberOfCorrectAnswer++;
                cache.TotalScoreEarned += questionScore;
            }
            else
            {
                cache.NumberOfWrongAnswer++;
                var correctOptionDTO = await _quizRepository.getCorrectAnswer(new GetCorrectAnswer
                {
                    QuizId = dto.QuizId,
                    QuestionId = dto.QuestionId
                });
                cache.WrongAnswers.Add(new WrongAnswerDTO
                {
                    QuestionId = dto.QuestionId,
                    SelectedOptionId = dto.SelectedOptionId,
                    CorrectOptionId = correctOptionDTO?.OptionId
                });
            }

            cache.AnsweredQuestions.Add(dto.QuestionId);
            await _redis.SetStringAsync(redisKey, JsonSerializer.Serialize(cache), TimeSpan.FromHours(2));
            return true;
        }

        // SUBMIT QUIZ
        public async Task<OfflineResultViewDTO?> SubmitOfflineQuiz(FinishOfflineQuizDTO dto, int accountId, string ipAddress)
        {
            try
            {
                var key = $"offline_quiz:{dto.StudentId}:{dto.QuizId}";
                var json = await _redis.GetStringAsync(key);
                if (json == null)
                {
                    _logger.LogError("Quiz data not found in Redis for submit.");
                    return null;
                }

                var cache = JsonSerializer.Deserialize<OfflineQuizCacheDTO>(json)!;
                cache.EndTime = dto.EndTime;
                cache.Duration = (int)(cache.EndTime!.Value - cache.StartTime).TotalSeconds;

                QuizzGroupModel qg = null;
                int maxAttempts = 1;

                if (dto.QGId != null && dto.QGId > 0)
                {
                    qg = await _context.quizzGroups.FirstOrDefaultAsync(x => x.QGId == dto.QGId);
                    if (qg == null)
                    {
                        _logger.LogError("Quiz group does not exist for QGId: {QGId}.", dto.QGId);
                        return null;
                    }
                    maxAttempts = qg.MaxAttempts;
                }

                var existing = await _context.offlineResults
                    .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.QGId == dto.QGId && x.QuizId == dto.QuizId);

                int totalMax = cache.TotalMaxScore > 0 ? cache.TotalMaxScore : 1;
                int score = (int)Math.Round((double)cache.TotalScoreEarned / totalMax * 100, 0);

                OfflineResultModel currentResult;

                if (existing != null && qg != null && existing.CountAttempts >= qg.MaxAttempts)
                {
                    _logger.LogError("Exceed the number of times for QGId: {QGId}", dto.QGId);
                    return null;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (existing == null)
                    {
                        var newResult = new OfflineResultModel
                        {
                            QGId = dto.QGId,
                            GroupId = qg?.GroupId,
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
                        currentResult = newResult;
                    }
                    else
                    {
                        var oldWrongAnswers = await _context.offlineWrongAnswers
                            .Where(wa => wa.OffResultId == existing.OffResultId)
                            .ToListAsync();
                        _context.offlineWrongAnswers.RemoveRange(oldWrongAnswers);

                        existing.CorrecCount = cache.NumberOfCorrectAnswer;
                        existing.WrongCount = cache.NumberOfWrongAnswer;
                        existing.TotalQuestion = cache.TotalQuestion;
                        existing.StartDate = cache.StartTime;
                        existing.EndDate = cache.EndTime.Value;
                        existing.Duration = cache.Duration;
                        existing.Score = score;
                        existing.CountAttempts += 1;
                        _context.offlineResults.Update(existing);
                        currentResult = existing;
                    }

                    // Lưu kết quả (OfflineResult) trước để lấy ID
                    await _context.SaveChangesAsync();
                    int offResultId = currentResult.OffResultId;
                    //  LƯU CÁC CÂU TRẢ LỜI SAI MỚI TỪ CACHE
                    var wrongAnswerEntities = cache.WrongAnswers.Select(wa => new OfflineWrongAnswerModel
                    {
                        OffResultId = offResultId,
                        QuestionId = wa.QuestionId,
                        SelectedOptionId = wa.SelectedOptionId,
                        CorrectOptionId = wa.CorrectOptionId,
                        CreateAt = DateTime.Now
                    }).ToList();

                    if (wrongAnswerEntities.Any())
                    {
                        await _context.offlineWrongAnswers.AddRangeAsync(wrongAnswerEntities);
                    }

                    // Lưu các câu trả lời sai
                    await _context.SaveChangesAsync();


                    //Cap nhat bang Report
                    if (dto.QGId != null)
                    {
                        // Lấy kết quả của nhóm này tính toán lại
                        var allResultsForGroup = await _context.offlineResults
                            .Where(r => r.QGId == dto.QGId)
                            .ToListAsync();

                        if (allResultsForGroup.Any())
                        {
                            // Tính toán các chỉ số mới
                            int totalParticipants = allResultsForGroup.Count;
                            int highestScore = allResultsForGroup.Max(r => r.Score);
                            int lowestScore = allResultsForGroup.Min(r => r.Score);

                            decimal averageScore = Math.Round((decimal)allResultsForGroup.Average(r => r.Score), 2);

                            // Tìm hoặc Tạo Report
                            var report = await _context.offlinereports
                                .FirstOrDefaultAsync(r => r.QGId == dto.QGId);

                            if (report == null)
                            {
                                var quizTitle = (await _context.quizzes
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(q => q.QuizId == dto.QuizId))?.Title ?? "Report";

                                // Tạo mới Report
                                report = new OfflineReportsModel
                                {
                                    QGId = (int)dto.QGId,
                                    QuizId = dto.QuizId,
                                    ReportName = $"Report: {quizTitle}", // Tên mặc định
                                    HighestScore = highestScore,
                                    LowestScore = lowestScore,
                                    AverageScore = averageScore,
                                    TotalParticipants = totalParticipants,
                                    CreateAt = DateTime.UtcNow
                                };
                                _context.offlinereports.Add(report);
                            }
                            else
                            {
                                // Cập nhật Report đã có
                                report.HighestScore = highestScore;
                                report.LowestScore = lowestScore;
                                report.AverageScore = averageScore;
                                report.TotalParticipants = totalParticipants;
                                _context.offlinereports.Update(report);
                            }

                            // lưu thay đổi của Report 
                            await _context.SaveChangesAsync();
                        }
                    }

                    // CẬP NHẬT RANK TRONG REDIS (Sử dụng Sorted Set ZSET)
                    string rankKey;
                    if (dto.QGId != null)
                    {
                        rankKey = $"quiz_group_rank:{dto.QGId}";
                    }
                    else
                    {
                        rankKey = $"quiz_public_rank:{dto.QuizId}";
                    }

                    await _redis.ZAddAsync(rankKey, dto.StudentId.ToString(), score);
                    var rank = await _redis.ZRankAsync(rankKey, dto.StudentId.ToString(), descending: true);

                    if (rank.HasValue)
                    {
                        currentResult.RANK = (int)rank.Value + 1;
                        _context.offlineResults.Update(currentResult);
                        await _context.SaveChangesAsync(); // Lưu Rank
                    }

                    // Commit toàn bộ
                    await transaction.CommitAsync();

                    // THÊM AUDIT LOG (RabbitMQ) - Đặt sau khi commit thành công
                    var log = new AuditLogModel()
                    {
                        AccountId = accountId,
                        Action = "Insert offline quiz result",
                        Description = $"Student ID:{accountId} submitted quiz ID:{dto.QuizId} (Group Quiz ID: {dto.QGId ?? 0}) with score: {score}%",
                        Timestamp = DateTime.Now,
                        IpAddress = ipAddress
                    };
                    await _rabbitMQ.SendMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(log));


                    // Xóa Cache phiên làm bài
                    await _redis.DeleteKeyAsync(key);

                    // Trả về DTO kết quả
                    return new OfflineResultViewDTO
                    {
                        QuizId = dto.QuizId,
                        QuizTitle = _context.quizzes.FirstOrDefault(q => q.QuizId == dto.QuizId)?.Title ?? "N/A",
                        CorrectCount = cache.NumberOfCorrectAnswer,
                        WrongCount = cache.NumberOfWrongAnswer,
                        TotalQuestion = cache.TotalQuestion,
                        CountAttempts = currentResult.CountAttempts,
                        MaxAttempts = maxAttempts,
                        Score = score,
                        StartDate = cache.StartTime,
                        EndDate = cache.EndTime,
                        Duration = cache.Duration,
                        RANK = currentResult.RANK
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed during submit, rolled back.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting offline quiz.");
                return null;
            }
        }

        // GET OFFLINE RESULT
        public async Task<OfflineResultViewDTO?> GetOfflineResult(int studentId, int quizId)
        {
            try
            {
                // Lấy kết quả mới nhất của học sinh cho quiz đó
                // (Ưu tiên kết quả có QGId nếu có nhiều)
                var result = await (
                    from r in _context.offlineResults
                    join q in _context.quizzes on r.QuizId equals q.QuizId
                    join g in _context.quizzGroups on r.QGId equals g.QGId into qgGroup
                    from g in qgGroup.DefaultIfEmpty() // Cho phép g (Quizz_Group) là NULL
                    where r.StudentId == studentId && r.QuizId == quizId
                    orderby r.QGId descending, r.EndDate descending // Ưu tiên kết quả có QGId, sau đó là mới nhất
                    select new OfflineResultViewDTO
                    {
                        QuizId = r.QuizId,
                        QuizTitle = q.Title,
                        CountAttempts = r.CountAttempts,
                        MaxAttempts = (g != null) ? g.MaxAttempts : 1,
                        CorrectCount = r.CorrecCount,
                        WrongCount = r.WrongCount,
                        TotalQuestion = r.TotalQuestion,
                        Score = r.Score,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                        Duration = r.Duration,
                        RANK = r.RANK
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