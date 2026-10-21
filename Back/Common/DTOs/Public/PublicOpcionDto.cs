namespace Reservas.Common.DTOs.Public;

public class PublicOpcionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
}
