using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<WelfareProgram> WelfarePrograms => Set<WelfareProgram>();
    public DbSet<EligibilityRule> EligibilityRules => Set<EligibilityRule>();
    public DbSet<WelfareApplication> WelfareApplications => Set<WelfareApplication>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<ApplicationReview> ApplicationReviews => Set<ApplicationReview>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RoleAccessRequest> RoleAccessRequests => Set<RoleAccessRequest>();
    public DbSet<Anomaly> Anomalies => Set<Anomaly>();
    public DbSet<AnomalyRule> AnomalyRules => Set<AnomalyRule>();
    public DbSet<AnomalyAlert> AnomalyAlerts => Set<AnomalyAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.HireDate).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            
            entity.HasData(
                new Role { Id = 1, Name = "Employee", Description = "Regular employee" },
                new Role { Id = 2, Name = "HROfficer", Description = "HR Officer" },
                new Role { Id = 3, Name = "CommitteeMember", Description = "Welfare Committee Member" },
                new Role { Id = 4, Name = "Management", Description = "Institutional Management" },
                new Role { Id = 5, Name = "Administrator", Description = "System Administrator" }
            );
        });

        // Configure Department entity
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Configure WelfareProgram entity
        modelBuilder.Entity<WelfareProgram>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.RequiredDocuments).HasColumnType("text");
        });

        // Configure EligibilityRule entity
        modelBuilder.Entity<EligibilityRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RuleValue).HasColumnType("text");
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.WelfareProgram)
                .WithMany(p => p.EligibilityRules)
                .HasForeignKey(e => e.WelfareProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WelfareApplication entity
        modelBuilder.Entity<WelfareApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.WelfareProgramId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmittedAt);
            entity.Property(e => e.RequestedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.AnomalyFlags).HasColumnType("text");
            entity.Property(e => e.RuleEvaluationResult).HasColumnType("text");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Applications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.WelfareProgram)
                .WithMany(p => p.Applications)
                .HasForeignKey(e => e.WelfareProgramId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ApplicationDocument entity
        modelBuilder.Entity<ApplicationDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Documents)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApplicationReview entity
        modelBuilder.Entity<ApplicationReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReviewerRole).HasMaxLength(50);
            entity.Property(e => e.Recommendation).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Comments).HasMaxLength(2000);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Reviews)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Reviewer)
                .WithMany()
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ApplicationStatusHistory entity
        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.StatusHistory)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EntityType);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.Details).HasColumnType("text");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProviderKey).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleAccessRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestedRole).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Anomaly entity
        modelBuilder.Entity<Anomaly>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AnomalyType);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DetectedAt);
            entity.HasIndex(e => e.ApplicationId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.AnomalyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).IsRequired().HasColumnType("text");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DetectionDetails).HasColumnType("text");
            entity.Property(e => e.ResolutionNotes).HasColumnType("text");

            entity.HasOne(e => e.Application)
                .WithMany()
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Resolver)
                .WithMany()
                .HasForeignKey(e => e.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure AnomalyRule entity
        modelBuilder.Entity<AnomalyRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AnomalyType);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AnomalyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ThresholdConfig).HasColumnType("text");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Updater)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure AnomalyAlert entity
        modelBuilder.Entity<AnomalyAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AnomalyId);
            entity.HasIndex(e => e.AlertType);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.AlertType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Recipient).HasMaxLength(500);
            entity.Property(e => e.AlertContent).HasColumnType("text");
            entity.Property(e => e.ErrorMessage).HasColumnType("text");

            entity.HasOne(e => e.Anomaly)
                .WithMany(a => a.Alerts)
                .HasForeignKey(e => e.AnomalyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
