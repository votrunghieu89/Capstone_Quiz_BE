﻿using Capstone.Database;
using Capstone.DTOs.Folder.Teacher;
using Capstone.Model;
using Capstone.Repositories.Folder;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
                    _logger.LogInformation("Create folder succesfull");
                    return true;
                }              
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Cannot create folder for teacherId={TeacherId}", teacherID);
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

                _logger.LogInformation("Get all folders successful");
                return rootFolders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex ,"Can not get list folder");
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
                    _logger.LogWarning($"Folder {folderId} not exists or not belong to teacher {teacherId}");
                    return null;
                }

                var teacher = await _context.authModels
                            .Where(t => t.AccountId == teacherId)
                            .FirstOrDefaultAsync();
                string teacherEmail = teacher?.Email ?? "Unknown";

                var subFolders = await _context.quizzFolders
                    .Where(f => f.ParentFolderId == folder.FolderId)
                    .Select(sf => new SubFolderDTO
                    {
                        Id = sf.FolderId,
                        Name = sf.FolderName
                    })
                    .ToListAsync();

                var quizzes = await _context.quizzes
                    .Where(q => q.FolderId == folder.FolderId && q.TeacherId == teacherId)
                    .Select(q => new QuizzFolderDTO
                    {
                        QuizzId = q.QuizId,
                        Title = q.Title,
                        AvatarURL = q.AvartarURL,
                        TotalQuestion = q.Questions.Count,
                        TeacherName = teacherEmail
                    })
                    .ToListAsync();

                _logger.LogInformation("Get folder detail successful");

                return new FolderDetailDTO
                {
                    FolderID = folder.FolderId,
                    FolderName = folder.FolderName,
                    ParentFolderID = folder.ParentFolderId,
                    SubFolder = subFolders,
                    QuizzFolder = quizzes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not get Quizz folder");
                return null;
            }
        }


    }
}
