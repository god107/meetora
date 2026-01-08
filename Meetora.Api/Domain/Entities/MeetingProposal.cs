namespace Meetora.Api.Domain.Entities;

public sealed class MeetingProposal
{
    public Guid Id { get; set; }

    public Guid OrganizerUserId { get; set; }
    public AppUser OrganizerUser { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public MeetingProposalStatus Status { get; set; }

    // Public voting link token (store hash only)
    public byte[] PublicTokenHash { get; set; } = null!;
    // Encrypted token (Data Protection) for organizer retrieval
    public string PublicTokenProtected { get; set; } = null!;
    public DateTimeOffset PublicTokenCreatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public List<MeetingTimeOption> TimeOptions { get; set; } = new();
}
