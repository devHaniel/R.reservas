using System;

namespace Reservas.Services.Interfaces;

public interface IRealtimeNotificacionService
{
    Task NotificarAsync(string evento);
}
