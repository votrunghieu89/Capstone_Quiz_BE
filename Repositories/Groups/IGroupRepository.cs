using Capstone.DTOs.Group;
using Capstone.Model;
using static Capstone.ENUMs.GroupEnum;

namespace Capstone.Repositories.Groups
{
    public interface IGroupRepository
    {
        // CRUD for Group by Teacher
        public Task<GroupModel> CreateGroup (GroupModel groupModel, string ipAddress); //
        public Task<bool> DeleteGroup (int groupId, int accountId, string ipAddress); //
        public Task<UpdateGroupDTO> updateGroup(UpdateGroupDTO groupModel, int accountId, string ipAddress); //
        public Task<GroupModel> GetGroupDetailById(int groupId); //
        public Task<List<AllGroupDTO>> GetAllGroupsbyTeacherId(int TeacherId); //

        public Task<JoinGroupResult> InsertStudentToGroup(int groupId, string IdUnique, int accountId, string ipAddress); // 
        public Task<bool> RemoveStudentFromGroup(int groupId, int studentId, int teacherId, string ipAddress); // 
        public Task<List<ViewStudentDTO>> GetAllStudentsByGroupId(int groupId);   //

        public Task<InsertQuiz> InsertQuizToGroup(InsertQuiz insertQuiz, int accountId, string ipAddress); //
        public Task<bool> RemoveQuizFromGroup(int QgID, int groupId, int quizId, int accountId, string ipAddress); //
        public Task<List<ViewQuizDTO>> GetAllDeliveredQuizzesByGroupId(int groupId); //

        // For Student
        public Task<List<AllGroupDTO>> GetAllGroupsByStudentId(int studentId); //
        public Task<bool> LeaveGroup(int groupId, int studentId, int teacherId, string ipAddress); // 
        public Task<JoinGroupResult> JoinGroupByInvite(string inviteCode, int studentId, string ipAddress);

        
    }
}
