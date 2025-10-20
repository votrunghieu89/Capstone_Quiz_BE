using Capstone.DTOs.Admin;

namespace Capstone.Repositories.Filter_Search
{
    public interface ISearchRepository
    {
        public Task<AllAccountByRoleDTO> SearchAccountByEmail(int email);
        public Task<bool> FilterByRole(int role, int page, int pageSize);
        public Task<bool> FilterByTopic(int role, int page, int pageSize);
        public Task<bool> SearchParticipantInGroup(int Name, int groupId);
        public Task<bool> SearchStudentInReport(int Name, int reportId);
    }
}
