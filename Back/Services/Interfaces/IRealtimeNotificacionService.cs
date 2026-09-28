// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

using System;

namespace Reservas.Services.Interfaces;

public interface IRealtimeNotificacionService
{
    Task NotificarAsync(string evento);
}
