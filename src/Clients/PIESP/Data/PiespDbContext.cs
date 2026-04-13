using Microsoft.EntityFrameworkCore;
using IntegrationHub.PIESP.Models;

namespace IntegrationHub.PIESP.Data
{
    /// <summary>
    /// Kontekst bazodanowy modułu PIESP.
    /// Tabele są tworzone w schemacie „piesp” bazy IntegrationHubDB.
    /// </summary>
    public class PiespDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<SecurityCode> SecurityCodes => Set<SecurityCode>();
        public DbSet<Duty> Duties => Set<Duty>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


        public PiespDbContext(DbContextOptions<PiespDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Użytkownik
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users", "piesp");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id)
                   .HasColumnType("uniqueidentifier")
                   .HasDefaultValueSql("newsequentialid()");
                e.Property(x => x.UserName).IsRequired().HasMaxLength(200);
                e.Property(x => x.BadgeNumber).IsRequired().HasMaxLength(32);
                e.HasIndex(x => x.BadgeNumber).IsUnique();
                e.Property(x => x.SamAccountName).HasMaxLength(256);
                e.HasIndex(x => x.SamAccountName).IsUnique().HasFilter("[SamAccountName] IS NOT NULL");
                e.Property(x => x.UnitName).HasMaxLength(200);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.Property(x => x.KsipUserId).HasMaxLength(64);
                e.Property(x => x.PinHash).HasMaxLength(200);
                e.Property(x => x.TokenVersion)
                    .IsRequired()
                    .HasDefaultValue(0);
            });

            // Role
            modelBuilder.Entity<UserRole>(e =>
            {
                e.ToTable("UserRoles", "piesp");
                e.HasKey(x => x.Id);
                e.Property(x => x.Role).IsRequired();
                e.Property(x => x.UserId).IsRequired();
                
            });

            // Kody bezpieczeństwa
            modelBuilder.Entity<SecurityCode>(e =>
            {
                e.ToTable("SecurityCodes", "piesp");
                e.HasKey(x => x.Id);
                e.Property(x => x.BadgeNumber).IsRequired().HasMaxLength(32);
                e.Property(x => x.Code).IsRequired().HasMaxLength(6);
                e.Property(x => x.Expiry).IsRequired();
                e.HasIndex(x => new { x.BadgeNumber, x.Code });
            });

            // Służby
            modelBuilder.Entity<Duty>(e =>
            {
                e.ToTable("Duties", "piesp");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserId).IsRequired();
                e.Property(x => x.Type).IsRequired().HasMaxLength(100);
                e.Property(x => x.Unit).HasMaxLength(200);
                e.Property(x => x.Status).IsRequired();
                // Współrzędne faktycznego rozpoczęcia / zakończenia służby
                e.Property(x => x.ActualStartLatitude)
                    .HasColumnType("decimal(9,6)");

                e.Property(x => x.ActualStartLongitude)
                    .HasColumnType("decimal(9,6)");

                e.Property(x => x.ActualEndLatitude)
                    .HasColumnType("decimal(9,6)");

                e.Property(x => x.ActualEndLongitude)
                    .HasColumnType("decimal(9,6)");
                // relacja na FK UserId (bez nawigacji po stronie Duty -> User w tym minimalnym zakresie)
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // RefreshTokens
            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshTokens", "piesp");
                e.HasKey(x => x.Id);

                e.Property(x => x.UserId).IsRequired();
                e.Property(x => x.FamilyId).IsRequired();

                e.Property(x => x.TokenHash)
                    .IsRequired()
                    .HasColumnType("varbinary(64)"); // SHA-256 = 32 bajty

                e.Property(x => x.IssuedAt).IsRequired();
                e.Property(x => x.ExpiresAt).IsRequired();

                e.Property(x => x.RevokedReason).HasMaxLength(50);

                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.FamilyId);

                // relacja do Users (bez nawigacji – minimalnie)
                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
