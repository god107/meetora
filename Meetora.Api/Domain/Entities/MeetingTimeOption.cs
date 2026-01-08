namespace Meetora.Api.Domain.Entities;

public sealed class MeetingTimeOption
{
    public Guid Id { get; set; }

    public Guid MeetingProposalId { get; set; }
    public MeetingProposal MeetingProposal { get; set; } = null!;

    // Stored as UTC timestamptz (use DateTimeOffset)
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<TimeOptionVote> Votes { get; set; } = new();
}
