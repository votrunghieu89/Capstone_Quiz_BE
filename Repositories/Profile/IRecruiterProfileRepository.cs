using Capstone.DTOs.RecruiterProfile;
using Capstone.Model;

namespace Capstone.Repositories.Profile
{
    public interface IRecruiterProfileRepository
    {


        // Quản lí JD ( create, update, delete) (recruiter)
        // Hiẻn thị toàn bộ JD đã tạo
        // Hiển thị toàn bộ Cvs đã apply vào JD (recruiter) ( nằm bên trong 1 JD)
        // update profile
        public Task<bool> checkConnection();
        Task<bool> CreateJD(RecruiterProfileCreateJDDTO createDTO);

        Task<bool> UpdateJD(RecruiterProfileUpdateJDDTO updateJDDTO);
        Task<bool> DeleteJD(int ID);
        Task<List<RecruiterProfileShowJDDTO>> GetAllJD(int accountId);

        // lấy toàn bộ Position
        public Task<List<PositionModel>> getAllPosition();

        // Viewing score between CV and JD,
        // accept or reject CV

        // Thêm JD yêu thích ( , recruiter)
        // Xóa JD yêu thích ( , recruiter)
        // Lấy danh sách JD yêu thích ( , recruiter)

        // Thêm CV yêu thích ( recruiter)
        // Xóa CV yêu thích ( recruiter)
        // Lấy danh sách CV yêu thích ( recruiter)

        // Thêm công ty yêu thích ( recruiter)
        // Xoá công ty yêu thích ( recruiter)
        // Lấy danh sách công ty yêu thích ( recruiter)
    }
}
