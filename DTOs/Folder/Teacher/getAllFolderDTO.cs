namespace Capstone.DTOs.Folder.Teacher
{
    public class getAllFolderDTO
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }

        public List<getAllFolderDTO> Folders { get; set; } = new List<getAllFolderDTO> ();
    }
}
