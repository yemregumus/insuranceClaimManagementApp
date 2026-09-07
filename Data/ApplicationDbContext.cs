using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Data
{
    // ApplicationDbContext class is responsible for interacting with the database
    // It inherits from DbContext, which is the base class for Entity Framework's database interaction
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ICurrentOrganization _currentOrganization;

        // Constructor to initialize the context with options (e.g., database connection string, configurations)
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentOrganization currentOrganization) : base(options)
        {
            _currentOrganization = currentOrganization;
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<ClaimStatusHistory> ClaimStatusHistories { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }

        // Customer records remain in the existing 'Users' database table.
        public DbSet<User> Customers { get; set; }

        // DbSet for Claims - represents the 'Claims' table in the database
        public DbSet<Claim> Claims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("Organizations");
                entity.Property(organization => organization.Name).HasMaxLength(200).IsRequired();
                entity.Property(organization => organization.Slug).HasMaxLength(100).IsRequired();
                entity.HasIndex(organization => organization.Slug).IsUnique();
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(staff => staff.DisplayName).HasMaxLength(200).IsRequired();
                entity.HasAlternateKey(staff => new { staff.OrganizationId, staff.Id });
                entity.HasOne(staff => staff.Organization)
                    .WithMany(organization => organization.StaffMembers)
                    .HasForeignKey(staff => staff.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(customer => customer.Name).HasMaxLength(255).IsRequired();
                entity.Property(customer => customer.Email).HasMaxLength(255).IsRequired();
                entity.Property(customer => customer.Username).HasMaxLength(255).IsRequired();
                entity.HasAlternateKey(customer => new { customer.OrganizationId, customer.Id });
                entity.HasIndex(customer => new { customer.OrganizationId, customer.Email })
                    .IsUnique()
                    .HasDatabaseName("IX_Users_OrganizationId_Email");
                entity.HasIndex(customer => new { customer.OrganizationId, customer.Username })
                    .IsUnique()
                    .HasDatabaseName("IX_Users_OrganizationId_Username");
                entity.HasOne(customer => customer.Organization)
                    .WithMany(organization => organization.Customers)
                    .HasForeignKey(customer => customer.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasQueryFilter(customer =>
                    _currentOrganization.OrganizationId.HasValue &&
                    customer.OrganizationId == _currentOrganization.OrganizationId.Value);
            });

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.ToTable("Claims");
                entity.Property(claim => claim.PolicyNumber).HasMaxLength(255).IsRequired();
                entity.Property(claim => claim.ClaimDate).HasColumnType("date");
                entity.Property(claim => claim.ConcurrencyToken)
                    .HasMaxLength(36)
                    .IsRequired()
                    .IsConcurrencyToken();
                entity.Property(claim => claim.DeletedByUserId).HasMaxLength(255);
                entity.HasAlternateKey(claim => new { claim.OrganizationId, claim.Id });
                entity.HasOne(claim => claim.Organization)
                    .WithMany(organization => organization.Claims)
                    .HasForeignKey(claim => claim.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(claim => claim.User)
                    .WithMany(customer => customer.Claims)
                    .HasForeignKey(claim => new { claim.OrganizationId, claim.UserId })
                    .HasPrincipalKey(customer => new { customer.OrganizationId, customer.Id })
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasQueryFilter(claim =>
                    _currentOrganization.OrganizationId.HasValue &&
                    claim.OrganizationId == _currentOrganization.OrganizationId.Value &&
                    !claim.IsDeleted);
            });

            modelBuilder.Entity<ClaimStatusHistory>(entity =>
            {
                entity.ToTable("ClaimStatusHistory");
                entity.Property(history => history.PreviousStatus).HasMaxLength(50);
                entity.Property(history => history.NewStatus).HasMaxLength(50).IsRequired();
                entity.Property(history => history.ChangedByUserId).HasMaxLength(255).IsRequired();
                entity.HasIndex(history => new { history.OrganizationId, history.ClaimId, history.ChangedUtc });
                entity.HasOne(history => history.Organization)
                    .WithMany(organization => organization.ClaimStatusHistory)
                    .HasForeignKey(history => history.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(history => history.Claim)
                    .WithMany(claim => claim.StatusHistory)
                    .HasForeignKey(history => new { history.OrganizationId, history.ClaimId })
                    .HasPrincipalKey(claim => new { claim.OrganizationId, claim.Id })
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(history => history.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(history => new { history.OrganizationId, history.ChangedByUserId })
                    .HasPrincipalKey(staff => new { staff.OrganizationId, staff.Id })
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasQueryFilter(history =>
                    _currentOrganization.OrganizationId.HasValue &&
                    history.OrganizationId == _currentOrganization.OrganizationId.Value);
            });

            modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.ToTable("AuditEvents");
                entity.Property(audit => audit.ActorUserId).HasMaxLength(255).IsRequired();
                entity.Property(audit => audit.EventType).HasMaxLength(100).IsRequired();
                entity.Property(audit => audit.EntityType).HasMaxLength(100).IsRequired();
                entity.Property(audit => audit.EntityId).HasMaxLength(100).IsRequired();
                entity.HasIndex(audit => new { audit.OrganizationId, audit.OccurredUtc });
                entity.HasOne(audit => audit.Organization)
                    .WithMany(organization => organization.AuditEvents)
                    .HasForeignKey(audit => audit.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(audit => audit.ActorUser)
                    .WithMany()
                    .HasForeignKey(audit => new { audit.OrganizationId, audit.ActorUserId })
                    .HasPrincipalKey(staff => new { staff.OrganizationId, staff.Id })
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasQueryFilter(audit =>
                    _currentOrganization.OrganizationId.HasValue &&
                    audit.OrganizationId == _currentOrganization.OrganizationId.Value);
            });
        }
    }
}
