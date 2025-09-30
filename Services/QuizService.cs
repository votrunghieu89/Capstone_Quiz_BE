using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Capstone.Services
{
    public class QuizService : IQuizRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QuizService> _logger;
        private readonly Redis _redis;

        public QuizService(AppDbContext context, ILogger<QuizService> logger, Redis redis)
        {
            _context = context;
            _logger = logger;
            _redis = redis;
        }


        public async Task<bool> CreateQuiz(QuizModel quiz)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var newQuiz = new QuizModel
                    {
                        TeacherId = quiz.TeacherId,
                        FolderId = quiz.FolderId,
                        TopicId = quiz.TopicId,
                        GroupId = quiz.GroupId,
                        Title = quiz.Title,
                        Description = quiz.Description,
                        IsPrivate = quiz.IsPrivate,
                        AvartarURL = quiz.AvartarURL,
                        NumberOfPlays = 0,
                        CreateAt = DateTime.Now
                    };

                    await _context.quizzes.AddAsync(newQuiz);
                    await _context.SaveChangesAsync();

                    var questions = new List<QuestionModel>();
                    foreach (var question in quiz.Questions)
                    {
                        var newQuestion = new QuestionModel
                        {
                            QuizId = newQuiz.QuizId,
                            QuestionType = question.QuestionType,
                            QuestionContent = question.QuestionContent,
                            Time = question.Time,
                            CreateAt = DateTime.Now
                        };
                        questions.Add(newQuestion);
                    }
                    await _context.questions.AddRangeAsync(questions);
                    await _context.SaveChangesAsync(); // cần để EF tạo QuestionId

                    var optionsToAdd = new List<OptionModel>();
                    for (int i = 0; i < questions.Count; i++)
                    {
                        var originalQuestion = quiz.Questions[i];
                        var questionId = questions[i].QuestionId;
                        foreach (var option in originalQuestion.Options)
                        {
                            var newOption = new OptionModel
                            {
                                QuestionId = questionId,
                                OptionContent = option.OptionContent,
                                IsCorrect = option.IsCorrect
                            };
                            optionsToAdd.Add(newOption);
                        }
                    }
                    await _context.options.AddRangeAsync(optionsToAdd);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return true;

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed, rolled back.");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz");
                return false;
            }
        }

        public async Task<bool> DeleteQuestion(int questionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tìm question theo Id
                var question = await _context.questions
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    _logger.LogWarning("DeleteQuestion: Question not found - QuestionId={QuestionId}", questionId);
                    return false;
                }

                // 2. Lấy tất cả options của question
                var options = await _context.options
                    .Where(o => o.QuestionId == questionId)
                    .ToListAsync();

                if (options.Any())
                {
                    _context.options.RemoveRange(options);
                }
                _context.questions.Remove(question);

                await _context.SaveChangesAsync();

                // 5. Commit transaction
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting question - QuestionId={QuestionId}", questionId);
                return false;
            }
        }

        public async Task<bool> DeleteQuiz(int quizId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy quiz
                var quiz = await _context.quizzes
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    _logger.LogWarning("Quiz not found for quizId: {QuizId}", quizId);
                    return false;
                }

                // 2. Lấy tất cả questions thuộc quiz
                var questions = await _context.questions
                    .Where(q => q.QuizId == quizId)
                    .ToListAsync();

                if (questions.Any())
                {
                    // 3. Lấy tất cả options của các questions
                    var questionIds = questions.Select(q => q.QuestionId).ToList();
                    var options = await _context.options
                        .Where(o => questionIds.Contains(o.QuestionId))
                        .ToListAsync();

                    if (options.Any())
                    {
                        _context.options.RemoveRange(options);
                        await _context.SaveChangesAsync();
                    }

                    _context.questions.RemoveRange(questions);
                    await _context.SaveChangesAsync();
                }

                // 4. Xóa quiz
                _context.quizzes.Remove(quiz);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting quiz with Id={QuizId}", quizId);
                return false;
            }
        }

        public async Task<bool> UpdateQuiz(QuizModel quiz)
        {
           return false; // Chưa làm
        }
        public async Task<List<GetQuizQuestionsDTO>> GetQuizQuestions(int quizId)
        {
            try
            {
                var questions = await _context.questions
                    .Where(q => q.QuizId == quizId)
                    .Include(q => q.Options)
                    .ToListAsync();

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("No questions found for quizId: {QuizId}", quizId);
                    return new List<GetQuizQuestionsDTO>();
                }

                var result = questions.Select(q => new GetQuizQuestionsDTO
                {
                    QuestionId = q.QuestionId,
                    QuestionType = q.QuestionType,
                    QuestionContent = q.QuestionContent,
                    Time = q.Time,
                    Options = q.Options.Select(o => new GetOptionDTO
                    {
                        OptionId = o.OptionId,
                        OptionContent = o.OptionContent,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList();
                var totalTime = result.Sum(q => q.Time);
                await _redis.SetStringAsync($"quiz_questions_{quizId}", JsonSerializer.Serialize(result), TimeSpan.FromSeconds(totalTime + 600));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz questions for quizId: {QuizId}", quizId);
                return new List<GetQuizQuestionsDTO>();
            }
        }

        public async Task<RightAnswerDTO> getCorrectAnswer(GetCorrectAnswer getCorrectAnswer)
        {
            var json = await _redis.GetStringAsync($"quiz_questions_{getCorrectAnswer.QuizId}");
            if (json == null)
            {
                _logger.LogWarning("No cached questions found for quizId: {QuizId}", getCorrectAnswer.QuizId);
                return null;
            }
            var questions = JsonSerializer.Deserialize<List<GetQuizQuestionsDTO>>(json);
            var question = questions?.FirstOrDefault(q => q.QuestionId == getCorrectAnswer.QuestionId);
            if (question == null)
            {
                _logger.LogWarning("Question not found for QuestionId: {QuestionId} in QuizId: {QuizId}", getCorrectAnswer.QuestionId, getCorrectAnswer.QuizId);
                return null;
            }
            var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect ==  true);
            return new RightAnswerDTO
            {
               OptionId = correctOption.OptionId,
               OptionContent = correctOption.OptionContent
            };  
        }

        public async Task<bool> checkAnswer(CheckAnswerDTO checkAnswerDTO)
        {
            var json = await _redis.GetStringAsync($"quiz_questions_{checkAnswerDTO.QuizId}");
            if (json == null)
            {
                _logger.LogWarning("No cached questions found for quizId: {QuizId}", checkAnswerDTO.QuizId);
                return false;
            }
            var questions = JsonSerializer.Deserialize<List<GetQuizQuestionsDTO>>(json);
            if (questions == null || !questions.Any())
            {
                _logger.LogWarning("Deserialized questions are null or empty for quizId: {QuizId}", checkAnswerDTO.QuizId);
                return false;
            }
            var question = questions?.FirstOrDefault(q => q.QuestionId == checkAnswerDTO.QuestionId);
            var option = question?.Options.FirstOrDefault(o => o.OptionId == checkAnswerDTO.OptionId);
            if(option == null)
            {
                _logger.LogWarning("Option not found for OptionId: {OptionId} in QuestionId: {QuestionId}", checkAnswerDTO.OptionId, checkAnswerDTO.QuestionId);
                return false;
            }
            return option.IsCorrect;

        }
    }
}
