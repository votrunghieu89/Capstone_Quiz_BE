using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Capstone.Repositories.Quizzes;
using Capstone.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnlineQuizController : ControllerBase
    {
        private readonly IOnlineQuizRepository _onlineQuizRepository;
        private readonly ILogger<OnlineQuizController> _logger;
        private readonly IRedis _redis;
        private readonly IQuizRepository _quizRepository;
        private readonly IHubContext<QuizHub> _quizHub;
        public OnlineQuizController(IOnlineQuizRepository onlineQuizRepository, ILogger<OnlineQuizController> logger, IRedis redis, IQuizRepository quizRepository, IHubContext<QuizHub> quizHub)
        {
            _onlineQuizRepository = onlineQuizRepository;
            _logger = logger;
            _redis = redis;
            _quizRepository = quizRepository;
            _quizHub = quizHub;

        }

        [HttpPost("InsertOnlineReport")]
        public async Task<IActionResult> InsertOnlineReport(int roomCode)
        {
            try
            { 
                // Thông tin phòng
                var jsonRoomRedis = await _redis.GetStringAsync($"quiz:room:{roomCode}"); 
                var roomRedis = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(jsonRoomRedis ?? "");
                
                // lấy danh sách Id Học sinh
                List<string> SetStudentId = await _redis.SMembersAsync($"quiz:room:{roomCode}:student");
                List<InsertOnlineResultDTO> newResultDTO = new List<InsertOnlineResultDTO>();
                foreach(var member in SetStudentId)
                {
                    string[] parts = member.Split(':');
                    string studentId = parts[parts.Length - 1];
                    Console.WriteLine("Studnet" + studentId);
                }
                foreach (var member in SetStudentId) {
                    string[] parts = member.Split(':');
                    string studentId = parts[parts.Length - 1];

                    var jsonStudentInfor = await _redis.GetStringAsync($"quiz:room:{roomCode}:student:{studentId}");
                    var StudentInfor = JsonConvert.DeserializeObject<CreateStudentRedisDTO>(jsonStudentInfor ?? "");
                    Dictionary<string,string> StudentDetail = await _redis.HGetAllAsync($"quiz:room:{roomCode}:student:{studentId}:detail");
                    InsertOnlineResultDTO newResult = new InsertOnlineResultDTO()
                    {
                        StudentName = StudentInfor.StudentName,
                        Score = Convert.ToInt32(StudentDetail["Score"]),
                        CorrectCount = Convert.ToInt32(StudentDetail["CorrectCount"]),
                        WrongCount = Convert.ToInt32(StudentDetail["WrongCount"]),
                        TotalQuestions = StudentInfor.TotalQuestions,
                        Rank = Convert.ToInt32(StudentDetail["Rank"]),
                        wrongAnswerDTOs = StudentInfor.WrongAnswerRedisDTOs,
                    };
                    newResultDTO.Add(newResult);
                }
                InsertOnlineReportDTO newReport = new InsertOnlineReportDTO() { 
                    QuizId = roomRedis.QuizId,
                    TeacherId = roomRedis.TeacherId,
                    HighestScore = newResultDTO.Max(r => r.Score),
                    LowestScore = newResultDTO.Min(r => r.Score),
                    AverageScore = newResultDTO.Average(r => (decimal)r.Score),
                    TotalParticipants = newResultDTO.Count,
                    InsertOnlineResultDTO = newResultDTO
                };
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                bool isSuccess = await _onlineQuizRepository.InsertOnlineReport(newReport,accountId,ipAddess);
                if (isSuccess)
                {
                    return Ok(new { message = "Insert online report successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to insert online report" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting online report");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    

        [HttpPost("CheckOnlineAnswer")]
        public async Task<IActionResult> CheckOnlineAnswer([FromBody] OnlineAnswerDTO onlineAnswerDTO)
        {
            try
            {
                bool isCorrect = await _quizRepository.checkAnswer(new CheckAnswerDTO
                {
                    QuizId = onlineAnswerDTO.quizId,
                    QuestionId = onlineAnswerDTO.questionId,
                    OptionId = onlineAnswerDTO.optionId
                });

                string studentHashKey = $"quiz:room:{onlineAnswerDTO.roomCode}:student:{onlineAnswerDTO.studentId}:detail";
                string studentJsonKey = $"quiz:room:{onlineAnswerDTO.roomCode}:student:{onlineAnswerDTO.studentId}";

                if (isCorrect)
                {
                    // Tăng điểm + câu đúng + leaderboard
                    await Task.WhenAll(
                        _redis.HashIncrementAsync(studentHashKey, "Score", 10),
                        _redis.HashIncrementAsync(studentHashKey, "CorrectCount", 1),
                        _redis.ZIncrByAsync($"quiz:room:{onlineAnswerDTO.roomCode}:leaderboard", onlineAnswerDTO.studentId, 10)
                    );
                }
                else
                {
                    await _redis.HashIncrementAsync(studentHashKey, "WrongCount", 1);

                    var correctAnswer = await _quizRepository.getCorrectAnswer(new GetCorrectAnswer
                    {
                        QuizId = onlineAnswerDTO.quizId,
                        QuestionId = onlineAnswerDTO.questionId
                    });

                    string json = await _redis.GetStringAsync(studentJsonKey);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var studentData = JsonConvert.DeserializeObject<CreateStudentRedisDTO>(json);
                        studentData.WrongAnswerRedisDTOs.Add(new InsertWrongAnswerDTO // cehckj
                        {
                            QuestionId = onlineAnswerDTO.questionId,
                            SelectedOptionId = onlineAnswerDTO.optionId,
                            CorrectOptionId = correctAnswer.OptionId
                        });
                        await _redis.SetStringAsync(studentJsonKey, JsonConvert.SerializeObject(studentData), TimeSpan.FromHours(3));
                    }
                }
                // Gọi hàm update trả về leaderboard ở đây
                string roomJson = await _redis.GetStringAsync($"quiz:room:{onlineAnswerDTO.roomCode}");
                if (!string.IsNullOrEmpty(roomJson))
                {
                    var roomData = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(roomJson);
                    if (roomData != null && !string.IsNullOrEmpty(roomData.TeacherConnectionId))
                    {
                        bool isUpdateLeaderBoard = await _onlineQuizRepository.updateLeaderBoard(onlineAnswerDTO.roomCode);
                    }
                }
                return Ok(new { isCorrect });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking online answer");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
