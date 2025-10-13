using Capstone.DTOs.Quizzes.QuizzOnline;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Capstone.SignalR
{
    public class QuizHub : Hub
    {
        // list ở dây là tên giáo viên và học sinh
        // String là mã Pin
        private static ConcurrentDictionary<string, ConcurrentBag<string>> Rooms = new();
        private readonly Redis _redis;
        private readonly ILogger<QuizHub> _logger;

        public QuizHub(Redis redis, ILogger<QuizHub> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<string> CreateRoom(int quizId, int teacherId, int totalQuestion)
        {
            string roomCode;
            do
            {
                roomCode = new Random().Next(100000, 999999).ToString();
            } while (await _redis.KeyExistsAsync($"quiz_room_{roomCode}"));

            Rooms[roomCode] = new ConcurrentBag<string>(); // Tạo phòng mới với mã Pin
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode); // Thêm người dùng vào 1 nhóm
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

        public async Task<string> JoinRoom(string roomCode, string studentName, int totalQuestion)
        {
            // Kiểm tra phòng tồn tại trong bộ nhớ cục bộ (Rooms)
            if (!Rooms.ContainsKey(roomCode))
            {
                _logger.LogInformation("JoinRoom failed: Room {roomCode} does not exist.", roomCode);
                return null;
            }

          
            Rooms[roomCode].Add(studentName);
            int TotalStudents = Rooms[roomCode].Count;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("UpdateStudentList", Rooms[roomCode], TotalStudents);

            string studentId = Guid.NewGuid().ToString("N");

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

        public async Task UpdateLeaderBoardHandle(string roomCode)
        {
            Console.WriteLine($"Updating leaderboard for room {roomCode}");
            string leaderboardKey = $"quiz:room:{roomCode}:leaderboard";
            string roomJson = await _redis.GetStringAsync($"quiz:room:{roomCode}");
            if (string.IsNullOrEmpty(roomJson)) return;

            var roomData = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(roomJson);
            if (roomData == null || string.IsNullOrEmpty(roomData.TeacherConnectionId)) return;

            var teacherConnectionId = roomData.TeacherConnectionId;
            // Lấy toàn bộ studentId theo điểm giảm dần
            var studentsWithScores = await _redis.ZRevRangeWithScoresAsync(leaderboardKey, 0, -1);
            var leaderboard = new List<LeaderboardDTO>();
            int rank = 1;
            foreach (var(studentId, score) in studentsWithScores)
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
            foreach(var entry in leaderboard)
            {
                Console.WriteLine($"StudentId: {entry.StudentId}, Name: {entry.StudentName}, Score: {entry.Score}, Rank: {entry.Rank}");
            }
            await Clients.Client(teacherConnectionId).SendAsync("ReceiveLeaderboard", leaderboard);
        }
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
    }
}
