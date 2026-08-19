import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./api";
import type {
  AnalyticsOverview,
  Booking,
  BookingPage,
  BookingStatus,
  CreateServiceRequest,
  Master,
  ReviewPage,
  ScheduleEntry,
  Service,
  TimeOff,
  WorkPhoto,
} from "../types";

// ── Брони (персонал) ─────────────────────────────────────────────────────────

export interface BookingListParams {
  from?: string;
  to?: string;
  masterId?: string;
  status?: BookingStatus;
  page?: number;
  pageSize?: number;
}

export function useStaffBookings(params: BookingListParams) {
  return useQuery({
    queryKey: ["staff-bookings", params],
    queryFn: async () => {
      const { data } = await api.get<BookingPage>("/bookings", { params });
      return data;
    },
  });
}

export function useUpdateBookingStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: BookingStatus }) => {
      const { data } = await api.patch<Booking>(`/bookings/${id}/status`, { status });
      return data;
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["staff-bookings"] });
    },
  });
}

// ── Расписание / time-off ────────────────────────────────────────────────────

export function useSetSchedule() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ masterId, entries }: { masterId: string; entries: ScheduleEntry[] }) => {
      await api.put(`/masters/${masterId}/schedule`, { entries });
    },
    onSuccess: (_d, v) => {
      void qc.invalidateQueries({ queryKey: ["master-schedule", v.masterId] });
    },
  });
}

export function useTimeOff(masterId: string | undefined) {
  return useQuery({
    queryKey: ["time-off", masterId],
    enabled: !!masterId,
    queryFn: async () => {
      const { data } = await api.get<TimeOff[]>(`/masters/${masterId}/time-off`);
      return data;
    },
  });
}

export function useAddTimeOff() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      masterId,
      startAt,
      endAt,
      reason,
    }: {
      masterId: string;
      startAt: string;
      endAt: string;
      reason?: string;
    }) => {
      const { data } = await api.post<TimeOff>(`/masters/${masterId}/time-off`, {
        startAt,
        endAt,
        reason,
      });
      return data;
    },
    onSuccess: (_d, v) => {
      void qc.invalidateQueries({ queryKey: ["time-off", v.masterId] });
    },
  });
}

export function useRemoveTimeOff() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ masterId, timeOffId }: { masterId: string; timeOffId: string }) => {
      await api.delete(`/masters/${masterId}/time-off/${timeOffId}`);
    },
    onSuccess: (_d, v) => {
      void qc.invalidateQueries({ queryKey: ["time-off", v.masterId] });
    },
  });
}

// ── Портфолио работ (мастер/админ) ───────────────────────────────────────────

export function useUploadWork() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ masterId, file }: { masterId: string; file: File }) => {
      const form = new FormData();
      form.append("file", file);
      // Content-Type (multipart с boundary) axios выставит сам.
      const { data } = await api.post<WorkPhoto>(`/masters/${masterId}/works`, form);
      return data;
    },
    onSuccess: (_d, v) => {
      void qc.invalidateQueries({ queryKey: ["master-works", v.masterId] });
    },
  });
}

export function useDeleteWork() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ masterId, photoId }: { masterId: string; photoId: string }) => {
      await api.delete(`/masters/${masterId}/works/${photoId}`);
    },
    onSuccess: (_d, v) => {
      void qc.invalidateQueries({ queryKey: ["master-works", v.masterId] });
    },
  });
}

// ── Услуги (admin) ───────────────────────────────────────────────────────────

export function useCreateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (req: CreateServiceRequest) => {
      const { data } = await api.post<Service>("/services", req);
      return data;
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["services"] }),
  });
}

export function useUpdateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...req }: { id: string } & CreateServiceRequest & { isActive: boolean }) => {
      const { data } = await api.put<Service>(`/services/${id}`, req);
      return data;
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["services"] }),
  });
}

// ── Мастера (admin) ──────────────────────────────────────────────────────────

export interface CreateMasterBody {
  name: string;
  bio?: string | null;
  photoUrl?: string | null;
  serviceIds?: string[];
  loginEmail?: string | null;
  loginPassword?: string | null;
}
export interface UpdateMasterBody {
  name: string;
  bio?: string | null;
  photoUrl?: string | null;
  isActive: boolean;
  serviceIds?: string[];
  loginEmail?: string | null;
  loginPassword?: string | null;
}

export function useCreateMaster() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (req: CreateMasterBody) => {
      const { data } = await api.post<Master>("/masters", req);
      return data;
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["masters"] }),
  });
}

export function useUpdateMaster() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...req }: { id: string } & UpdateMasterBody) => {
      const { data } = await api.put<Master>(`/masters/${id}`, req);
      return data;
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["masters"] }),
  });
}

/** Загрузка фото профиля мастера файлом (multipart). Возвращает обновлённый профиль. */
export function useUploadMasterPhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ masterId, file }: { masterId: string; file: File }) => {
      const form = new FormData();
      form.append("file", file);
      const { data } = await api.post<Master>(`/masters/${masterId}/photo`, form);
      return data;
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["masters"] }),
  });
}

// ── Отзывы (admin) ───────────────────────────────────────────────────────────

export function useReviewsForModeration(isPublished: boolean | undefined, page = 1) {
  return useQuery({
    queryKey: ["moderation-reviews", { isPublished, page }],
    queryFn: async () => {
      const { data } = await api.get<ReviewPage>("/reviews", {
        params: { isPublished, page, pageSize: 50 },
      });
      return data;
    },
  });
}

export function useModerateReview() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, isPublished }: { id: string; isPublished: boolean }) => {
      await api.patch(`/reviews/${id}/moderate`, { isPublished });
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["moderation-reviews"] }),
  });
}

// ── Аналитика (admin) ────────────────────────────────────────────────────────

export function useAnalytics(from?: string, to?: string) {
  return useQuery({
    queryKey: ["analytics", { from, to }],
    queryFn: async () => {
      const { data } = await api.get<AnalyticsOverview>("/analytics/overview", {
        params: { from, to },
      });
      return data;
    },
  });
}
