using Capstone.DTOs.Folder.Teacher;
using System.Numerics;

namespace Capstone.Repositories.Folder
{
    public interface ITeacherFolder
    {
        public Task<List<getAllFolderDTO?>> getAllFolder(int teacherID);
        public Task<bool> createFolder (int  teacherID , string folderName , int ? parentFolderID );
        public Task<FolderDetailDTO> GetFolderDetail(int techerId, int folderId);
    }
}

