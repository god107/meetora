using Meetora.Api.Domain.Entities;

namespace Meetora.Api.Contracts.Public;

public sealed class PublicPollResponse
{
    public required Guid ProposalId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required MeetingProposalStatus Status { get; init; }

    public required List<TimeOption> TimeOptions { get; init; }

    public sealed class TimeOption
    {
        public required Guid Id { get; init; }
        public required DateTimeOffset StartAt { get; init; }
        public DateTimeOffset? EndAt { get; init; }
        public required int VoteCount { get; init; }
        public required bool IsLeading { get; init; }
    }
}
