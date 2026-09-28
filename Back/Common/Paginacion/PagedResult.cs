// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

using System;

namespace Reservas.Common.Paginacion;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int Pagina { get; set; }
    public int Cantidad { get; set; }
    public int Total { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)Total / Cantidad);
}
