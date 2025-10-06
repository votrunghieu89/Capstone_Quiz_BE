using Capstone.DTOs.Admin;

namespace Capstone.Repositories.Admin
{
    public interface IAdminRepository
    {
        public Task<int> GetNumberOfCreatedAccount();
        public Task<int> GetNumberOfCreatedAccountByMonth(int month , int year);

        public Task<int> GetNumberOfCreatedStudentAcount();
        public Task<int> GetNumberOfCreatedStudentAcountByMonth(int month, int year);

        public Task<int> GetNumberOfCreatedTeacherAccount();
        public Task<int> GetNumberOfCreatedTeacherAccountByMonth(int month, int year);
        public Task<bool> DeleteAccount(int accountId);

        public Task<List<AllAccountByRoleDTO>> GetAllAccountByRole(int page, int pageSize);

        public Task<int> GetNumberOfCreatedQuizzes ();
        public Task<int> GetNumberOfCreatedQuizzesByMonth(int month, int year);
    }
}
