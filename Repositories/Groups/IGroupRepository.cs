using Capstone.DTOs.Group;
using Capstone.Model;
using static Capstone.ENUMs.GroupEnum;

namespace Capstone.Repositories.Groups
{
    public interface IGroupRepository
    {
        // CRUD for Group by Teacher
        public Task<GroupModel> CreateGroup (GroupModel groupModel); //
        public Task<bool> DeleteGroup (int groupId); //
        public Task<UpdateGroupDTO> updateGroup(UpdateGroupDTO groupModel); //
        public Task<GroupModel> GetGroupDetailById(int groupId); //
        public Task<List<AllGroupDTO>> GetAllGroupsbyTeacherId(int TeacherId); //

        public Task<JoinGroupResult> InsertStudentToGroup(int groupId, string IdUnique); // 
        public Task<bool> RemoveStudentFromGroup(int groupId, int studentId); // 
        public Task<List<ViewStudentDTO>> GetAllStudentsByGroupId(int groupId);   //

        public Task<InsertQuiz> InsertQuizToGroup(InsertQuiz insertQuiz); //
        public Task<bool> RemoveQuizFromGroup(int groupId, int quizId); //
        public Task<List<ViewQuizDTO>> GetAllQuizzesByGroupId(int groupId); //

        // For Student
        public Task<List<AllGroupDTO>> GetAllGroupsByStudentId(int studentId); //
        public Task<bool> LeaveGroup(int groupId, int studentId); // 
        public Task<JoinGroupResult> JoinGroupByInvite(string inviteCode, int studentId);

        
    }
}
