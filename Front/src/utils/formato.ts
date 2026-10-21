// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

/**
 * Formato de moneda configurable. Por defecto se usa MXN y locale es-MX para
 * conservar el comportamiento anterior, pero cualquier instalación puede
 * cambiarlo desde .env con VITE_CURRENCY / VITE_LOCALE.
 */
const currency = (import.meta.env.VITE_CURRENCY || 'MXN').toUpperCase()
const locale = import.meta.env.VITE_LOCALE || 'es-MX'

const formatter = new Intl.NumberFormat(locale, {
  style: 'currency',
  currency,
  minimumFractionDigits: 2,
})

export function formatearMoneda(valor: number): string {
  return formatter.format(Number.isFinite(valor) ? valor : 0)
}
