// Доменные типы, отражающие контракты backend (см. src/Barberos.Application/**/*Dtos.cs).
// ВАЖНО: enum'ы сериализуются числами (JsonStringEnumConverter не подключён),
// свойства — camelCase (дефолт ASP.NET Core). Даты — ISO 8601 UTC (строки).
// Клиенты НЕ являются пользователями — бронь гостевая (имя + телефон).

/** Роль сотрудника. Domain/Enums/UserRole. */
export const UserRole = {
  Master: 1,
  Admin: 2,
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

/** Статус брони. Domain/Enums/BookingStatus. Гостевая бронь создаётся как Confirmed. */
export const BookingStatus = {
  Confirmed: 1,
  Completed: 2,
  Cancelled: 3,
  NoShow: 4,
} as const;
export type BookingStatus = (typeof BookingStatus)[keyof typeof BookingStatus];

/** Сотрудник барбершопа (мастер/админ). Вход по email + паролю. AuthUserDto. */
export interface AuthUser {
  id: string;
  email: string;
  name?: string | null;
  role: UserRole;
}

/** Ответ входа: access-токен в теле, refresh — в httpOnly cookie. */
export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

/** ServiceDto. */
export interface Service {
  id: string;
  name: string;
  description?: string | null;
  durationMinutes: number;
  bufferMinutes: number;
  price: number;
  isActive: boolean;
}

/** MasterDto. */
export interface Master {
  id: string;
  name: string;
  bio?: string | null;
  photoUrl?: string | null;
  isActive: boolean;
  userId?: string | null;
  serviceIds: string[];
}

/** SlotDto — StartAt/EndAt в UTC (ISO 8601 с Z). */
export interface Slot {
  startAt: string;
  endAt: string;
}

/** AvailabilityDto. */
export interface Availability {
  date: string; // DateOnly YYYY-MM-DD
  slots: Slot[];
}

/** CreateBookingRequest. */
export interface CreateBookingRequest {
  guestName: string;
  guestPhone: string;
  masterId: string;
  serviceId: string;
  startAt: string; // UTC ISO
}

/** CreateBookingResult. */
export interface CreateBookingResult {
  id: string;
  manageToken: string;
}

/** BookingManageDto — представление для клиента (без телефона гостя). */
export interface BookingManage {
  id: string;
  guestName: string;
  masterName: string;
  serviceName: string;
  startAt: string;
  endAt: string;
  price: number;
  status: BookingStatus;
}

/** BookingDto — представление для персонала (с контактами гостя). */
export interface Booking {
  id: string;
  guestName: string;
  guestPhone: string;
  masterId: string;
  masterName: string;
  serviceId: string;
  serviceName: string;
  startAt: string;
  endAt: string;
  status: BookingStatus;
  createdAt: string;
}

/** BookingPageDto. */
export interface BookingPage {
  items: Booking[];
  page: number;
  pageSize: number;
  total: number;
}

/** CreateServiceRequest / поля обновления услуги (admin). */
export interface CreateServiceRequest {
  name: string;
  description?: string | null;
  durationMinutes: number;
  bufferMinutes: number;
  price: number;
}

/** ScheduleEntryDto — время локальное (зона барбершопа), HH:mm:ss. */
export interface ScheduleEntry {
  dayOfWeek: number; // 0=Sunday .. 6=Saturday (System.DayOfWeek)
  startTime: string; // "09:00:00"
  endTime: string;
}

/** TimeOffDto — время в UTC. */
export interface TimeOff {
  id: string;
  startAt: string;
  endAt: string;
  reason?: string | null;
}

/** ReviewDto — публичный опубликованный отзыв. */
export interface Review {
  id: string;
  masterId: string;
  guestName: string;
  rating: number;
  comment?: string | null;
  createdAt: string;
}

/** MasterRatingDto. */
export interface MasterRating {
  masterId: string;
  average?: number | null;
  count: number;
}

/** MasterReviewsDto — публичная лента отзывов мастера. */
export interface MasterReviews {
  rating: MasterRating;
  items: Review[];
}

/** ReviewModerationDto — для админ-модерации. */
export interface ReviewModeration {
  id: string;
  bookingId: string;
  masterId: string;
  masterName: string;
  guestName: string;
  rating: number;
  comment?: string | null;
  isPublished: boolean;
  createdAt: string;
}

/** ReviewPageDto. */
export interface ReviewPage {
  items: ReviewModeration[];
  page: number;
  pageSize: number;
  total: number;
}

// ── Аналитика (AnalyticsOverviewDto) ─────────────────────────────────────────
export interface StatusCount {
  status: BookingStatus;
  count: number;
}
export interface MasterLoad {
  masterId: string;
  masterName: string;
  bookings: number;
  busyMinutes: number;
}
export interface ServicePopularity {
  serviceId: string;
  serviceName: string;
  bookings: number;
}
export interface AnalyticsOverview {
  from?: string | null;
  to?: string | null;
  totalBookings: number;
  byStatus: StatusCount[];
  masterLoad: MasterLoad[];
  popularServices: ServicePopularity[];
}

/** WorkPhotoDto — фото работы мастера (портфолио). */
export interface WorkPhoto {
  id: string;
  url: string;
  sortOrder: number;
}
