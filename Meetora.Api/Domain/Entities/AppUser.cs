namespace Meetora.Api.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; }

    // Google "sub" claim (stable, unique per Google account)
    public string GoogleSubject { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? PictureUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastLoginAt { get; set; }

    public List<MeetingProposal> MeetingProposals { get; set; } = new();
}
