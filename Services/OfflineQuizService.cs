using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace Capstone.Services
{
    public class OfflineQuizService : IOfflineQuizRepository
    {
        private readonly AppDbContext _context;
        private readonly Redis _redis;
        private readonly ILogger<OfflineQuizService> _logger;
        private readonly IQuizRepository _quizRepository; // Inject IQuizRepository để sử dụng cache đáp án

        // Cập nhật Constructor để inject IQuizRepository
        public OfflineQuizService(AppDbContext context, Redis redis, ILogger<OfflineQuizService> logger, IQuizRepository quizRepository)
        {
            _context = context;
            _redis = redis;
            _logger = logger;
            _quizRepository = quizRepository;
        }

        // START QUIZ
        public async Task<bool> StartOfflineQuiz(StartOfflineQuizDTO dto)
        {
            try
            {
                var qg = await _context.quizzGroups.FirstOrDefaultAsync(x => x.QGId == dto.QGId);
                if (qg == null)
                {
                    _logger.LogError("Quiz group does not exist (QGId: {QGId})", dto.QGId);
                    return false;
                }

                var result = await _context.offlineResults
                    .FirstOrDefaultAsync(r => r.StudentId == dto.StudentId && r.QGId == dto.QGId);

                if (result != null && result.CountAttempts >= qg.MaxAttempts)
                {
                    _logger.LogError("Exceed the number of times done for QGId: {QGId}", dto.QGId);
                    return false;
                }

                var quiz = await _context.quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.QuizId == qg.QuizId);
                if (quiz == null)
                {
                    _logger.LogError("No quiz found for QuizId: {QuizId}", qg.QuizId);
                    return false;
                }

                // Lấy cache cũ (nếu có làm dở) hoặc tạo mới
                var redisKey = $"offline_quiz:{dto.StudentId}:{quiz.QuizId}";
                var existingCacheJson = await _redis.GetStringAsync(redisKey);
                OfflineQuizCacheDTO cache;

                if (existingCacheJson != null)
                {
                    cache = JsonSerializer.Deserialize<OfflineQuizCacheDTO>(existingCacheJson)!;
                }
                else
                {
                    // Tạo dữ liệu Redis mới
                    cache = new OfflineQuizCacheDTO
                    {
                        QuizId = quiz.QuizId,
                        StudentId = dto.StudentId,
                        NumberOfCorrectAnswer = 0,
                        NumberOfWrongAnswer = 0,
                        TotalQuestion = quiz.Questions.Count(q => q.IsDeleted == false), // Tính tổng câu hỏi chưa xóa
                        StartTime = DateTime.UtcNow,
                        WrongAnswers = new(),
                        AnsweredQuestions = new() // Khởi tạo HashSet để theo dõi câu hỏi đã trả lời
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

        // PROCESS STUDENT ANSWER (Sử dụng Redis Cache đáp án)
        // Phương thức này nhận từng câu trả lời và cập nhật trạng thái trong Redis
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

            // Nếu câu hỏi đã được trả lời, bỏ qua (hoặc xử lý ghi đè nếu muốn)
            if (cache.AnsweredQuestions.Contains(dto.QuestionId))
            {
                _logger.LogInformation($"Question {dto.QuestionId} already answered. Skipping re-submission.");
                return true;
            }

            // KIỂM TRA ĐÁP ÁN: SỬ DỤNG REDIS CACHE
            // Khóa Redis cho trạng thái đúng/sai của Option
            var answerCheckKey = $"quiz_questions_{dto.QuizId}:question_{dto.QuestionId}:option_{dto.SelectedOptionId}";
            var isCorrectJson = await _redis.GetStringAsync(answerCheckKey);

            bool isCorrect = false;

            if (isCorrectJson != null)
            {
                isCorrect = isCorrectJson.ToLower() == "true";
            }
            else
            {
                // FALLBACK: Cache không tồn tại, gọi QuizService (đã có logic DB fallback + tái tạo cache)
                var checkDto = new CheckAnswerDTO
                {
                    QuizId = dto.QuizId,
                    QuestionId = dto.QuestionId,
                    OptionId = (int)dto.SelectedOptionId
                };
                isCorrect = await _quizRepository.checkAnswer(checkDto);
            }

            //  CẬP NHẬT CACHE
            if (isCorrect)
            {
                cache.NumberOfCorrectAnswer++;
            }
            else
            {
                cache.NumberOfWrongAnswer++;

                // Lấy ID đáp án đúng để lưu vào WrongAnswerDTO
                var correctOptionDTO = await _quizRepository.getCorrectAnswer(new GetCorrectAnswer
                {
                    QuizId = dto.QuizId,
                    QuestionId = dto.QuestionId
                });

                // Thêm vào danh sách câu trả lời sai
                cache.WrongAnswers.Add(new WrongAnswerDTO
                {
                    QuestionId = dto.QuestionId,
                    SelectedOptionId = dto.SelectedOptionId,
                    CorrectOptionId = correctOptionDTO?.OptionId
                });
            }

            // Đánh dấu câu hỏi đã trả lời
            cache.AnsweredQuestions.Add(dto.QuestionId);

            //  Lưu lại Cache (Reset TTL)
            await _redis.SetStringAsync(redisKey, JsonSerializer.Serialize(cache), TimeSpan.FromHours(2));

            return true;
        }


        // SUBMIT QUIZ (Lưu kết quả & Rank)
        public async Task<OfflineResultViewDTO?> SubmitOfflineQuiz(FinishOfflineQuizDTO dto)
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
                cache.EndTime = DateTime.UtcNow;
                cache.Duration = (int)(cache.EndTime!.Value - cache.StartTime).TotalSeconds;

                var qg = await _context.quizzGroups.FirstOrDefaultAsync(x => x.QGId == dto.QGId);
                if (qg == null)
                {
                    _logger.LogError("Quiz group does not exist for QGId: {QGId}.", dto.QGId);
                    return null;
                }

                var existing = await _context.offlineResults
                    .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.QGId == dto.QGId);

                // Tính toán Score (làm tròn về số nguyên)
                int score = (int)Math.Round((double)cache.NumberOfCorrectAnswer / cache.TotalQuestion * 100, 0);

                OfflineResultModel currentResult;

                if (existing != null && existing.CountAttempts >= qg.MaxAttempts)
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
                        currentResult = newResult;
                    }
                    else
                    {
                        // Xóa chi tiết lỗi cũ trước khi cập nhật kết quả và lưu cái mới
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

                    //  LƯU KẾT QUẢ VÀO DB
                    await _context.SaveChangesAsync();
                    int offResultId = currentResult.OffResultId;

                    //  LƯU CÁC CÂU TRẢ LỜI SAI MỚI TỪ CACHE
                    var wrongAnswerEntities = cache.WrongAnswers.Select(wa => new OfflineWrongAnswerModule
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
                    await _context.SaveChangesAsync(); // Lưu chi tiết câu trả lời sai

                    //  CẬP NHẬT RANK TRONG REDIS (Sử dụng Sorted Set ZSET)
                    var rankKey = $"quiz_group_rank:{dto.QGId}";
                    // Sử dụng điểm số làm score trong ZSET. Điểm càng cao, Rank càng nhỏ (đứng đầu)
                    await _redis.ZAddAsync(rankKey, dto.StudentId.ToString(), score);

                    // Lấy Rank hiện tại của học sinh sau khi submit
                    var rank = await _redis.ZRankAsync(rankKey, dto.StudentId.ToString(), descending: true);

                    if (rank.HasValue)
                    {
                        currentResult.RANK = (int)rank.Value + 1; // Rank là 0-based, nên +1
                        _context.offlineResults.Update(currentResult);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    //  Xóa Cache phiên làm bài
                    await _redis.DeleteKeyAsync(key);

                    //  Trả về DTO kết quả
                    return new OfflineResultViewDTO
                    {
                        QuizId = dto.QuizId,
                        QuizTitle = _context.quizzes.FirstOrDefault(q => q.QuizId == dto.QuizId)?.Title ?? "N/A",
                        CorrectCount = cache.NumberOfCorrectAnswer,
                        WrongCount = cache.NumberOfWrongAnswer,
                        TotalQuestion = cache.TotalQuestion,
                        CountAttempts = currentResult.CountAttempts,
                        MaxAttempts = qg.MaxAttempts,
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