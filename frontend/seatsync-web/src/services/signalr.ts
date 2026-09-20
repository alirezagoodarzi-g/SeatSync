import * as signalR from '@microsoft/signalr'

let connection: signalR.HubConnection | null = null

const SIGNALR_URL = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5141/hubs/seatmap'

function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(SIGNALR_URL)
      .withAutomaticReconnect()
      .build()
  }
  return connection
}

export async function ensureConnected(): Promise<signalR.HubConnection> {
  const conn = getConnection()
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start()
  }
  return conn
}

export async function joinEventGroup(eventId: string) {
  const conn = await ensureConnected()
  await conn.invoke('JoinEventGroup', eventId)
}

export async function leaveEventGroup(eventId: string) {
  const conn = getConnection()
  if (conn.state === signalR.HubConnectionState.Connected) {
    await conn.invoke('LeaveEventGroup', eventId)
  }
}

export function onSeatStatusChanged(callback: (seatId: string, status: string) => void) {
  getConnection().on('SeatStatusChanged', callback)
}

export function offSeatStatusChanged(callback: (seatId: string, status: string) => void) {
  getConnection().off('SeatStatusChanged', callback)
}