using Capstone.Database;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Capstone.Model;
using Capstone.RabbitMQ;
using Capstone.Repositories.Quizzes;
using Capstone.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Capstone.Services
{
    public class OnlineQuizService : IOnlineQuizRepository
    {
        private readonly ILogger<OnlineQuizService> _logger;
        private readonly AppDbContext _context;
        private readonly Redis _redis;
        private readonly IRabbitMQProducer _rabbitMQ;
        private readonly string connectionString;
        private readonly IHubContext<QuizHub> _quizHub;

        public OnlineQuizService(AppDbContext context, Redis redis,
            IConfiguration configuration, ILogger<OnlineQuizService> logger, IRabbitMQProducer rabbitMQ,
            IHubContext<QuizHub> quizHub)
        {
            _context = context;
            _redis = redis;
            connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
            _rabbitMQ = rabbitMQ;
            _quizHub = quizHub;

        }


        public async Task<bool> InsertOnlineReport(InsertOnlineReportDTO insertOnlineReportDTO, int accountId, string ipAddress)
        {
            _logger.LogInformation("InsertOnlineReport: Start - QuizId={QuizId}, AccountId={AccountId}", insertOnlineReportDTO.QuizId, accountId);
            try
            {

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. TẠO ONLINE REPORT
                        var ReportName = await _context.quizzes
                                                     .Where(q => q.QuizId == insertOnlineReportDTO.QuizId)
                                                     .Select(q => q.Title)
                                                     .FirstOrDefaultAsync();

                        var newRerport = new OnlineReportModel()
                        {
                            QuizId = insertOnlineReportDTO.QuizId,
                            TeacherId = insertOnlineReportDTO.TeacherId,
                            ReportName = ReportName ?? $"Online Report for Quiz {insertOnlineReportDTO.QuizId}",
                            HighestScore = insertOnlineReportDTO.HighestScore,
                            LowestScore = insertOnlineReportDTO.LowestScore,
                            AverageScore = insertOnlineReportDTO.AverageScore,
                            TotalParticipants = insertOnlineReportDTO.TotalParticipants,
                            CreateAt = DateTime.Now
                        };

                        await _context.onlinereports.AddAsync(newRerport);
                        await _context.SaveChangesAsync();
                        int onlineReportId = newRerport.OnlineReportId;

                        // 2. TẠO ONLINE RESULTS VÀ ONLINE WRONG ANSWERS

                        var resultsToInsert = new List<OnlineResultModel>();
                        var wrongAnswersToInsert = new List<OnlineWrongAnswerModel>();


                        foreach (var resultDTO in insertOnlineReportDTO.InsertOnlineResultDTO)
                        {
                            // Tạo OnlineResultModel cho từng học sinh
                            var newResult = new OnlineResultModel()
                            {
                                OnlineReportId = onlineReportId,
                                QuizId = insertOnlineReportDTO.QuizId,
                                StudentName = resultDTO.StudentName,
                                Score = resultDTO.Score,
                                CorrecCount = resultDTO.CorrectCount,
                                WrongCount = resultDTO.WrongCount,
                                TotalQuestion = resultDTO.TotalQuestions,
                                Rank = resultDTO.Rank,
                                CreateAt = DateTime.Now,
                            };

                            resultsToInsert.Add(newResult);

                        }

                        await _context.onlineResults.AddRangeAsync(resultsToInsert);
                        await _context.SaveChangesAsync();

                        int isUpdate = await _context.quizzes.Where(q => q.QuizId == insertOnlineReportDTO.QuizId).ExecuteUpdateAsync(e => e.SetProperty(q => q.TotalParticipants, q => q.TotalParticipants + insertOnlineReportDTO.TotalParticipants));

                        for (int i = 0; i < resultsToInsert.Count; i++)
                        {
                            var newResult = resultsToInsert[i];
                            var resultDTO = insertOnlineReportDTO.InsertOnlineResultDTO[i];

                            int onlineResultId = newResult.OnlResultId;


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
                        await _context.SaveChangesAsync();

                        // 3. COMMIT TRANSACTION
                        await transaction.CommitAsync();

                        // Thêm Audit Log (RabbitMQ) sau khi Commit thành công
                        var log = new AuditLogModel()
                        {
                            AccountId = accountId,
                            Action = "Thêm báo cáo bài kiểm tra trực tuyến",
                            Description = $"Báo cáo trực tuyến có ID:{onlineReportId} cho Bài kiểm tra có ID:{insertOnlineReportDTO.QuizId} được tạo bởi Tài khoản có ID:{accountId}",
                            CreatAt = DateTime.Now,
                            IpAddress = ipAddress
                        };
                        await _rabbitMQ.SendMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(log));

                        _logger.LogInformation("InsertOnlineReport: Success - OnlineReportId={OnlineReportId}", onlineReportId);
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

        public async Task<bool> updateLeaderBoard(string roomCode)
        {
            string leaderboardKey = $"quiz:room:{roomCode}:leaderboard";
            string roomJson = await _redis.GetStringAsync($"quiz:room:{roomCode}");
            if (string.IsNullOrEmpty(roomJson)) return false;

            var roomData = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(roomJson);
            if (roomData == null || string.IsNullOrEmpty(roomData.TeacherConnectionId)) return false;

            var teacherConnectionId = roomData.TeacherConnectionId;
            // Lấy toàn bộ studentId theo điểm giảm dần
            var studentsWithScores = await _redis.ZRevRangeWithScoresAsync(leaderboardKey, 0, -1);
            var leaderboard = new List<LeaderboardDTO>();
            int rank = 1;
            foreach (var (studentId, score) in studentsWithScores)
            {
                string studentKey = $"quiz:room:{roomCode}:student:{studentId}";
                var studentJson = await _redis.GetStringAsync(studentKey);
                var studentData = JsonConvert.DeserializeObject<CreateStudentRedisDTO>(studentJson);
                leaderboard.Add(new LeaderboardDTO
                {
                    StudentId = studentId,
                    StudentName = studentData?.StudentName ?? "Unknown",
                    Score = (int)score,
                    Rank = rank
                });
                rank++;
            }
            foreach (var ld in leaderboard)
            {
                Console.WriteLine($"ID:{ld.StudentId} || Name: {ld.StudentName} || Score: {ld.Score} || Rank: {ld.Rank}");
            }
            await _quizHub.Clients.Client(roomData.TeacherConnectionId)
                            .SendAsync("ReceiveLeaderboard", leaderboard);
            return true;
        }
        //public async Task<bool> UpdateNumberOfParticipants(int quizId, int totalParticipant)
        //{
        //    try
        //    {
        //        int isUpdate = await _context.quizzes.Where(q => q.QuizId == quizId).ExecuteUpdateAsync(e => e.SetProperty(q => q.TotalParticipants, q => q.TotalParticipants + totalParticipant));
        //        if (isUpdate > 0) { 
        //            return true;
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}