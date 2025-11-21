using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Quizzes.QuizzOnline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Capstone.SignalR
{
    public class QuizHub : Hub
    {
        // list ở dây là tên giáo viên và học sinh
        // String là mã Pin
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Rooms = new(); // key là roomCode value là {StudentId; Name Student}
        private static ConcurrentDictionary<string, (string RoomCode, string StudentId)> StudentConnections = new(); // key là connectionID value là {roomcoed; studentId}
        private readonly IRedis _redis;
        private readonly ILogger<QuizHub> _logger;
        private readonly AppDbContext _dbContext;

        public QuizHub(IRedis redis, ILogger<QuizHub> logger, AppDbContext dbContext)
        {
            _redis = redis;
            _logger = logger;
            _dbContext = dbContext;
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                // Kiểm tra xem connection này có ánh xạ tới StudentId không
                if (StudentConnections.TryRemove(Context.ConnectionId, out var info))
                {
                    var (roomCode, studentId) = info;

        
                    if (Rooms.TryGetValue(roomCode, out var studentsInRoom))
                    {
                
                        if (studentsInRoom.TryRemove(studentId, out var studentName))
                        {
                            _logger.LogInformation("Student {StudentName} ({StudentId}) disconnected from room {RoomCode}",
                                studentName, studentId, roomCode);

                        
                            await Clients.Group(roomCode).SendAsync("UpdateStudentList", studentsInRoom.Values, studentsInRoom.Count);
                        }

                
                        if (studentsInRoom.IsEmpty)
                        {
                            Rooms.TryRemove(roomCode, out _);
                            _logger.LogInformation("Room {RoomCode} is empty and removed.", roomCode);
                        }
                    }

                    // Xoá dữ liệu Redis
                    await _redis.DeleteKeysByPatternAsync($"quiz:room:{roomCode}:student:{studentId}*");

                    // Loại bỏ khỏi leaderboard
                    await _redis.ZRemAsync($"quiz:room:{roomCode}:leaderboard", studentId);

                    // Xóa khỏi tập hợp student keys
                    await _redis.SRemAsync($"quiz:room:{roomCode}:student", $"quiz:room:{roomCode}:student:{studentId}");
                }
            }
            catch (Exception ex)
            {  
                _logger.LogError(ex, "Error while disconnecting student with ConnectionId {ConnectionId}", Context.ConnectionId);
            }
            finally
            {
                // Đảm bảo luôn gọi base method
                await base.OnDisconnectedAsync(exception);
            }
        }
        //[Authorize(Roles = "Teacher")]
        public async Task<string> CreateRoom(int quizId, int teacherId, int totalQuestion)
        {
            string roomCode;
            do
            {
                roomCode = new Random().Next(100000, 999999).ToString();
            } while (await _redis.KeyExistsAsync($"quiz_room_{roomCode}"));
            //int totalQuestions = await _dbContext.questions.Where(q => q.QuizId == quizId).CountAsync();
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
            var joinRoom = await _redis.GetStringAsync($"quiz:room:{roomCode}");
            CreateRoomRedisDTO joinRoomDe = new CreateRoomRedisDTO();
            if (!string.IsNullOrEmpty(joinRoom))
            {
                 joinRoomDe = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(joinRoom);
                // Sử dụng joinRoomDe tiếp theo
            }
            return JsonConvert.SerializeObject(new
            {
                studentId,
                totalStudents = TotalStudents,
                roomCode,
                quizId = joinRoomDe.QuizId
            });
        }
        //[Authorize(Roles = "Teacher")]
        public async Task<string> StartGame(string roomCode)
        {
            string roomJson = await _redis.GetStringAsync($"quiz:room:{roomCode}");
            var roomData = JsonConvert.DeserializeObject<CreateRoomRedisDTO>(roomJson);
            if (Rooms.ContainsKey(roomCode))
            {
                await Clients.Group(roomCode).SendAsync("GameStarted",roomData.QuizId);
            }
            int totalStudents = Rooms[roomCode].Count;
            if (!string.IsNullOrEmpty(roomJson))
            {
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

                //if (!string.IsNullOrEmpty(teacherConnectionId))
                //    await Clients.Client(teacherConnectionId).SendAsync("GameStarted",createRoomRedis.QuizId);
                return JsonConvert.SerializeObject(new
                {
                    quizId = createRoomRedis.QuizId,
                });
            }
            return null;
        }
        //[Authorize(Roles = "Teacher")]
        // Giáo viên Kết thúc live trước khi bắt đầu trò chơi
        public async Task EndBeforeStartGame(string roomCode)
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

                var connectionsToRemove = StudentConnections
                    .Where(kv => kv.Value.RoomCode == roomCode)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var connId in connectionsToRemove)
                {
                    StudentConnections.TryRemove(connId, out _);
                }

                _logger.LogInformation("All Redis data and in-memory data for room {RoomCode} deleted.", roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ending game for room {RoomCode}", roomCode);
            }
        }
        //[Authorize(Roles = "Teacher")]
        // Đặt hàm này trong QuizHub
        // Giáo viên click end sau khi hoàn thành trò chơi và show ra leaderboard cuối
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
                    // Cập nhật trường Rank trong Hash Set của học sinh
                    await _redis.HSetAsync(detailKey, "Rank", rank.ToString(), TimeSpan.FromHours(3));
                    rank++;
                    await StudentComplete(roomCode, studentId);
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
        //[Authorize(Roles = "Teacher")]
        // XAu khi giáo viên click nút thoát thì gọi để làm sạch rác
        public async Task EndClick(string roomCode)
        {
            try
            {
              
                await _redis.DeleteKeysByPatternAsync($"quiz:room:{roomCode}*");
                var connectionsToRemove = StudentConnections
                    .Where(kv => kv.Value.RoomCode == roomCode)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var connId in connectionsToRemove)
                {
                    StudentConnections.TryRemove(connId, out _);
                }
                Rooms.TryRemove(roomCode, out _);
                await Clients.Group(roomCode).SendAsync("EndClick", $"Room {roomCode} has ended.");

                _logger.LogInformation("Room {RoomCode} ended and all related memory & Redis data deleted.", roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ending game for room {RoomCode}", roomCode);
            }
        }
        // Khi học sinh hoàn thành bài quiz của mình
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

            var wrongAnswersDict = (studentData?.WrongAnswerRedisDTOs ?? new List<InsertWrongAnswerDTO>())
                                   .ToDictionary(w => w.QuestionId, w => w);
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
                    Options = optionResults,
                    IsSkipped = wrong != null && wrong.SelectedOptionId == null
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
        // Khi học sinh tự động thoát phòng
        public async Task LeaveRoom(string roomCode, string studentId)
        {
            if (Rooms.TryGetValue(roomCode, out var students)) // lấy values ứng với room code nếu có
            {
                students.TryRemove(studentId, out _); // xoá student ứng với studnet
                int totalStudentsLeft = students.Count;
                await Clients.Group(roomCode).SendAsync("UpdateStudentList", students.Values, totalStudentsLeft);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
            }
            StudentConnections.TryRemove(Context.ConnectionId, out _);
            await _redis.DeleteKeysByPatternAsync($"quiz:room:{roomCode}:student:{studentId}*");
        }

    }
}
