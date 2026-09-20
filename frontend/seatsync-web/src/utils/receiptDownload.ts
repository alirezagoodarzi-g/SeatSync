import api from '../services/api'
import type { Booking } from '../types'

/**
 * Booking confirmation is async (RabbitMQ consumer creates the row).
 * Poll briefly for the confirmed booking before fetching its receipt.
 */
export async function waitForConfirmedBooking(
  bookingRequestId: string,
  maxAttempts = 15,
  intervalMs = 1000
): Promise<Booking> {
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    try {
      const { data } = await api.get<Booking>(`/bookings/by-request/${bookingRequestId}`)
      if (data.status === 'Confirmed') return data
    } catch {
      // 404 until the consumer creates the row — expected, keep polling.
    }
    await new Promise((resolve) => setTimeout(resolve, intervalMs))
  }
  throw new Error('Booking confirmation timed out')
}

export async function downloadReceipt(bookingId: string): Promise<void> {
  const response = await api.get(`/bookings/${bookingId}/receipt`, {
    responseType: 'blob',
  })

  const blob = new Blob([response.data], { type: 'application/pdf' })
  const url = window.URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = url
  link.download = 'bilit-seatsync.pdf'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(url)
}