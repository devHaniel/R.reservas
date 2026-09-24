using System;
using Microsoft.AspNetCore.SignalR;
using Reservas.Hubs;

namespace Reservas.Services.Interfaces;

public class RealtimeNotificacionService : IRealtimeNotificacionService
{
    private readonly IHubContext<NotificacionHub> _hubContext;

    public RealtimeNotificacionService(
        IHubContext<NotificacionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotificarAsync(string evento)
    {
        await _hubContext.Clients.All.SendAsync(evento);
    }
}
