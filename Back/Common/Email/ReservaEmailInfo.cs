using Reservas.Models.Entities;

namespace Reservas.Common.Email;

/// <summary>
/// Datos mínimos para construir un correo de reserva. Se pasan ya formateados
/// como hora local del negocio para no reinterpretar zonas en el correo.
/// </summary>
public sealed record ReservaEmailInfo(
    string Folio,
    string NombreCliente,
    string NombreEspacio,
    string NombreServicio,
    string FechaHoraInicio,
    string FechaHoraFin,
    decimal Precio,
    EstadoReserva Estado);
