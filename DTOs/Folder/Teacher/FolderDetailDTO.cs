namespace Capstone.DTOs.Folder.Teacher
{
    public class FolderDetailDTO
    {
        public int FolderID { get; set; }
        public string FolderName { get; set; }
        public int? ParentFolderID { get; set; }

        //public List<SubFolderDTO> SubFolder { get; set; } = new List<SubFolderDTO>();
        public List <QuizzFolderDTO> QuizzFolder { get; set; } = new List<QuizzFolderDTO> ();
    }
}
