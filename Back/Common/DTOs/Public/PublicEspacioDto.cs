namespace Reservas.Common.DTOs.Public;

public class PublicEspacioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string HorarioApertura { get; set; } = string.Empty;
    public string HorarioCierre { get; set; } = string.Empty;
    public decimal PrecioDesde { get; set; }
    public List<PublicOpcionDto> Opciones { get; set; } = [];
}
