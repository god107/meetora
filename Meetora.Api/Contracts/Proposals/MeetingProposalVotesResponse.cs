namespace Meetora.Api.Contracts.Proposals;

public sealed class MeetingProposalVotesResponse
{
    public required Guid ProposalId { get; init; }
    public required List<Voter> Voters { get; init; }

    public sealed class Voter
    {
        public required Guid VoterId { get; init; }
        public string? VoterName { get; init; }
        public required List<Guid> TimeOptionIds { get; init; }
    }
}
