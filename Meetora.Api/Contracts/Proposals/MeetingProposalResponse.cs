using Meetora.Api.Domain.Entities;

namespace Meetora.Api.Contracts.Proposals;

public sealed class MeetingProposalResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required MeetingProposalStatus Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }

    public required string PublicToken { get; init; }

    public required List<TimeOptionDto> TimeOptions { get; init; }

    public sealed class TimeOptionDto
    {
        public required Guid Id { get; init; }
        public required DateTimeOffset StartAt { get; init; }
        public DateTimeOffset? EndAt { get; init; }
        public required int VoteCount { get; init; }
    }
}
