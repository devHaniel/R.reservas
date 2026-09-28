// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { API_BASE_URL } from './apiClient'

export async function connectReservationNotifications(onUpdated: () => void) {
  const connection = new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/notificaciones`, { withCredentials: false })
    .withAutomaticReconnect()
    .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
    .build()

  connection.on('ReservaActualizado', onUpdated)
  await connection.start()

  return async () => {
    connection.off('ReservaActualizado', onUpdated)
    await connection.stop()
  }
}
