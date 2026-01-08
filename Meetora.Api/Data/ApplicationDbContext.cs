using Microsoft.EntityFrameworkCore;
using Meetora.Api.Domain.Entities;

namespace Meetora.Api.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<MeetingProposal> MeetingProposals => Set<MeetingProposal>();
    public DbSet<MeetingTimeOption> MeetingTimeOptions => Set<MeetingTimeOption>();
    public DbSet<TimeOptionVote> TimeOptionVotes => Set<TimeOptionVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.GoogleSubject).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.PictureUrl).HasMaxLength(2048);

            entity.HasIndex(x => x.GoogleSubject).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.LastLoginAt).IsRequired();
        });

        modelBuilder.Entity<MeetingProposal>(entity =>
        {
            entity.ToTable("meeting_proposals");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Status).IsRequired();

            entity.Property(x => x.PublicTokenHash).IsRequired();
            entity.Property(x => x.PublicTokenProtected).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.PublicTokenCreatedAt).IsRequired();

            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasIndex(x => x.OrganizerUserId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.PublicTokenHash).IsUnique();

            entity.HasOne(x => x.OrganizerUser)
                .WithMany(x => x.MeetingProposals)
                .HasForeignKey(x => x.OrganizerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MeetingTimeOption>(entity =>
        {
            entity.ToTable("meeting_time_options");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.StartAt).IsRequired();
            entity.Property(x => x.EndAt);
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => x.MeetingProposalId);
            entity.HasIndex(x => new { x.MeetingProposalId, x.StartAt, x.EndAt }).IsUnique();

            entity.HasOne(x => x.MeetingProposal)
                .WithMany(x => x.TimeOptions)
                .HasForeignKey(x => x.MeetingProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimeOptionVote>(entity =>
        {
            entity.ToTable("time_option_votes");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.VoterName).HasMaxLength(200);
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => x.MeetingProposalId);
            entity.HasIndex(x => x.TimeOptionId);
            entity.HasIndex(x => new { x.MeetingProposalId, x.VoterId });
            entity.HasIndex(x => new { x.MeetingProposalId, x.VoterId, x.TimeOptionId }).IsUnique();

            entity.HasOne(x => x.MeetingProposal)
                .WithMany()
                .HasForeignKey(x => x.MeetingProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.TimeOption)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.TimeOptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case AppUser user when entry.State == EntityState.Added:
                    user.CreatedAt = now;
                    user.LastLoginAt = now;
                    break;
                case MeetingProposal proposal:
                    if (entry.State == EntityState.Added)
                    {
                        proposal.CreatedAt = now;
                        proposal.PublicTokenCreatedAt = now;
                    }

                    proposal.UpdatedAt = now;
                    break;
                case MeetingTimeOption option when entry.State == EntityState.Added:
                    option.CreatedAt = now;
                    break;
                case TimeOptionVote vote when entry.State == EntityState.Added:
                    vote.CreatedAt = now;
                    break;
            }
        }
    }
}
