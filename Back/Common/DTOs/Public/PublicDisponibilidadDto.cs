namespace Reservas.Common.DTOs.Public;

public class PublicHorarioDto
{
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
}

public class PublicDisponibilidadDto
{
    public int OpcionId { get; set; }
    public string NombreOpcion { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public List<PublicHorarioDto> Horarios { get; set; } = [];
}
