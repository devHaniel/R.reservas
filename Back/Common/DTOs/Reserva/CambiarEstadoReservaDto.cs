using System.ComponentModel.DataAnnotations;
using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Reserva;

public class CambiarEstadoReservaDto
{
    [EnumDataType(typeof(EstadoReserva))]
    public EstadoReserva Estado { get; set; }
}