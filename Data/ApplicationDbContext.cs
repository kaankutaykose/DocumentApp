using DocumentApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions options):base(options) { 
        
        }
        
        public DbSet<DocumentApp.Models.Topic> Topic { get; set; }
        public DbSet<DocumentApp.Models.Documents> Documents { get; set; }
        public DbSet<DocumentApp.Models.Admin> Admin { get; set; }
        public DbSet<DocumentApp.Models.User> User { get; set; }

        public DbSet<DocumentApp.Models.Unit> Unit { get; set; }

        

    }
}
