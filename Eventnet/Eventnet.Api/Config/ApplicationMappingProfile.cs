﻿using AutoMapper;
using Eventnet.Api.Models;
using Eventnet.Api.Models.Authentication;
using Eventnet.Api.Models.Events;
using Eventnet.Api.Models.Tags;
using Eventnet.DataAccess.Entities;
using Eventnet.Domain.Events;
using Eventnet.Domain.Selectors;

namespace Eventnet.Api.Config;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateEventsMap();
        CreateLocationMap();
        CreateTagsMap();
        CreateMap<CreateEventModel, Event>();
        CreateProjection<UserEntity, UserNameModel>();
        CreateMap<UserEntity, UserViewModel>();
        CreateMap<RegisterModel, UserEntity>()
            .ForSourceMember(x => x.Password,
                opt => opt.DoNotValidate());
    }

    private void CreateEventsMap()
    {
        CreateMap<EventEntity, Event>();
        CreateMap<EventEntity, EventName>();
        CreateMap<Event, EventLocationViewModel>();
        CreateMap<Event, EventEntity>();
        CreateMap<EventName, EventNameViewModel>();
    }

    private void CreateTagsMap()
    {
        CreateMap<TagEntity, Tag>();
        CreateProjection<TagEntity, TagName>();
        CreateMap<TagName, TagNameViewModel>();
        CreateMap<TagEntity, TagNameViewModel>();
    }

    private void CreateLocationMap()
    {
        CreateMap<LocationEntity, Location>();
        CreateMap<Location, LocationEntity>();
        CreateMap<LocationEntity, LocationViewModel>();
        CreateMap<Location, LocationViewModel>();
    }
}