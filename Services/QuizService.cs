 using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Data.SqlClient;
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
        private readonly string _connectionString;
        public QuizService(AppDbContext context, ILogger<QuizService> logger, Redis redis, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _redis = redis;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<bool> CreateQuiz(QuizCreateDTo quiz)
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
                        Title = quiz.Title,
                        Description = quiz.Description,
                        IsPrivate = quiz.IsPrivate,
                        AvatarURL = quiz.AvatarURL,
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
                            IsDeleted = false,
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
                                IsCorrect = option.IsCorrect,
                                IsDeleted = false,
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

        public async Task<string> DeleteQuiz(int quizId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy quiz
                var quiz = await _context.quizzes
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);
                var oldAvatarURL = quiz?.AvatarURL;
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz not found for quizId: {QuizId}", quizId);
                    return null;
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
                return oldAvatarURL;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting quiz with Id={QuizId}", quizId);
                return null;
            }
        }

        public async Task<QuizUpdateDTO> UpdateQuiz(QuizUpdateDTO dto)
        {
            var quiz = await _context.quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuizId == dto.QuizId);

            if (quiz == null) throw new Exception("Quiz not found");

            // Update quiz info
            quiz.FolderId = dto.FolderId;
            quiz.Title = dto.Title;
            quiz.Description = dto.Description;
            quiz.IsPrivate = dto.IsPrivate;
            quiz.AvatarURL = dto.AvartarURL;
            quiz.UpdateAt = DateTime.Now;

            // Lấy danh sách questionId từ DTO
            var dtoQuestionIds = dto.Questions.Where(q => q.QuestionId > 0).Select(q => q.QuestionId).ToList();

            // Đánh dấu question cũ mà không còn trong DTO là IsDeleted = true
            foreach (var oldQ in quiz.Questions)
            {
                if (!dtoQuestionIds.Contains(oldQ.QuestionId))
                {
                    oldQ.IsDeleted = true;
                    oldQ.UpdateAt = DateTime.Now;
                    foreach (var opt in oldQ.Options)
                    {
                        opt.IsDeleted = true;
                    }
                }
            }

            // Duyệt từng câu hỏi trong DTO
            foreach (var qDto in dto.Questions)
            {
                QuestionModel question;

                if (qDto.QuestionId > 0) // Update câu hỏi cũ
                {
                    question = quiz.Questions.FirstOrDefault(x => x.QuestionId == qDto.QuestionId);
                    if (question != null)
                    {
                        question.QuestionContent = qDto.QuestionContent;
                        question.QuestionType = qDto.QuestionType;
                        question.Time = qDto.Time;
                        question.IsDeleted = false; // bật lại nếu trước đó bị xóa
                        question.UpdateAt = DateTime.Now;
                    }
                    else
                    {
                        continue; // tránh lỗi null
                    }
                }
                else // Thêm câu hỏi mới
                {
                    question = new QuestionModel
                    {
                        QuizId = quiz.QuizId,
                        QuestionContent = qDto.QuestionContent,
                        QuestionType = qDto.QuestionType,
                        Time = qDto.Time,
                        Options = new List<OptionModel>(),
                        IsDeleted = false
                    };
                    await _context.questions.AddAsync(question);
                    await _context.SaveChangesAsync(); // save để có QuestionId
                    quiz.Questions.Add(question);
                }

                // Lấy danh sách optionId từ DTO
                var dtoOptionIds = qDto.Options.Where(o => o.OptionId > 0).Select(o => o.OptionId).ToList();

                // Đánh dấu option cũ không còn trong DTO là IsDeleted = true
                foreach (var oldO in question.Options)
                {
                    if (!dtoOptionIds.Contains(oldO.OptionId))
                    {
                        oldO.IsDeleted = true;
                    }
                }

                // Duyệt option
                foreach (var oDto in qDto.Options)
                {
                    if (oDto.OptionId > 0) // Update option cũ
                    {
                        var option = question.Options.FirstOrDefault(o => o.OptionId == oDto.OptionId);
                        if (option != null)
                        {
                            option.OptionContent = oDto.OptionContent;
                            option.IsCorrect = oDto.IsCorrect;
                            option.IsDeleted = false; // bật lại nếu trước đó bị xóa
                        }
                    }
                    else // Thêm option mới
                    {
                        var option = new OptionModel
                        {
                            QuestionId = question.QuestionId,
                            OptionContent = oDto.OptionContent,
                            IsCorrect = oDto.IsCorrect,
                            IsDeleted = false
                        };
                        await _context.options.AddAsync(option);
                        question.Options.Add(option);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Trả DTO cập nhật lại (có Id mới)
            return new QuizUpdateDTO
            {
                QuizId = quiz.QuizId,
                FolderId = quiz.FolderId,
                Title = quiz.Title,
                Description = quiz.Description,
                IsPrivate = quiz.IsPrivate,
                AvartarURL = quiz.AvatarURL,
                Questions = quiz.Questions.Select(q => new QuestionUpdateDTO
                {
                    QuestionId = q.QuestionId,
                    QuestionContent = q.QuestionContent,
                    QuestionType = q.QuestionType,
                    Time = q.Time,
                    IsDeleted = q.IsDeleted,
                    Options = q.Options.Select(o => new OptionUpdateDTO
                    {
                        OptionId = o.OptionId,
                        OptionContent = o.OptionContent,
                        IsCorrect = o.IsCorrect,
                        IsDeleted = o.IsDeleted
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<List<getQuizQuestionWithoutAnswerDTO>> GetQuizQuestions(int quizId)
        {
            try
            {
                var questions = await _context.questions
                    .Where(q => q.QuizId == quizId && q.IsDeleted == false)
                    .Include(q => q.Options.Where(o => o.IsDeleted == false))
                    .ToListAsync();

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("No questions found for quizId: {QuizId}", quizId);
                    return new List<getQuizQuestionWithoutAnswerDTO>();
                }

                // Bản đầy đủ (có IsCorrect)
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

             
                var cacheWithoutAnswer = result.Select(q => new getQuizQuestionWithoutAnswerDTO
                {
                    QuestionId = q.QuestionId,
                    QuestionType = q.QuestionType,
                    QuestionContent = q.QuestionContent,
                    Time = q.Time,
                    Options = q.Options.Select(o => new getQuizOptionWithoutAnswerDTO
                    {
                        OptionId = o.OptionId,
                        OptionContent = o.OptionContent
                    }).ToList()
                }).ToList();

                
                await _redis.SetStringAsync(
                    $"quiz_questions_{quizId}",
                    JsonSerializer.Serialize(cacheWithoutAnswer),
                    TimeSpan.FromHours(2)
                );

            
                foreach (var q in result)
                {
                    RightAnswerDTO? optionModel = null;
                    foreach (var o in q.Options)
                    {
                        await _redis.SetStringAsync(
                            $"quiz_questions_{quizId}:question_{q.QuestionId}:option_{o.OptionId}",
                            o.IsCorrect.ToString().ToLower(),
                            TimeSpan.FromHours(2)
                        );

                        if (o.IsCorrect)
                        {
                            optionModel = new RightAnswerDTO
                            {
                                OptionId = o.OptionId,
                                OptionContent = o.OptionContent
                            };
                        }
                    }

                    if (optionModel != null)
                    {
                        await _redis.SetStringAsync(
                            $"quiz_questions_{quizId}:question_{q.QuestionId}:correctAnswer",
                            JsonSerializer.Serialize(optionModel),
                            TimeSpan.FromHours(2)
                        );
                    }
                }

                return cacheWithoutAnswer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz questions for quizId: {QuizId}", quizId);
                return new List<getQuizQuestionWithoutAnswerDTO>();
            }
        }

        public async Task<RightAnswerDTO> getCorrectAnswer(GetCorrectAnswer getCorrectAnswer) // quizId, questionId
        {
            var json = await _redis.GetStringAsync($"quiz_questions_{getCorrectAnswer.QuizId}:question_{getCorrectAnswer.QuestionId}:correcAnswer");
            if (json == null)
            {
                _logger.LogWarning("No cached correct answer found for quizId: {QuizId}, questionId: {QuestionId}", getCorrectAnswer.QuizId, getCorrectAnswer.QuestionId);
                var question = await _context.questions
                    .Where(q => q.QuizId == getCorrectAnswer.QuizId && q.QuestionId == getCorrectAnswer.QuestionId)
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync();
                if (question == null)
                {
                    _logger.LogWarning("Question not found for quizId: {QuizId}, questionId: {QuestionId}", getCorrectAnswer.QuizId, getCorrectAnswer.QuestionId);
                    return null;
                }
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect == true);
                if (correctOption == null)
                {
                    _logger.LogWarning("No correct option found for questionId: {QuestionId}", getCorrectAnswer.QuestionId);
                    return null;
                }
                RightAnswerDTO rightAnswerDTO = new RightAnswerDTO
                {
                    OptionId = correctOption.OptionId,
                    OptionContent = correctOption.OptionContent
                };
                await _redis.SetStringAsync($"quiz_questions_{getCorrectAnswer.QuizId}:question_{getCorrectAnswer.QuestionId}:correcAnswer", JsonSerializer.Serialize(rightAnswerDTO), TimeSpan.FromHours(2));
                return rightAnswerDTO;
            }
            var correctAnswer = JsonSerializer.Deserialize<RightAnswerDTO>(json);
            if (correctAnswer == null)
            {
                _logger.LogWarning("Deserialized correct answer is null for quizId: {QuizId}, questionId: {QuestionId}", getCorrectAnswer.QuizId, getCorrectAnswer.QuestionId);
                return null;
            }
            return new RightAnswerDTO
            {
                OptionId = correctAnswer.OptionId,
                OptionContent = correctAnswer.OptionContent
            };
        }
        public async Task<bool> checkAnswer(CheckAnswerDTO checkAnswerDTO)
        {
            try
            {
                var json = await _redis.GetStringAsync($"quiz_questions_{checkAnswerDTO.QuizId}:question_{checkAnswerDTO.QuestionId}:option_{checkAnswerDTO.OptionId}");
                if (json == null)
                {
                    _logger.LogWarning("No cached answer found for quizId: {QuizId}, questionId: {QuestionId}, optionId: {OptionId}", checkAnswerDTO.QuizId, checkAnswerDTO.QuestionId, checkAnswerDTO.OptionId);
                    var option = await _context.options
                         .Where(o => o.QuestionId == checkAnswerDTO.QuestionId && o.OptionId == checkAnswerDTO.OptionId)
                         .FirstOrDefaultAsync();
                    if (option == null)
                    {
                        _logger.LogWarning("Option not found for questionId: {QuestionId}, optionId: {OptionId}", checkAnswerDTO.QuestionId, checkAnswerDTO.OptionId);
                        return false;
                    }
                    await _redis.SetStringAsync($"quiz_questions_{checkAnswerDTO.QuizId}:question_{checkAnswerDTO.QuestionId}:option_{checkAnswerDTO.OptionId}", option.IsCorrect.ToString().ToLower(), TimeSpan.FromHours(2));
                    return option.IsCorrect;
                }
                else
                {
                    if (json.ToLower() == "true")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking answer for quizId: {QuizId}, questionId: {QuestionId}, optionId: {OptionId}", checkAnswerDTO.QuizId, checkAnswerDTO.QuestionId, checkAnswerDTO.OptionId);
                return false;
            }
        }
        public async Task<ViewDetailDTO> getDetailOfAQuiz(int quizId)
        {
            try
            {
                var quizDetail = await _context.quizzes
                    .Where(q => q.QuizId == quizId)
                    .Select(q => new
                    {
                        q.QuizId,
                        q.Title,
                        q.Description,
                        q.AvatarURL,
                        q.TotalParticipants,
                        q.CreateAt,
                    })
                    .FirstOrDefaultAsync();
                var totalQuestions = await _context.questions
                    .Where(q => q.QuizId == quizId && q.IsDeleted == false)
                    .CountAsync();

                if (quizDetail == null)
                {
                    _logger.LogWarning("No quiz found for quizId: {QuizId}", quizId);
                    return null;
                }

                // Lấy question chưa bị xóa
                List<QuestionDetailDTO> questionDetails = await _context.questions
                    .Where(q => q.QuizId == quizId && q.IsDeleted == false)
                    .Select(q => new QuestionDetailDTO
                    {
                        QuestionId = q.QuestionId,
                        QuestionType = q.QuestionType,
                        QuestionContent = q.QuestionContent,
                        Time = q.Time,
                        Options = q.Options
                            .Where(o => o.IsDeleted == false) // chỉ lấy option chưa xóa
                            .Select(o => new OptionDetailDTO
                            {
                                OptionId = o.OptionId,
                                OptionContent = o.OptionContent,
                                IsCorrect = o.IsCorrect
                            }).ToList()
                    })
                    .ToListAsync();

                var viewDetailDTO = new ViewDetailDTO
                {
                    QuizId = quizDetail.QuizId,
                    Title = quizDetail.Title,
                    Description = quizDetail.Description,
                    AvatarURL = quizDetail.AvatarURL,
                    TotalParticipants = quizDetail.TotalParticipants,
                    TotalQuestions = totalQuestions,
                    CreatedDate = quizDetail.CreateAt,
                    Questions = questionDetails ?? new List<QuestionDetailDTO>()
                };

                return viewDetailDTO;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detail of quiz for quizId: {QuizId}", quizId);
                return null;
            }
        }

        public async Task<string> getOrlAvatarURL(int quizId)
        {
            try
            {
                var quiz = await _context.quizzes
                    .Where(q => q.QuizId == quizId)
                    .Select(q => new { q.AvatarURL })
                    .FirstOrDefaultAsync();
                if (quiz == null)
                {
                    _logger.LogWarning("No quiz found for quizId: {QuizId}", quizId);
                    return null;
                }
                return quiz.AvatarURL;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting avatar URL for quizId: {QuizId}", quizId);
                return null;
            }
        }
        public async Task<List<ViewAllQuizDTO>> getAllQuizzes(int page, int pageSize)
        {
            var result = new List<ViewAllQuizDTO>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Tính số dòng bỏ qua
                    int skip = (page - 1) * pageSize;

                    string query = @"
                SELECT 
                    q.QuizId,
                    q.Title,
                    q.AvatarURL,
                    a.Email AS CreatedBy,
                    tpc.TopicName,
                    ISNULL(COUNT(ques.QuestionId), 0) AS TotalQuestions
                FROM Quizzes q
                LEFT JOIN TeacherProfile t
                    ON q.TeacherId = t.TeacherId
                LEFT JOIN Questions ques
                    ON q.QuizId = ques.QuizId AND ques.IsDeleted = 0
                LEFT JOIN Accounts a
                    ON q.TeacherId = a.AccountId
                JOIN Topics tpc
                    ON q.TopicId = tpc.TopicId
                WHERE q.IsPrivate = 0       
                GROUP BY q.QuizId, q.Title, q.AvatarURL, t.FullName, a.Email, tpc.TopicName
                ORDER BY q.QuizId
                OFFSET @Skip ROWS
                FETCH NEXT @Take ROWS ONLY;
            ";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Skip", skip);
                        cmd.Parameters.AddWithValue("@Take", pageSize);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var quiz = new ViewAllQuizDTO
                                {
                                    QuizId = reader.GetInt32(reader.GetOrdinal("QuizId")),
                                    Title = reader.GetString(reader.GetOrdinal("Title")),
                                    AvatarURL = reader.GetString(reader.GetOrdinal("AvatarURL")),
                                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                                                ? null
                                                : reader.GetString(reader.GetOrdinal("CreatedBy")),
                                    TopicName = reader.GetString(reader.GetOrdinal("TopicName")),
                                    TotalQuestions = reader.GetInt32(reader.GetOrdinal("TotalQuestions"))
                                };

                                result.Add(quiz);
                            }
                        }
                    }
                }
                await _redis.SetStringAsync($"all_quizzes_page_{page}_size_{pageSize}", JsonSerializer.Serialize(result), TimeSpan.FromHours(3));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all quizzes");
                return new List<ViewAllQuizDTO>();
            }

            return result;
        }
    }
}
