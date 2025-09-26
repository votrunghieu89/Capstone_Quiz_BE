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
        public async Task<bool> checkConnection()
        {
            try
            {
                bool canConnect = await _dbContext.Database.CanConnectAsync();
                if (canConnect)
                {
                    _logger.LogInformation("Database connection successful.");
                    return true;
                }
                else
                {
                    _logger.LogError("Database connection failed.");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in checkConnection");
                return false;
            }
        }
        public async Task<bool> CreateJD(RecruiterProfileCreateJDDTO createDTO)
        {
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

                foreach (var positionId in createDTO.PositionIds ?? new List<int>())
                {
                    var position = new JDPositionModel()
                    {
                        JDId = jd.JDId,
                        PositionId = positionId,
                        CreatedAt = DateTime.Now
                    };
                    await _dbContext.jDPositionsModel.AddAsync(position);
                }

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
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                int checkDeleteJDDetail = await _dbContext.jDDetailModels
                    .Where(jdd => jdd.JDId == ID)
                    .ExecuteDeleteAsync();

                int checkDeleteJDPosition = await _dbContext.jDPositionsModel
                    .Where(jdp => jdp.JDId == ID)
                    .ExecuteDeleteAsync();

                int checkDeleteJD = await _dbContext.jDsModel
                    .Where(jd => jd.JDId == ID)
                    .ExecuteDeleteAsync();

                if (checkDeleteJD > 0)
                {
                    _logger.LogInformation(
                        "Deleted JDId {ID}: {JDCount} JD(s), {JDDetailCount} JDDetail(s), {JDPositionCount} JDPosition(s)",
                        ID, checkDeleteJD, checkDeleteJDDetail, checkDeleteJDPosition
                    );

                    await transaction.CommitAsync();
                    return true;
                }
                else
                {
                    _logger.LogWarning("JD with JDId {ID} not found", ID);
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting JD with ID {ID}", ID);
                return false;
            }
        }
        public async Task<bool> UpdateJD(RecruiterProfileUpdateJDDTO updateDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Update JD (batch update)
                int jdUpdated = await _dbContext.jDsModel
                    .Where(jd => jd.JDId == updateDTO.JDId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(jd => jd.JDTitle, updateDTO.JDTitle)
                        .SetProperty(jd => jd.JDSalary, updateDTO.JDSalary)
                        .SetProperty(jd => jd.JDLocation, updateDTO.JDLocation)
                        .SetProperty(jd => jd.JDExperience, updateDTO.JDExperience)
                        .SetProperty(jd => jd.JDExpiredTime, updateDTO.JDExpiredTime)
                        .SetProperty(jd => jd.UpdatedAt, DateTime.Now)
                    );

                if (jdUpdated == 0) return false; // JD không tồn tại

                // Update JDDetail (batch update)
                await _dbContext.jDDetailModels
                    .Where(d => d.JDId == updateDTO.JDId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(d => d.Description, updateDTO.Description)
                        .SetProperty(d => d.Requirement, updateDTO.Requirement)
                        .SetProperty(d => d.Benefits, updateDTO.Benefits)
                        .SetProperty(d => d.Location, updateDTO.Location)
                        .SetProperty(d => d.WorkingTime, updateDTO.WorkingTime)
                        .SetProperty(d => d.UpdatedAt, DateTime.Now)
                    );

                // Đồng bộ Position
                var oldList = await _dbContext.jDPositionsModel
                    .Where(jdp => jdp.JDId == updateDTO.JDId)
                    .Select(jdp => jdp.PositionId)
                    .ToListAsync();
               
                var newList = updateDTO.PositionIds ?? new List<int>();

                // Xoá những cái có trong oldList mà không có trong newList
                var toDelete = oldList.Except(newList).ToList();
                if (toDelete.Any())
                {
                    await _dbContext.jDPositionsModel
                        .Where(jdp => jdp.JDId == updateDTO.JDId && toDelete.Contains(jdp.PositionId))
                        .ExecuteDeleteAsync();
                }

                // xoá những cái có trong newList mà không có trong oldList
                var toInsert = newList.Except(oldList).ToList();
                if (toInsert.Any())
                {
                    var newEntities = toInsert.Select(pid => new JDPositionModel
                    {
                        JDId = updateDTO.JDId,
                        PositionId = pid,
                        CreatedAt = DateTime.Now
                    }).ToList();

                    await _dbContext.jDPositionsModel.AddRangeAsync(newEntities);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating JD with ID {JDId}", updateDTO.JDId);
                return false;
            }
        }
        public async Task<List<PositionModel>> getAllPosition()
        {
            try
            {
                var positions = await _dbContext.positionsModel.ToListAsync();

                if (positions != null && positions.Count > 0)
                {
                    _logger.LogInformation("getAllPosition successful");
                    return positions;
                }
                else
                {
                    _logger.LogWarning("No positions found");
                    return new List<PositionModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in getAllPosition");
                return new List<PositionModel>();
            }
        }

        public async Task<List<RecruiterProfileShowJDDTO>> GetAllJD(int accountId)
        {
            try
            {
                var query = from pc in _dbContext.profileCompanies
                            join jd in _dbContext.jDsModel on pc.PCId equals jd.PCId
                            join jdd in _dbContext.jDDetailModels on jd.JDId equals jdd.JDId into jddg
                            from jdd in jddg.DefaultIfEmpty()
                            where pc.AccountId == accountId
                            select new RecruiterProfileShowJDDTO
                            {
                                PCId = pc.PCId,
                                CompanyName = pc.CompanyName,
                                AvatarURL = pc.AvatarURL,
                                CompanyAddress = pc.CompanyAddress,
                                JDTitle = jd.JDTitle,
                                JDSalary = jd.JDSalary,
                                JDLocation = jd.JDLocation,
                                JDExperience = jd.JDExperience,
                                JDExpiredTime = jd.JDExpiredTime,
                                Description = jdd != null ? jdd.Description ?? string.Empty : string.Empty,
                                Requirement = jdd != null ? jdd.Requirement ?? string.Empty : string.Empty,
                                Benefits = jdd != null ? jdd.Benefits ?? string.Empty : string.Empty,
                                Location = jdd != null ? jdd.Location ?? string.Empty : string.Empty,
                                WorkingTime = jdd != null ? jdd.WorkingTime ?? string.Empty : string.Empty,
                                PositionName = (from jdp in _dbContext.jDPositionsModel
                                                join p in _dbContext.positionsModel on jdp.PositionId equals p.PositionId
                                                where jdp.JDId == jd.JDId
                                                select p.PositionName).ToList()
                            };

                var result = await query
                                .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in GetAllJD");
                return new List<RecruiterProfileShowJDDTO>();
            }
        }
    }
}


