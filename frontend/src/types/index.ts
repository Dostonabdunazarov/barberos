// Доменные типы, отражающие модель backend.

export type UserRole = "Client" | "Master" | "Admin";

export type BookingStatus =
  | "Pending"
  | "Confirmed"
  | "Completed"
  | "Cancelled"
  | "NoShow";

export interface User {
  id: string;
  phone: string;
  name?: string;
  role: UserRole;
}

export interface Service {
  id: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export interface Master {
  id: string;
  name: string;
  bio?: string;
  photoUrl?: string;
  isActive: boolean;
}

export interface TimeSlot {
  startAt: string; // ISO UTC
  endAt: string;
}

export interface Booking {
  id: string;
  masterId: string;
  serviceId: string;
  startAt: string;
  endAt: string;
  status: BookingStatus;
}

export interface Review {
  id: string;
  masterId: string;
  rating: number;
  comment?: string;
  createdAt: string;
}
