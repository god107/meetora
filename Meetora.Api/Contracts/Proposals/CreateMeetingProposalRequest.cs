namespace Meetora.Api.Contracts.Proposals;

public sealed class CreateMeetingProposalRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }

    public required List<TimeOptionInput> TimeOptions { get; init; }

    public sealed class TimeOptionInput
    {
        public required DateTimeOffset StartAt { get; init; }
        public DateTimeOffset? EndAt { get; init; }
    }
}
