/**
 * All dates from the API are UTC ISO 8601 strings (matches the backend's
 * DateTime/timestamptz). These utilities convert to Jalali calendar,
 * Iran local time, Persian digits — for display only. Never store or
 * send dates in this format back to the API.
 */

const dateFormatter = new Intl.DateTimeFormat('fa-IR', {
  calendar: 'persian',
  timeZone: 'Asia/Tehran',
  year: 'numeric',
  month: 'long',
  day: 'numeric',
})

const dateTimeFormatter = new Intl.DateTimeFormat('fa-IR', {
  calendar: 'persian',
  timeZone: 'Asia/Tehran',
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const timeFormatter = new Intl.DateTimeFormat('fa-IR', {
  timeZone: 'Asia/Tehran',
  hour: '2-digit',
  minute: '2-digit',
})

export function formatJalaliDate(isoString: string): string {
  return dateFormatter.format(new Date(isoString))
}

export function formatJalaliDateTime(isoString: string): string {
  return dateTimeFormatter.format(new Date(isoString))
}

export function formatTime(isoString: string): string {
  return timeFormatter.format(new Date(isoString))
}

export function formatPrice(amount: number): string {
  return amount.toLocaleString('fa-IR') + ' تومان'
}
/**
 * A <input type="datetime-local"> gives a plain "YYYY-MM-DDTHH:mm" string
 * with no timezone info — we deliberately interpret that as Iran wall-clock
 * time (never the browser's own timezone, which could be anything) and
 * convert it to a correct UTC instant before sending to the API.
 */
export function iranLocalToUtcIso(localDateTimeValue: string): string {
  const [datePart, timePart] = localDateTimeValue.split('T')
  const [year, month, day] = datePart.split('-').map(Number)
  const [hour, minute] = timePart.split(':').map(Number)

  const IRAN_OFFSET_MINUTES = 3 * 60 + 30
  const utcMillis = Date.UTC(year, month - 1, day, hour, minute) - IRAN_OFFSET_MINUTES * 60 * 1000

  return new Date(utcMillis).toISOString()
}