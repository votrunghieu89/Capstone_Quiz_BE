using Capstone.Database;
using Capstone.DTOs.Reports.Student;
using Capstone.Repositories.Histories;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class StudentReportService : IStudentReportRepository
    {
        private readonly ILogger<StudentReportService> _logger;
        private readonly AppDbContext _context; 
        public StudentReportService(ILogger<StudentReportService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ViewDetailOfCompletedQuizDTO> DetailOfCompletedQuiz(int studentId, int quizId, DateTime createAt)
        {
            var result = await _context.offlineResults
                         .Where(r => r.StudentId == studentId
                             && r.QuizId == quizId
                             && EF.Functions.DateDiffDay(r.CreateAt, createAt) == 0)
                         .FirstOrDefaultAsync();

            if (result == null)
                throw new Exception("Không tìm thấy kết quả quiz này.");

            // Lấy thông tin quiz + giáo viên
            var quiz = await (from q in _context.quizzes
                              join a in _context.authModels on q.TeacherId equals a.AccountId
                              where q.QuizId == quizId
                              select new
                              {
                                  q.Title,
                                  q.TeacherId,
                                  CreatedBy = a.Email
                              }).FirstOrDefaultAsync();

            // Lấy toàn bộ câu hỏi và options
            var questions = await (from ques in _context.questions
                                   where ques.QuizId == quizId
                                   select new QuestionDetailDTO
                                   {
                                       QuestionId = ques.QuestionId,
                                       QuestionContent = ques.QuestionContent,
                                       Time = ques.Time,
                                       Answers = (from opt in _context.options
                                                  where opt.QuestionId == ques.QuestionId
                                                  select new OptionDetailDTO
                                                  {
                                                      OptionId = opt.OptionId,
                                                      OptionContent = opt.OptionContent,
                                                      IsCorrect = opt.IsCorrect,
                                                      IsSelected = false
                                                  }).ToList()
                                   }).ToListAsync();

            // Lấy danh sách câu sai
            var wrongAnswers = await _context.offlineWrongAnswers
                .Where(w => w.OffResultId == result.OffResultId)
                .ToListAsync();

            // Duyệt qua từng câu hỏi
            foreach (var q in questions)
            {
                var wrong = wrongAnswers.FirstOrDefault(w => w.QuestionId == q.QuestionId);

                if (wrong == null)
                {
                    // Học sinh làm đúng → đánh dấu đáp án đúng là selected
                    foreach (var opt in q.Answers)
                    {
                        if (opt.IsCorrect)
                            opt.IsSelected = true;
                    }
                }
                else
                {
                    // Học sinh làm sai → đánh dấu đáp án học sinh chọn là selected
                    foreach (var opt in q.Answers)
                    {
                        if (opt.OptionId == wrong.SelectedOptionId)
                            opt.IsSelected = true;
                    }
                }
            }

            // Tổng số câu hỏi và câu đúng
            int totalQuestions = questions.Count;
            int correctAnswers = totalQuestions - wrongAnswers.Count;
            int wrongCount = wrongAnswers.Count;

            return new ViewDetailOfCompletedQuizDTO
            {
                QuizTitle = quiz.Title,
                NumberOfCorrectAnswers = correctAnswers,
                NumberOfWrongAnswers = wrongCount,
                TotalQuestions = totalQuestions,
                FinalScore = (int)((correctAnswers / (double)totalQuestions) * 100),
                StartDate = result.StartDate,
                CompletedAt = result.CreateAt,
                CreatedBy = quiz.CreatedBy,
                QuestionDetails = questions
            };
        }


        public async Task<List<GetAllCompletedPrivateQuizzesDTO>> GetAllCompletedPrivateQuizzes(int studentId)
        {
            try
            {
                var result = await (from or in _context.offlineResults
                                    join q in _context.quizzes on or.QuizId equals q.QuizId
                                    join a in _context.authModels on q.TeacherId equals a.AccountId
                                    where or.StudentId == studentId && or.GroupId != null
                                    select new GetAllCompletedPrivateQuizzesDTO
                                    {
                                        QuizId = q.QuizId,
                                        GroupId = or.GroupId,
                                        QuizTitle = q.Title,
                                        AvatarURL = q.AvatarURL,
                                        CompletedAt = or.EndDate,
                                        createBy = a.Email
                                    }).ToListAsync();
                foreach (var item in result)
                {
                    item.TotalQuestions = await _context.questions.Where(q => q.QuizId == item.QuizId).CountAsync();
                    item.GroupName = await _context.groups.Where(g => g.GroupId == item.GroupId).Select(g => g.GroupName).FirstOrDefaultAsync() ?? string.Empty;
                }
                if (result == null)
                {
                    _logger.LogWarning("No completed private quizzes found for studentId: {studentId}", studentId);
                    return new List<GetAllCompletedPrivateQuizzesDTO>();
                }
                else { 
                    return result;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving completed private quizzes for studentId: {studentId}", studentId);
                return new List<GetAllCompletedPrivateQuizzesDTO>();
            }
        }

        public async  Task<List<GetAllCompletedPublicQuizzesDTO>> GetAllCompletedPublicQuizzes(int studentId)
        {
            try { 
                var result = await (from or in _context.offlineResults
                                    join q in _context.quizzes on or.QuizId equals q.QuizId
                                    join a in _context.authModels on q.TeacherId equals a.AccountId
                                    where or.StudentId == studentId && or.GroupId == null
                                    select new GetAllCompletedPublicQuizzesDTO
                                    {
                                        QuizId = q.QuizId,
                                        QuizTitle = q.Title,
                                        AvatarURL = q.AvatarURL,
                                        CompletedAt = or.EndDate,
                                        createBy = a.Email
                                    }).ToListAsync();
                foreach (var item in result) { 
                    item.TotalQuestions = await _context.questions.Where(q => q.QuizId == item.QuizId).CountAsync();
                }
                if(result == null) 
                {
                    _logger.LogWarning("No completed public quizzes found for studentId: {studentId}", studentId);
                    return new List<GetAllCompletedPublicQuizzesDTO>();
                }
                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving completed public quizzes for studentId: {studentId}", studentId);
                return new List<GetAllCompletedPublicQuizzesDTO>();
            }
        }
    }
}
