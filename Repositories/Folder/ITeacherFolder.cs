using Capstone.DTOs.Folder.Teacher;
using System.Numerics;
using static Capstone.ENUMs.TeacherFolderEnum;

namespace Capstone.Repositories.Folder
{
    public interface ITeacherFolder
    {
        public Task<List<getAllFolderDTO?>> getAllFolder(int teacherID);
        public Task<bool> createFolder (int  teacherID , string folderName , int ? parentFolderID );
        public Task<FolderDetailDTO> GetFolderDetail(int techerId, int folderId);
        public Task<bool> UpdateFolder(int folderId, string folderName);
        public Task<CheckQuizInFolder> DeleteFolder(int folderId);
        public Task<bool> RemoveQuizToOtherFolder(int quizId, int folderId);

    }
}

