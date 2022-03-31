﻿using System.ComponentModel.DataAnnotations;

namespace Eventnet.Api.Models.Authentication;

public record RestorePasswordModel(
    [DataType(DataType.Password)] string OldPassword,
    [DataType(DataType.Password)] string NewPassword);