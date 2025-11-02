using Capstone.Database;
using Capstone.DTOs.Folder.Teacher;
using Capstone.Model;
using Capstone.Repositories.Folder;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static Capstone.ENUMs.TeacherFolderEnum;

namespace Capstone.Services
{
    public class TeacherFolderService : ITeacherFolder
    {
        private readonly AppDbContext _context;
        private readonly Redis _redis;
        private readonly ILogger<TeacherFolderService> _logger;
        public TeacherFolderService(ILogger<TeacherFolderService> logger , Redis redis , AppDbContext appDbContext)
        {
            _logger = logger;
            _redis = redis;
            _context = appDbContext;
        }
        public async Task<bool> createFolder(int teacherID, string folderName, int? parentFolderID )
        {
            try
            {
                var folder = new QuizzFolderModel
                {
                    TeacherId = teacherID,
                    FolderName = folderName,
                    ParentFolderId = parentFolderID,
                    CreateAt = DateTime.UtcNow
                };

                _context.quizzFolders.Add(folder);
                int check = await _context.SaveChangesAsync();
                if (check > 0)
                {                    
                    _logger.LogInformation("Tạo thư mục thành công");
                    return true;
                }              
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Không thể tạo thư mục cho teacherId={TeacherId}", teacherID);
                return false;
            }

            return false;
        }

        public async Task<List<getAllFolderDTO?>> getAllFolder(int teacherID)
        {
            try
            {
                var folders = await _context.quizzFolders
                    .Where(f => f.TeacherId == teacherID)
                    .Select(f => new getAllFolderDTO
                    {
                        FolderId = f.FolderId,
                        FolderName = f.FolderName,
                        ParentFolderId = f.ParentFolderId,
                        Folders = new List<getAllFolderDTO>()
                    })
                    .ToListAsync();

                //(Dict) trỏ tới địa chỉ lưu FolderId của folders  
                var folderDict = folders.ToDictionary(f => f.FolderId);

                var rootFolders = new List<getAllFolderDTO>();

                foreach (var folder in folders)
                {
                    if (folder.ParentFolderId.HasValue && folderDict.ContainsKey(folder.ParentFolderId.Value))
                    {
                        folderDict[folder.ParentFolderId.Value].Folders.Add(folder);
                    }
                    else                    {
                        rootFolders.Add(folder);
                    }
                }

                _logger.LogInformation("Lấy tất cả thư mục thành công");
                return rootFolders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex ,"Không thể lấy danh sách thư mục");
                return null;
            }

        }
        public async Task<FolderDetailDTO> GetFolderDetail(int teacherId, int folderId)
        {
            try
            {
                var folder = await _context.quizzFolders
                            .FirstOrDefaultAsync(f => f.TeacherId == teacherId && f.FolderId == folderId);

                if (folder == null)
                {
                    _logger.LogWarning($"Thư mục {folderId} không tồn tại hoặc không thuộc về giáo viên {teacherId}");
                    return null;
                }

                var teacher = await _context.authModels
                            .Where(t => t.AccountId == teacherId)
                            .FirstOrDefaultAsync();
                string teacherEmail = teacher?.Email ?? "Unknown";

                //var subFolders = await _context.quizzFolders
                //    .Where(f => f.ParentFolderId == folder.FolderId)
                //    .Select(sf => new SubFolderDTO
                //    {
                //        Id = sf.FolderId,
                //        Name = sf.FolderName
                //    })
                //    .ToListAsync();

                var quizzes = await (from q in _context.quizzes
                                     join t in _context.topics on q.TopicId equals t.TopicId
                                     where q.FolderId == folderId && q.TeacherId == teacherId
                                     select new QuizzFolderDTO
                                     {
                                         QuizzId = q.QuizId,
                                         Title = q.Title,
                                         AvatarURL = q.AvatarURL,
                                         TotalQuestion = q.Questions.Count,
                                         TopicName = t.TopicName,
                                         TotalParticipants = q.TotalParticipants,
                                         TeacherName = teacherEmail 
                                     })
                      .ToListAsync();

                _logger.LogInformation("Lấy chi tiết thư mục thành công");

                return new FolderDetailDTO
                {
                    FolderID = folder.FolderId,
                    FolderName = folder.FolderName,
                    ParentFolderID = folder.ParentFolderId,
                    QuizzFolder = quizzes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể lấy thư mục câu đố");
                return null;
            }
        }

        public async Task<bool> RemoveQuizToOtherFolder(int quizId, int folderId)
        {
            try
            {
                int isChange = await _context.quizzes.Where(qf => qf.QuizId == quizId).ExecuteUpdateAsync
                    (e => e.SetProperty(qf => qf.FolderId, folderId));
                if (isChange > 0) { 
                    return true;
                }
                return false;
            }
            catch (Exception ex) {
                return false;
            }
        }

        public async Task<bool> UpdateFolder(int folderId, string folderName)
        {
            try
            {
                int UpdateCoute = await _context.quizzFolders.Where(qf => qf.FolderId == folderId).ExecuteUpdateAsync
                    (e => e.SetProperty(qf => qf.FolderName, folderName)
                           .SetProperty(qf => qf.UpdateAt, DateTime.Now));
                if (UpdateCoute > 0) { 
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<CheckQuizInFolder> DeleteFolder(int folderId)
        {
            try
            {
                bool hasQuiz = await _context.quizzes.AnyAsync(q => q.FolderId == folderId);
                if (hasQuiz)
                {
                    return CheckQuizInFolder.HasQuiz;
                }
                int deletedCount = await _context.quizzFolders.Where(qf => qf.FolderId == folderId).ExecuteDeleteAsync();
                if(deletedCount > 0)
                {
                    return CheckQuizInFolder.Success;
                }
                return CheckQuizInFolder.Error;
            }
            catch (Exception ex) { 
                return CheckQuizInFolder.Error;
            }
        }
    }
}
