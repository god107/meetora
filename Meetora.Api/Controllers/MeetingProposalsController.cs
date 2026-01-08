using Meetora.Api.Auth;
using Meetora.Api.Contracts.Proposals;
using Meetora.Api.Data;
using Meetora.Api.Domain.Entities;
using Meetora.Api.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meetora.Api.Controllers;

[ApiController]
[Authorize]
[Route("proposals")]
public sealed class MeetingProposalsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPublicTokenService _tokenService;

    public MeetingProposalsController(ApplicationDbContext db, ICurrentUserAccessor currentUser, IPublicTokenService tokenService)
    {
        _db = db;
        _currentUser = currentUser;
        _tokenService = tokenService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListMeetingProposalsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListMeetingProposalsResponse>> List(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var items = await _db.MeetingProposals
            .Where(p => p.OrganizerUserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ListMeetingProposalsResponse.Item
            {
                Id = p.Id,
                Title = p.Title,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ClosedAt = p.ClosedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new ListMeetingProposalsResponse { Items = items });
    }

    [HttpPost]
    [ProducesResponseType(typeof(MeetingProposalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeetingProposalResponse>> Create([FromBody] CreateMeetingProposalRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length is < 1 or > 200)
        {
            return BadRequest(new { error = "invalid_title" });
        }

        var description = request.Description?.Trim();
        if (description is not null && description.Length > 4000)
        {
            return BadRequest(new { error = "description_too_long" });
        }

        if (request.TimeOptions is null || request.TimeOptions.Count is < 1 or > 20)
        {
            return BadRequest(new { error = "invalid_time_options_count" });
        }

        var normalizedOptions = new List<(DateTimeOffset start, DateTimeOffset? end)>();
        foreach (var opt in request.TimeOptions)
        {
            if (opt.EndAt is not null && opt.EndAt <= opt.StartAt)
            {
                return BadRequest(new { error = "invalid_time_option_range" });
            }

            normalizedOptions.Add((opt.StartAt.ToUniversalTime(), opt.EndAt?.ToUniversalTime()));
        }

        if (normalizedOptions.Distinct().Count() != normalizedOptions.Count)
        {
            return BadRequest(new { error = "duplicate_time_options" });
        }

        // Generate a non-guessable token and store only an HMAC hash.
        var token = _tokenService.GenerateToken();
        var tokenHash = _tokenService.ComputeHash(token);
        var tokenProtected = _tokenService.ProtectToken(token);

        var proposal = new MeetingProposal
        {
            Id = Guid.NewGuid(),
            OrganizerUserId = userId,
            Title = title,
            Description = description,
            Status = MeetingProposalStatus.Open,
            PublicTokenHash = tokenHash,
            PublicTokenProtected = tokenProtected,
            // timestamps handled by DbContext audit
            TimeOptions = normalizedOptions.Select(x => new MeetingTimeOption
            {
                Id = Guid.NewGuid(),
                StartAt = x.start,
                EndAt = x.end,
            }).ToList(),
        };

        _db.MeetingProposals.Add(proposal);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Extremely unlikely token collision; client can retry.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "token_generation_failed" });
        }

        return Created($"/proposals/{proposal.Id}", await BuildProposalResponse(proposal.Id, token, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MeetingProposalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingProposalResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var proposal = await _db.MeetingProposals
            .AsNoTracking()
            .Where(p => p.Id == id && p.OrganizerUserId == userId)
            .Select(p => new
            {
                Proposal = p,
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

        string publicToken;
        try
        {
            publicToken = _tokenService.UnprotectToken(proposal.Proposal.PublicTokenProtected);
        }
        catch
        {
            // If token protection settings changed, allow read but don't leak anything.
            publicToken = "";
        }

        return Ok(new MeetingProposalResponse
        {
            Id = proposal.Proposal.Id,
            Title = proposal.Proposal.Title,
            Description = proposal.Proposal.Description,
            Status = proposal.Proposal.Status,
            CreatedAt = proposal.Proposal.CreatedAt,
            UpdatedAt = proposal.Proposal.UpdatedAt,
            ClosedAt = proposal.Proposal.ClosedAt,
            PublicToken = publicToken,
            TimeOptions = proposal.Options
                .OrderBy(o => o.StartAt)
                .Select(o => new MeetingProposalResponse.TimeOptionDto
                {
                    Id = o.Id,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    VoteCount = o.VoteCount,
                })
                .ToList(),
        });
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var proposal = await _db.MeetingProposals
            .Where(p => p.Id == id && p.OrganizerUserId == userId)
            .SingleOrDefaultAsync(cancellationToken);

        if (proposal is null)
        {
            return NotFound(new { error = "not_found" });
        }

        if (proposal.Status != MeetingProposalStatus.Closed)
        {
            proposal.Status = MeetingProposalStatus.Closed;
            proposal.ClosedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/votes")]
    [ProducesResponseType(typeof(MeetingProposalVotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingProposalVotesResponse>> GetVotes(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var exists = await _db.MeetingProposals
            .AsNoTracking()
            .AnyAsync(p => p.Id == id && p.OrganizerUserId == userId, cancellationToken);

        if (!exists)
        {
            return NotFound(new { error = "not_found" });
        }

        var grouped = await _db.TimeOptionVotes
            .AsNoTracking()
            .Where(v => v.MeetingProposalId == id)
            .GroupBy(v => new { v.VoterId, v.VoterName })
            .Select(g => new MeetingProposalVotesResponse.Voter
            {
                VoterId = g.Key.VoterId,
                VoterName = g.Key.VoterName,
                TimeOptionIds = g.Select(x => x.TimeOptionId).ToList(),
            })
            .OrderBy(v => v.VoterName)
            .ThenBy(v => v.VoterId)
            .ToListAsync(cancellationToken);

        return Ok(new MeetingProposalVotesResponse
        {
            ProposalId = id,
            Voters = grouped,
        });
    }

    private async Task<MeetingProposalResponse> BuildProposalResponse(Guid proposalId, string publicToken, CancellationToken cancellationToken)
    {
        var proposal = await _db.MeetingProposals
            .AsNoTracking()
            .Include(p => p.TimeOptions)
            .SingleAsync(p => p.Id == proposalId, cancellationToken);

        return new MeetingProposalResponse
        {
            Id = proposal.Id,
            Title = proposal.Title,
            Description = proposal.Description,
            Status = proposal.Status,
            CreatedAt = proposal.CreatedAt,
            UpdatedAt = proposal.UpdatedAt,
            ClosedAt = proposal.ClosedAt,
            PublicToken = publicToken,
            TimeOptions = proposal.TimeOptions
                .OrderBy(o => o.StartAt)
                .Select(o => new MeetingProposalResponse.TimeOptionDto
                {
                    Id = o.Id,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    VoteCount = 0,
                })
                .ToList(),
        };
    }
}
