export type Role = 'Customer' | 'Organizer'

export interface User {
  id: string
  email: string
  displayName: string
  role: Role
}

export interface AuthResponse {
  token: string
}

export interface Seat {
  id: string
  row: string
  number: number
  price: number
  status: 'Available' | 'Held' | 'Booked'
}

export interface EventDetail {
  id: string
  name: string
  venue: string
  dateTime: string // ISO 8601 UTC string from the API
  organizerId: string
  seats: Seat[]
}

export interface EventSummary {
  id: string
  name: string
  venue: string
  dateTime: string
  totalSeats: number
}

export interface HoldResponse {
  seatId: string
  heldUntil: string
}

export interface ConfirmBookingResponse {
  bookingRequestId: string
  status: string
}

export interface Booking {
  id: string
  eventId: string
  seatIds: string[]
  status: 'Pending' | 'Confirmed' | 'Cancelled'
  createdAt: string
  confirmedAt: string | null
}