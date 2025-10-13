using Capstone.Database;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
using Microsoft.EntityFrameworkCore;


namespace Capstone.Services
{
    public class OnlineQuizService : IOnlineQuizRepository
    {
        private readonly ILogger<OnlineQuizService> _logger;
        private readonly AppDbContext _context;
        private readonly Redis _redis;
        private readonly string connectionString;

        public OnlineQuizService(AppDbContext context, Redis redis, IConfiguration configuration, ILogger<OnlineQuizService> logger)
        {
            _context = context;
            _redis = redis;
            connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }
        public async Task<bool> InsertOnlineReport(InsertOnlineReportDTO insertOnlineReportDTO)
        {
            try
            {
                // Bắt đầu Transaction để đảm bảo tính toàn vẹn (ACID) cho Report, Results, và Wrong Answers.
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. TẠO ONLINE REPORT
                        // Lấy tên Quiz để đặt làm ReportName
                        var ReportName = await _context.quizzes
                                                    .Where(q => q.QuizId == insertOnlineReportDTO.QuizId)
                                                    .Select(q => q.Title)
                                                    .FirstOrDefaultAsync();

                        var newRerport = new OnlineReportModel()
                        {
                            QuizId = insertOnlineReportDTO.QuizId,
                            TeacherId = insertOnlineReportDTO.TeacherId,
                            ReportName = ReportName ?? $"Online Report for Quiz {insertOnlineReportDTO.QuizId}", // Đặt tên mặc định nếu Title null
                            HighestScore = insertOnlineReportDTO.HighestScore,
                            LowestScore = insertOnlineReportDTO.LowestScore,
                            AverageScore = insertOnlineReportDTO.AverageScore,
                            TotalParticipants = insertOnlineReportDTO.TotalParticipants,
                            CreateAt = DateTime.Now
                        };

                        await _context.onlinereports.AddAsync(newRerport);
                        await _context.SaveChangesAsync(); // Lưu để lấy OnlineReportId
                        int onlineReportId = newRerport.OnlineReportId;

                        // 2. TẠO ONLINE RESULTS VÀ ONLINE WRONG ANSWERS
                        var resultsToInsert = new List<OnlineResultModel>();
                        var wrongAnswersToInsert = new List<OnlineWrongAnswerModel>();

                        // LẶP QUA TẤT CẢ KẾT QUẢ HỌC SINH TRONG DTO
                        foreach (var resultDTO in insertOnlineReportDTO.InsertOnlineResultDTO)
                        {
                            // Tạo OnlineResultModel cho từng học sinh
                            var newResult = new OnlineResultModel()
                            {
                                OnlineReportId = onlineReportId,
                                QuizId = insertOnlineReportDTO.QuizId, // Giữ lại QuizId để tối ưu truy vấn (như đã thảo luận)
                                StudentName = resultDTO.StudentName,
                                Score = resultDTO.Score,
                                CorrecCount = resultDTO.CorrectCount,
                                WrongCount = resultDTO.WrongCount,
                                TotalQuestion = resultDTO.TotalQuestions,
                                Rank = resultDTO.Rank,
                                CreateAt = DateTime.Now,
                            };

                            resultsToInsert.Add(newResult);

                            // Lưu OnlineResultModel vào DbContext (Chưa SaveChanges)
                            // Lưu ý: Thêm vào danh sách và sẽ gọi AddRangeAsync sau vòng lặp để tối ưu
                        }

                        await _context.onlineResults.AddRangeAsync(resultsToInsert);
                        await _context.SaveChangesAsync(); // LƯU TẤT CẢ RESULTS để lấy OnlResultId cho Wrong Answers

                        // Sau khi lưu Results, OnlResultId đã được tự động gán cho các đối tượng trong resultsToInsert
                        for (int i = 0; i < resultsToInsert.Count; i++)
                        {
                            var newResult = resultsToInsert[i];
                            var resultDTO = insertOnlineReportDTO.InsertOnlineResultDTO[i];

                            int onlineResultId = newResult.OnlResultId;

                            // LẶP QUA TẤT CẢ CÂU TRẢ LỜI SAI CỦA HỌC SINH NÀY
                            foreach (var wrongAnswer in resultDTO.wrongAnswerDTOs)
                            {
                                var newWrongAnswer = new OnlineWrongAnswerModel()
                                {
                                    OnlResultId = onlineResultId,
                                    QuestionId = wrongAnswer.QuestionId,
                                    SelectedOptionId = wrongAnswer.SelectedOptionId,
                                    CorrectOptionId = wrongAnswer.CorrectOptionId
                                };
                                wrongAnswersToInsert.Add(newWrongAnswer);
                            }
                        }

                        await _context.onlineWrongAnswers.AddRangeAsync(wrongAnswersToInsert);
                        await _context.SaveChangesAsync(); // Lưu tất cả Wrong Answers

                        // 3. COMMIT TRANSACTION
                        await transaction.CommitAsync();
                        return true;

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Transaction failed in InsertOnlineReport");
                        await transaction.RollbackAsync();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InsertOnlineReport");
                return false;
            }
        }
    }
}
