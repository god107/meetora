using Meetora.Api.Domain.Entities;

namespace Meetora.Api.Contracts.Proposals;

public sealed class ListMeetingProposalsResponse
{
    public required List<Item> Items { get; init; }

    public sealed class Item
    {
        public required Guid Id { get; init; }
        public required string Title { get; init; }
        public required MeetingProposalStatus Status { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset UpdatedAt { get; init; }
        public DateTimeOffset? ClosedAt { get; init; }
    }
}
