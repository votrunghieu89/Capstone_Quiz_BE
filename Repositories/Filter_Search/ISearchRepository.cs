using Capstone.DTOs.Admin;

namespace Capstone.Repositories.Filter_Search
{
    public interface ISearchRepository
    {
        public Task<AllAccountByRoleDTO> SearchAccountByEmail(int email); // tìm kiếm account trong hiển thị full tk admin dashboard
        public Task<bool> FilterByRole(int role, int page, int pageSize); // lọc trong full tk admin dashboard
        public Task<bool> FilterByTopic(int role, int page, int pageSize); // // lọc trong full tk admin dashboard
        public Task<bool> SearchParticipantInGroup(int Name, int groupId); // Tìm kiếm xem hs nào trong group
        public Task<bool> SearchStudentInOfflineReport(int Name, int reportId); // tìm kiếm tên student trong report cả on và off
        public Task<bool> SearchStudentInOnlineReport(int Name, int reportId);
    }
}
