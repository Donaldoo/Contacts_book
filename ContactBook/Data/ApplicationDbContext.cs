using ContactBook.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContactBook.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Contacts> Contacts { get; set; }
        public DbSet<AppUser> AppUser { get; set; }
        public DbSet<Email> Emails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Email>()
                .HasOne(e => e.Contacts)
                .WithMany(c => c.Email)
                .HasForeignKey(e => e.ContactId);

            modelBuilder.Entity<Contacts>()
                .HasMany(c => c.Email)
                .WithOne(e => e.Contacts)
                .HasForeignKey(e => e.ContactId);
        }
    }
}
 