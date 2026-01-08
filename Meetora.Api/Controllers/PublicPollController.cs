using Meetora.Api.Contracts.Public;
using Meetora.Api.Data;
using Meetora.Api.Domain.Entities;
using Meetora.Api.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meetora.Api.Controllers;

[ApiController]
[Route("public/polls")]
public sealed class PublicPollController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPublicTokenService _tokenService;

    public PublicPollController(ApplicationDbContext db, IPublicTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpGet("{token}")]
    [ProducesResponseType(typeof(PublicPollResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicPollResponse>> Get(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NotFound(new { error = "not_found" });
        }

        var tokenHash = _tokenService.ComputeHash(token);

        var proposal = await _db.MeetingProposals
            .AsNoTracking()
            .Where(p => p.PublicTokenHash == tokenHash)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.Status,
                Options = p.TimeOptions.Select(o => new
                {
                    o.Id,
                    o.StartAt,
                    o.EndAt,
                    VoteCount = _db.TimeOptionVotes.Count(v => v.TimeOptionId == o.Id)
                }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (proposal is null)
        {
            return NotFound(new { error = "not_found" });
        }

        var maxVotes = proposal.Options.Count == 0 ? 0 : proposal.Options.Max(o => o.VoteCount);

        return Ok(new PublicPollResponse
        {
            ProposalId = proposal.Id,
            Title = proposal.Title,
            Description = proposal.Description,
            Status = proposal.Status,
            TimeOptions = proposal.Options
                .OrderBy(o => o.StartAt)
                .Select(o => new PublicPollResponse.TimeOption
                {
                    Id = o.Id,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    VoteCount = o.VoteCount,
                    IsLeading = maxVotes > 0 && o.VoteCount == maxVotes,
                })
                .ToList(),
        });
    }

    [HttpPost("{token}/votes")]
    [ProducesResponseType(typeof(SubmitVotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitVotesResponse>> SubmitVotes(string token, [FromBody] SubmitVotesRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NotFound(new { error = "not_found" });
        }

        if (request.TimeOptionIds is null)
        {
            return BadRequest(new { error = "invalid_time_option_ids" });
        }

        if (request.TimeOptionIds.Count > 20)
        {
            return BadRequest(new { error = "too_many_votes" });
        }

        if (request.TimeOptionIds.Distinct().Count() != request.TimeOptionIds.Count)
        {
            return BadRequest(new { error = "duplicate_time_option_ids" });
        }

        var voterName = request.VoterName?.Trim();
        if (voterName is not null && voterName.Length > 200)
        {
            return BadRequest(new { error = "voter_name_too_long" });
        }

        var voterId = request.VoterId ?? Guid.NewGuid();
        if (voterId == Guid.Empty)
        {
            voterId = Guid.NewGuid();
        }

        var tokenHash = _tokenService.ComputeHash(token);

        var proposal = await _db.MeetingProposals
            .AsNoTracking()
            .Where(p => p.PublicTokenHash == tokenHash)
            .Select(p => new
            {
                p.Id,
                p.Status,
                OptionIds = p.TimeOptions.Select(o => o.Id).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (proposal is null)
        {
            return NotFound(new { error = "not_found" });
        }

        if (proposal.Status != MeetingProposalStatus.Open)
        {
            return Conflict(new { error = "poll_closed" });
        }

        // Validate the submitted option ids belong to the proposal.
        var optionIdSet = proposal.OptionIds.ToHashSet();
        if (request.TimeOptionIds.Any(id => !optionIdSet.Contains(id)))
        {
            return BadRequest(new { error = "invalid_time_option_ids" });
        }

        // Upsert pattern: delete existing votes for this voter+proposal, then insert new votes.
        await _db.TimeOptionVotes
            .Where(v => v.MeetingProposalId == proposal.Id && v.VoterId == voterId)
            .ExecuteDeleteAsync(cancellationToken);

        if (request.TimeOptionIds.Count > 0)
        {
            var votes = request.TimeOptionIds.Select(optionId => new TimeOptionVote
            {
                Id = Guid.NewGuid(),
                MeetingProposalId = proposal.Id,
                TimeOptionId = optionId,
                VoterId = voterId,
                VoterName = voterName,
            });

            _db.TimeOptionVotes.AddRange(votes);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new SubmitVotesResponse { VoterId = voterId });
    }
}
