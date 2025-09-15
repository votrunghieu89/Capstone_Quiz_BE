using Capstone.Model;
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
        public DbSet<Profile_CDD_Admin> profile_CDD_Admins { get; set; }
        public DbSet<Profile_Recruiter> profile_Recruiters { get; set; }

    }
}
