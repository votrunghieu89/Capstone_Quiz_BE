using Capstone.Database;
using Capstone.DTOs.RecruiterProfile;
using Capstone.Model;
using Capstone.Repositories.Profile;
using Microsoft.EntityFrameworkCore;


namespace Capstone.Services
{
    public class RecruiterProfileService : IRecruiterProfileRepository
    {
        public readonly AppDbContext _dbContext;
        public readonly ILogger<RecruiterProfileService> _logger;
        public RecruiterProfileService(AppDbContext dbContext , ILogger<RecruiterProfileService> logger)
        {   
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CreateJD(RecruiterProfileCreateJDDTO createDTO)
        {
            bool connection = _dbContext.Database.CanConnect();
            if (!connection) return false;
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var exists = await _dbContext.profileCompanies
                                             .AnyAsync(pc => pc.PCId == createDTO.PCId);

                if (!exists)
                {
                    _logger.LogWarning("PCId {PCId} does not exist in ProfileCompany", createDTO.PCId);
                    return false; 
                }

                var jd = new JDsModel(
                    createDTO.PCId,
                    createDTO.JDTitle,
                    createDTO.JDSalary,
                    createDTO.JDLocation,
                    createDTO.JDExperience,
                    createDTO.JDExpiredTime,
                    DateTime.Now,
                    DateTime.Now
                );
                await _dbContext.jdsModels.AddAsync(jd);
                int checkjd = await _dbContext.SaveChangesAsync();
                var jdDetail = new JDDetailModel(
                    jd.JDId,
                    createDTO.Description,
                    createDTO.Requirement,
                    createDTO.Benefits,
                    createDTO.Location,
                    createDTO.WorkingTime,
                    DateTime.Now,
                    DateTime.Now
                );
 
                
                await _dbContext.jdDetailModels.AddAsync(jdDetail);
                int checkdt= await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if ( checkjd >0 && checkdt > 0)
                {
                    _logger.LogInformation("Create JDs successfull");
                    return true;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Can not create JDs");                
                return false;
            }

            return false;
        }


        public async Task<bool> DeleteJD(int ID)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateJD(int ID)
        {
            throw new NotImplementedException();
        }
    }
}
