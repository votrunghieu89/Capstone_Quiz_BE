using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Capstone.SignalR
{
    [Authorize]
    public class QuizHub : Hub
    {
        // list ở dây là tên giáo viên và học sinh
        // String là mã Pin
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Rooms = new();
        private static ConcurrentDictionary<string, (string RoomCode, string StudentId)> StudentConnections = new();
        private readonly IRedis _redis;
        private readonly ILogger<QuizHub> _logger;

        public QuizHub(IRedis redis, ILogger<QuizHub> logger)
        {
            _redis = redis;
            _logger = logger;
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Kiểm tra xem connection này có ánh xạ tới StudentId không
            if (StudentConnections.TryRemove(Context.ConnectionId, out var info))
            {
                var (roomCode, studentId) = info;

                // Kiểm tra phòng có tồn tại
                if (Rooms.TryGetValue(roomCode, out var studentsInRoom))
                {
                    // Xoá student khỏi phòng
                    if (studentsInRoom.TryRemove(studentId, out var studentName))
                    {
                        _logger.LogInformation("Student {StudentName} ({StudentId}) disconnected from room {RoomCode}",
                            studentName, studentId, roomCode);

                        // Gửi update danh sách student còn lại cho toàn bộ phòng
                        await Clients.Group(roomCode).SendAsync("UpdateStudentList", studentsInRoom.Values, studentsInRoom.Count);
                    }

                    // Nếu phòng trống, bạn có thể xóa room luôn
                    if (studentsInRoom.IsEmpty)
                    {
                        Rooms.TryRemove(roomCode, out _);
                        _logger.LogInformation("Room {RoomCode} is empty and removed.", roomCode);
                    }
                }

                // Xoá dữ liệu Redis nếu cần
                await _redis.DeleteKeysByPatternAsync($"quiz:room:{roomCode}:student:{studentId}*");
            }

            await base.OnDisconnectedAsync(exception);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<string> CreateRoom(int quizId, int teacherId, int totalQuestion)
        {
            string roomCode;
            do
            {
                roomCode = new Random().Next(100000, 999999).ToString();
            } while (await _redis.KeyExistsAsync($"quiz_room_{roomCode}"));

            Rooms[roomCode] = new ConcurrentDictionary<string, string>();
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode); 
            CreateRoomRedisDTO createRoomRedis = new CreateRoomRedisDTO()
            {
                QuizId = quizId,
                TeacherId = teacherId,
                TeacherConnectionId = Context.ConnectionId,
                TotalStudents = 0,
                TotalQuestion = totalQuestion,
                StartDate = DateTime.Now
            };
            string jsonData = JsonConvert.SerializeObject(createRoomRedis);
            await _redis.SetStringAsync($"quiz:room:{roomCode}",jsonData, TimeSpan.FromHours(3));
            return roomCode;
        }
        [Authorize(Roles = "Student")]
        public async Task<string> JoinRoom(string roomCode, string studentName, int totalQuestion)
        {
            // Kiểm tra phòng tồn tại trong bộ nhớ cục bộ (Rooms)
            if (!Rooms.ContainsKey(roomCode))
            {
                _logger.LogInformation("JoinRoom failed: Room {roomCode} does not exist.", roomCode);
                return null;
            }
            string studentId = Guid.NewGuid().ToString("N");
            Rooms[roomCode][studentId] = studentName;
            StudentConnections[Context.ConnectionId] = (roomCode, studentId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            //
            int TotalStudents = Rooms[roomCode].Count;
            await Clients.Group(roomCode).SendAsync("UpdateStudentList", Rooms[roomCode].Values, TotalStudents);

            CreateStudentRedisDTO newStudentRedis = new CreateStudentRedisDTO()
            {
                StudentName = studentName,
                TotalQuestions = totalQuestion,
                WrongAnswerRedisDTOs = new List<InsertWrongAnswerDTO>()
            };
            string jsonData = JsonConvert.SerializeObject(newStudentRedis);
            string redisKey = $"quiz:room:{roomCode}:student:{studentId}";
            await _redis.SAddAsync($"quiz:room:{roomCode}:student", redisKey, TimeSpan.FromHours(3));
            await _redis.SetStringAsync(redisKey, jsonData, TimeSpan.FromHours(3));


            await _redis.HSetAsync($"quiz:room:{roomCode}:student:{studentId}:detail", new Dictionary<string, string>
            {
                ["Score"] = "0",
                ["CorrectCount"] = "0",
                ["WrongCount"] = "0",
                ["Rank"] = "0"
            }, TimeSpan.FromHours(3));
            await _redis.ZAddAsync($"quiz:room:{roomCode}:leaderboard", studentId, 0, TimeSpan.FromHours(3));
            _logger.LogInformation("Student {StudentName} joined room {RoomCode} with RedisKey {RedisKey}", studentName, roomCode, redisKey);
            return JsonConvert.SerializeObject(new
            {
                studentId,
                totalStudents = TotalStudents,
                roomCode
            });
        }
        [Authorize(Roles = "Teacher")]
        public async Task StartGame(string roomCode)
        {
            if (Rooms.ContainsKey(roomCode))
            {
                await Clients.Group(roomCode).SendAsync("GameStarted");
            }
            string roomJson = await _redis.GetStringAsync($"quiz:room:{roomCode}");
            int totalStudents = Rooms[roomCode].Count;
            if (!string.IsNullOrEmpty(roomJson))
            {
                var roomData = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(roomJson);
                CreateRoomRedisDTO createRoomRedis = new CreateRoomRedisDTO()
                {
                    QuizId = roomData.QuizId,
                    TeacherId = roomData.TeacherId,
                    TeacherConnectionId = roomData.TeacherConnectionId,
                    TotalStudents = totalStudents,
                    TotalQuestion = roomData.TotalQuestion,
                    StartDate = DateTime.Now
                };
                await _redis.SetStringAsync($"quiz:room:{roomCode}", JsonConvert.SerializeObject(createRoomRedis), TimeSpan.FromHours(3));
                var teacherConnectionId = roomData?.TeacherConnectionId;

                if (!string.IsNullOrEmpty(teacherConnectionId))
                    await Clients.Client(teacherConnectionId).SendAsync("GameStarted");
            }
        }
        [Authorize(Roles = "Teacher")]
        public async Task EndBeforeStartGameHandler(string roomCode)
        {
            _logger.LogInformation("Ending game for room {RoomCode}", roomCode);

            if (!Rooms.ContainsKey(roomCode))
            {
                _logger.LogWarning("EndGame failed: Room {RoomCode} not found in memory", roomCode);
                return;
            }

            try
            {
                _logger.LogInformation("Ending game for room {RoomCode}", roomCode);
                await Clients.Group(roomCode).SendAsync("EndBeforeStartGame", $"Room {roomCode} has ended.");
                await _redis.DeleteKeysByPatternAsync($"quiz:room:{roomCode}*");
                Rooms.TryRemove(roomCode, out _);
                _logger.LogInformation("All Redis data for room {RoomCode} deleted.", roomCode);
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ending game for room {RoomCode}", roomCode);
            }
        }
        [Authorize(Roles = "Teacher")]
        // Đặt hàm này trong QuizHub
        public async Task EndAfterComplete(string roomCode)
        {
            _logger.LogInformation("Processing end of game and saving results for room {RoomCode}", roomCode);

            if (!Rooms.ContainsKey(roomCode))
            {
                _logger.LogWarning("EndAfterComplete failed: Room {RoomCode} not found in memory", roomCode);
                return;
            }

            try
            {
                string leaderboardKey = $"quiz:room:{roomCode}:leaderboard";

                // 1. Lấy toàn bộ studentId và điểm (score) theo điểm giảm dần từ Sorted Set
                var studentsWithScores = await _redis.ZRevRangeWithScoresAsync(leaderboardKey, 0, -1);

                int rank = 1;

                // 2. LẶP QUA TỪNG HỌC SINH ĐỂ CẬP NHẬT RANK VÀ GỬI KẾT QUẢ CUỐI CÙNG
                foreach (var (studentId, score) in studentsWithScores)
                {
                    // Cập nhật Rank vào Hash Set (:detail)
                    string detailKey = $"quiz:room:{roomCode}:student:{studentId}:detail";

                    // Cập nhật trường "Rank" trong Hash Set của học sinh
                    await _redis.HSetAsync(detailKey, "Rank", rank.ToString(), TimeSpan.FromHours(3));
                    rank++;
                }
                _logger.LogInformation("Successfully updated ranks for all participants in room {RoomCode}.", roomCode);
                await Clients.Group(roomCode).SendAsync("GameEnded");
                _logger.LogInformation("All Redis data and in-memory room {RoomCode} deleted.", roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ending game after completion for room {RoomCode}", roomCode);
            }
        }
        [Authorize(Roles = "Teacher")]
        public async Task EndClick(string roomCode)
        {
            try
            {
                await _redis.DeleteKeysByPatternAsync($"quiz:room:{roomCode}*");
                Rooms.TryRemove(roomCode, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ending game after completion for room {RoomCode}", roomCode);
            }
        }
        [Authorize(Roles = "Student")]
        public async Task StudentComplete(string roomCode, string studentId) // result
        {
            string leaderboardKey = $"quiz:room:{roomCode}:leaderboard";

            // 1. Lấy thứ hạng của học sinh đang nộp bài
            // Redis ZRevRank trả về thứ hạng index bắt đầu từ 0
            long? rankIndex = await _redis.ZRankAsync(leaderboardKey, studentId);
            int rank = rankIndex.HasValue ? (int)rankIndex.Value + 1 : 0;

            // 2. Cập nhật Rank của học sinh trong Hash
            string detailKey = $"quiz:room:{roomCode}:student:{studentId}:detail";
            await _redis.HSetAsync(detailKey, "Rank", rank.ToString(), TimeSpan.FromHours(3));
            // 1. Lấy thông tin phòng
            var jsonRoomRedis = await _redis.GetStringAsync($"quiz:room:{roomCode}");
            if (string.IsNullOrEmpty(jsonRoomRedis)) return ;
            var roomRedis = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(jsonRoomRedis);
            if (roomRedis == null) return ;

            // 2. Lấy danh sách câu hỏi, đáp án
            var jsonlistQuestion = await _redis.GetStringAsync($"quiz_questions_{roomRedis.QuizId}_Answer");
            if (string.IsNullOrEmpty(jsonlistQuestion)) return ;

            var listQuestion = JsonConvert.DeserializeObject<List<GetQuizQuestionsDTO>>(jsonlistQuestion);

            // 3. Lấy thông tin học sinh và kết quả cuối
            // Thông tin
            var jsonStudentInfor = await _redis.GetStringAsync($"quiz:room:{roomCode}:student:{studentId}");
            var studentData = JsonConvert.DeserializeObject<CreateStudentRedisDTO>(jsonStudentInfor ?? "");
            // Kết quả
            Dictionary<string, string> StudentDetail = await _redis.HGetAllAsync($"quiz:room:{roomCode}:student:{studentId}:detail");


            var wrongAnswersDict = studentData.WrongAnswerRedisDTOs.ToDictionary(w => w.QuestionId, w => w);
            var resultQuestions = new List<QuestionResultDTO>();
            foreach (var q in listQuestion)
            {
                // Kiểm tra học sinh có sai ở câu này không
                wrongAnswersDict.TryGetValue(q.QuestionId, out var wrong);
                var optionResults = q.Options.Select(o => new OptionResultDTO
                {
                    OptionId = o.OptionId,
                    OptionContent = o.OptionContent,
                    IsCorrect = o.IsCorrect,
                    IsSelectedWrong = wrong != null && wrong.SelectedOptionId == o.OptionId 
                }).ToList();

                resultQuestions.Add(new QuestionResultDTO
                {
                    QuestionId = q.QuestionId,
                    QuestionContent = q.QuestionContent,
                    Options = optionResults
                });
            }

            StudentCompleteResultDTO studentCompleteResultDTO = new StudentCompleteResultDTO()
            {
                StudentName = studentData.StudentName,
                Score = Convert.ToInt32(StudentDetail["Score"]),
                CorrectCount = Convert.ToInt32(StudentDetail["CorrectCount"]),
                WrongCount = Convert.ToInt32(StudentDetail["WrongCount"]),
                TotalQuestions = studentData.TotalQuestions,
                Rank = Convert.ToInt32(StudentDetail["Rank"]),
                Questions = resultQuestions
            };
            var connectionEntry = StudentConnections.FirstOrDefault(x => x.Value.StudentId == studentId);

            if (connectionEntry.Key != null)
            {
                var Connection = connectionEntry.Key;
                await Clients.Client(Connection).SendAsync("CompleteQuiz", studentCompleteResultDTO);
            }
        }
    }
}
