using Microsoft.EntityFrameworkCore;
using Models;

namespace Database
{
    /// <summary>
    /// EF Core database context for the application.
    /// Configures entity sets and relationships for Users, Stats, Sessions, Global stats, and Authentication.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructs the context with the specified options (e.g., connection string, server version).
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Users table mapping.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// UserGameStats table mapping (one-to-one with User).
        /// </summary>
        public DbSet<UserGameStats> UserGameStats { get; set; }

        /// <summary>
        /// GameSessions table mapping.
        /// </summary>
        public DbSet<GameSession> GameSessions { get; set; }

        /// <summary>
        /// GlobalGameStats table mapping (usually a single row tracking overall metrics).
        /// </summary>
        public DbSet<GlobalGameStats> GlobalGameStats { get; set; }

        /// <summary>
        /// Authentications table mapping for refresh token records.
        /// </summary>
        public DbSet<Authentication> Authentications { get; set; }

        /// <summary>
        /// Configure table names, primary keys, relationships, and constraints.
        /// Called by the framework on model creation.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.Username)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(u => u.Password)
                      .IsRequired();

                entity.HasMany(u => u.GameSessionsAsUser1)
                      .WithOne(gs => gs.User1)
                      .HasForeignKey(gs => gs.User1Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.GameSessionsAsUser2)
                      .WithOne(gs => gs.User2)
                      .HasForeignKey(gs => gs.User2Id)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // UserGameStats entity configuration
            modelBuilder.Entity<UserGameStats>(entity =>
            {
                entity.ToTable("user_game_stats");
                entity.HasKey(ugs => ugs.UserId);

                entity.HasOne(ugs => ugs.User)
                      .WithOne(u => u.UserGameStats)
                      .HasForeignKey<UserGameStats>(ugs => ugs.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // GameSession entity configuration
            modelBuilder.Entity<GameSession>(entity =>
            {
                entity.ToTable("game_sessions");
                entity.HasKey(gs => gs.SessionId);
            });

            // GlobalGameStats entity configuration
            modelBuilder.Entity<GlobalGameStats>(entity =>
            {
                entity.ToTable("global_game_stats");
                entity.HasKey(g => g.Id);
            });

            // Authentication entity configuration
            modelBuilder.Entity<Authentication>(entity =>
            {
                entity.ToTable("authentications");
                entity.HasKey(a => a.AuthId);

                entity.HasOne(a => a.User)
                      .WithMany() // One user may have multiple refresh tokens
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
