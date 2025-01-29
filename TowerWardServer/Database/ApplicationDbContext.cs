using Microsoft.EntityFrameworkCore;
using Models;

namespace Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserGameStats> UserGameStats { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<GlobalGameStats> GlobalGameStats { get; set; }

        // Add this line:
        public DbSet<Authentication> Authentications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Example: rename tables or apply constraints
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // optional, if you want a custom table name
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.Username)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(u => u.Password)
                      .IsRequired();

                // relationships
                entity.HasMany(u => u.GameSessionsAsUser1)
                      .WithOne(gs => gs.User1)
                      .HasForeignKey(gs => gs.User1Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.GameSessionsAsUser2)
                      .WithOne(gs => gs.User2)
                      .HasForeignKey(gs => gs.User2Id)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserGameStats>(entity =>
            {
                entity.ToTable("user_game_stats");
                entity.HasKey(ugs => ugs.UserId);

                // 1-to-1 with User
                entity.HasOne(ugs => ugs.User)
                      .WithOne(u => u.UserGameStats)
                      .HasForeignKey<UserGameStats>(ugs => ugs.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GameSession>(entity =>
            {
                entity.ToTable("game_sessions");
                entity.HasKey(gs => gs.SessionId);
            });

            modelBuilder.Entity<GlobalGameStats>(entity =>
            {
                entity.ToTable("global_game_stats");
                entity.HasKey(g => g.Id);
            });

            // Optionally, config for Authentication:
            modelBuilder.Entity<Authentication>(entity =>
            {
                entity.ToTable("authentications"); // or name it something else if you prefer
                entity.HasKey(a => a.AuthId);

                // If you want a foreign key
                entity.HasOne(a => a.User)
                      .WithMany() // or .WithOne() if it's a 1-to-1
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
