using Capstone.Model.Others;
using Capstone.Model.Profile;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Capstone.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
        {
        }
        public DbSet<AuthModel> authModels { get; set; }
        public DbSet<ProfileCandidate> profileCandidates { get; set; }
        public DbSet<ProfileCompany> profileCompanies { get; set; }

    }
}
