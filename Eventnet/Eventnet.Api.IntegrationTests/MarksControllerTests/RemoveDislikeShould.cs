﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Eventnet.Api.IntegrationTests.Helpers;
using Eventnet.Api.Models.Marks;
using FluentAssertions;
using NUnit.Framework;

namespace Eventnet.Api.IntegrationTests.MarksControllerTests;

public class RemoveDislikeShould : MarksApiTestBase
{
        [Test]
    public async Task Response400_WhenNotAuthorized()
    {
        var entity = ApplyToDb(context =>
        {
            context.AddUsers();
            context.AddEvents(context.Users);
            return context.Events.First();
        });
        var request = BuildRemoveDislikeRequest(entity.Id);

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Test]
    public async Task Response404_WhenEventNotFound()
    {
        var request = BuildRemoveDislikeRequest(Guid.Empty);

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Response200_WhenRemoveSingleTime()
    {
        var (_, client) = await CreateAuthorizedClient("TestUser", "TestPassword");
        var entity = ApplyToDb(context =>
        {
            context.AddUsers();
            context.AddEvents(context.Users);
            return context.Events.First();
        });
        var request = BuildRemoveDislikeRequest(entity.Id);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var viewModel = response.ReadContentAs<MarksCountViewModel>();
        viewModel.Likes.Should().Be(0);
        viewModel.Dislikes.Should().Be(0);
    }
    
    [TestCase(2)]
    [TestCase(10)]
    public async Task Response200_WhenRemoveSeveralTimes(int n)
    {
        var (_, client) = await CreateAuthorizedClient("TestUser", "TestPassword");
        var entity = ApplyToDb(context =>
        {
            context.AddUsers();
            context.AddEvents(context.Users);
            return context.Events.First();
        });

        for (var i = 0; i < n; i++)
        {
            var request = BuildRemoveDislikeRequest(entity.Id);

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var viewModel = response.ReadContentAs<MarksCountViewModel>();
            viewModel.Likes.Should().Be(0);
            viewModel.Dislikes.Should().Be(0);
        }
    }
    
    private HttpRequestMessage BuildRemoveDislikeRequest(Guid eventId) => new(HttpMethod.Post, BuildRemoveDislikeUri(eventId));
}