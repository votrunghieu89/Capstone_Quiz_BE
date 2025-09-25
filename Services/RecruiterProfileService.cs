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
        public RecruiterProfileService(AppDbContext dbContext, ILogger<RecruiterProfileService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
      
        public async Task<bool> CreateJD(RecruiterProfileCreateJDDTO createDTO)
        {
            bool connection = _dbContext.Database.CanConnect();
            if (!connection)
            {
                _logger.LogError("Can not connection server ");
                return false;
            }
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

                var jd = new JDsModel()
                {
                    PCId = createDTO.PCId,
                    JDTitle = createDTO.JDTitle,
                    JDSalary = createDTO.JDSalary,
                    JDLocation = createDTO.JDLocation,
                    JDExperience = createDTO.JDExperience,
                    JDExpiredTime = createDTO.JDExpiredTime,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };
                await _dbContext.jDsModel.AddAsync(jd);
                int checkjd = await _dbContext.SaveChangesAsync();

                var jdDetail = new JDDetailModel()
                {
                    JDId = jd.JDId,
                    Description = createDTO.Description,
                    Requirement = createDTO.Requirement,
                    Benefits = createDTO.Benefits,
                    Location = createDTO.Location,
                    WorkingTime = createDTO.WorkingTime,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };
                await _dbContext.jDDetailModels.AddAsync(jdDetail);
                int checkdt = await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                if (checkjd > 0 && checkdt > 0)
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
            bool connection = _dbContext.Database.CanConnect();
            if (!connection)
            {
                _logger.LogError("Can not connection server ");
                return false;
            }
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                int checkDeleteJDDetail = await _dbContext.jDDetailModels.Where(jdd => jdd.JDId == ID).ExecuteDeleteAsync();
                int checkDeleteJD = await _dbContext.jDsModel.Where(jd => jd.JDId == ID).ExecuteDeleteAsync();

                if (checkDeleteJD > 0)
                {
                    _logger.LogInformation($"Deleted {checkDeleteJD} read JD \n " +
                                           $"Deleteted {checkDeleteJDDetail} read JD Detail ");
                    await transaction.CommitAsync();
                    return true;
                }
                else
                {
                    _logger.LogError("JD with JDId {ID} not found ", ID);
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read JD ");
            }
            return false;

        }

        public async Task<bool> UpdateJD(RecruiterProfileUpdateJDDTO updateJDDTO)
        {
            bool connection = _dbContext.Database.CanConnect();
            if (!connection)
            {
                _logger.LogError("Can not connection server ");
                return false;
            }
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var JD = await _dbContext.jDsModel.FirstOrDefaultAsync(jd => jd.JDId == updateJDDTO.JDId);
                if (JD == null)
                {
                    _logger.LogWarning("JD {JDId} not found", updateJDDTO.JDId);
                    return false;
                }

                if (updateJDDTO.JDTitle != null)
                {
                    JD.JDTitle = updateJDDTO.JDTitle;
                }
                if (updateJDDTO.JDSalary != null)
                {
                    JD.JDSalary = updateJDDTO.JDSalary;
                }
                if (updateJDDTO.JDLocation != null)
                {
                    JD.JDLocation = updateJDDTO.JDLocation;
                }
                if (updateJDDTO.JDExperience != null)
                {
                    JD.JDExperience = updateJDDTO.JDExperience;
                }
                if (updateJDDTO.JDExpiredTime.HasValue)
                {
                    JD.JDExpiredTime = updateJDDTO.JDExpiredTime.Value;
                }
                JD.UpdatedAt = DateTime.Now;


                var JDD = await _dbContext.jDDetailModels.FirstOrDefaultAsync(jdd => jdd.JDId == updateJDDTO.JDId);
                if (JDD != null)
                {
                    if (updateJDDTO.Description != null)
                    {
                        JDD.Description = updateJDDTO.Description;
                    }
                    if (updateJDDTO.Requirement != null)
                    {
                        JDD.Requirement = updateJDDTO.Requirement;
                    }
                    if (updateJDDTO.Benefits != null)
                    {
                        JDD.Benefits = updateJDDTO.Benefits;
                    }
                    if (updateJDDTO.Location != null)
                    {
                        JDD.Location = updateJDDTO.Location;
                    }
                    if (updateJDDTO.WorkingTime != null)
                    {
                        JDD.WorkingTime = updateJDDTO.WorkingTime;
                    }
                    JDD.UpdatedAt = DateTime.Now;
                }
                _logger.LogInformation("Updated JD {JDId} successfully", updateJDDTO.JDId);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating JD {JDId}", updateJDDTO.JDId);
                await transaction.RollbackAsync();
                return false;
            }

        }
    }
}


