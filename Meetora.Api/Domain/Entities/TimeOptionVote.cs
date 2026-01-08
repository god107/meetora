namespace Meetora.Api.Domain.Entities;

public sealed class TimeOptionVote
{
    public Guid Id { get; set; }

    public Guid MeetingProposalId { get; set; }
    public MeetingProposal MeetingProposal { get; set; } = null!;

    public Guid TimeOptionId { get; set; }
    public MeetingTimeOption TimeOption { get; set; } = null!;

    // Anonymous voter identifier (client-generated GUID; no login required)
    public Guid VoterId { get; set; }

    public string? VoterName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
