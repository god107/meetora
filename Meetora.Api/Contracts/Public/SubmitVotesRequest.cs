namespace Meetora.Api.Contracts.Public;

public sealed class SubmitVotesRequest
{
    public Guid? VoterId { get; init; }
    public string? VoterName { get; init; }

    public required List<Guid> TimeOptionIds { get; init; }
}
