// Доменные типы, отражающие модель backend.
// Клиенты НЕ являются пользователями — бронь гостевая (имя + телефон).

export type UserRole = "Master" | "Admin";

export type BookingStatus =
  | "Pending"
  | "Confirmed"
  | "Completed"
  | "Cancelled"
  | "NoShow";

/** Сотрудник барбершопа (мастер/админ). Вход по email + паролю. */
export interface User {
  id: string;
  email: string;
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
  guestName: string;
  guestPhone: string;
  manageToken?: string; // возвращается только при создании
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
