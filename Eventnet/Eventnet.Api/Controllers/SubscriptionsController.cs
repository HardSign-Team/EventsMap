﻿using Eventnet.Api.Models.Subscriptions;
using Eventnet.Api.Services;
using Eventnet.DataAccess;
using Eventnet.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eventnet.Api.Controllers;

[Route("api/subscriptions")]
public class SubscriptionsController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly CurrentUserService currentUserService;

    public SubscriptionsController(
        ApplicationDbContext dbContext,
        CurrentUserService currentUserService)
    {
        this.dbContext = dbContext;
        this.currentUserService = currentUserService;
    }

    [Authorize]
    [HttpPost("subscribe/{eventId:guid}")]
    public async Task<IActionResult> Subscribe(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return NotFound();

        var eventEntity = await dbContext.Events.FirstOrDefaultAsync(x => x.Id == eventId);
        if (eventEntity is null)
            return NotFound();

        if (eventEntity.EndDate.GetValueOrDefault(eventEntity.StartDate) < DateTime.Now)
            return Conflict("Event had been ended.");

        var user = await currentUserService.GetCurrentUser();
        if (user is null)
            return Unauthorized();

        dbContext.Subscriptions.RemoveRange(
            dbContext.Subscriptions.Where(x => x.EventId == eventId && x.UserId == user.Id));
        var subscription = new SubscriptionEntity(eventId, user.Id, DateTime.Now);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        return await GetSubscriptionsCount(eventId);
    }

    [Authorize]
    [HttpPost("unsubscribe/{eventId:guid}")]
    public async Task<IActionResult> UnSubscribe(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return NotFound();

        var eventEntity = await dbContext.Events.FirstOrDefaultAsync(x => x.Id == eventId);
        if (eventEntity is null)
            return NotFound();

        if (eventEntity.EndDate.GetValueOrDefault(eventEntity.StartDate) < DateTime.Now)
            return Conflict("Event had been ended.");

        var user = await currentUserService.GetCurrentUser();
        if (user is null)
            return Unauthorized();

        dbContext.Subscriptions.RemoveRange(
            dbContext.Subscriptions.Where(x => x.EventId == eventId && x.UserId == user.Id));
        await dbContext.SaveChangesAsync();

        return await GetSubscriptionsCount(eventId);
    }

    [HttpGet("count/{eventId:guid}")]
    public async Task<IActionResult> GetSubscriptionsCount(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return NotFound();

        var result = await dbContext.Events
            .Select(x => new
            {
                x.Id,
                SubscriptionsCount = x.Subscriptions.Count
            })
            .FirstOrDefaultAsync(x => x.Id == eventId);

        if (result is null)
            return NotFound();

        return Ok(new SubscriptionsCountViewModel(result.Id, result.SubscriptionsCount));
    }
}