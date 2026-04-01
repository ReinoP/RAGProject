using AIproject.Models;
using Microsoft.EntityFrameworkCore;

namespace AIproject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentChunk>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<DocumentChunk>()
                .Property(f => f.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
