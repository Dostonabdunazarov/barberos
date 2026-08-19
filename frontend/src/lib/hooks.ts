import { useMutation, useQuery } from "@tanstack/react-query";
import { api } from "./api";
import type {
  Availability,
  BookingManage,
  CreateBookingRequest,
  CreateBookingResult,
  Master,
  MasterReviews,
  WorkPhoto,
  Review,
  Service,
  ScheduleEntry,
} from "../types";

// ── Публичные справочники ────────────────────────────────────────────────────

export function useServices(includeInactive = false) {
  return useQuery({
    queryKey: ["services", { includeInactive }],
    queryFn: async () => {
      const { data } = await api.get<Service[]>("/services", {
        params: includeInactive ? { includeInactive: true } : undefined,
      });
      return data;
    },
  });
}

export function useMasters(includeInactive = false) {
  return useQuery({
    queryKey: ["masters", { includeInactive }],
    queryFn: async () => {
      const { data } = await api.get<Master[]>("/masters", {
        params: includeInactive ? { includeInactive: true } : undefined,
      });
      return data;
    },
  });
}

export function useMaster(id: string | undefined) {
  return useQuery({
    queryKey: ["master", id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await api.get<Master>(`/masters/${id}`);
      return data;
    },
  });
}

export function useMasterReviews(id: string | undefined) {
  return useQuery({
    queryKey: ["master-reviews", id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await api.get<MasterReviews>(`/masters/${id}/reviews`);
      return data;
    },
  });
}

export function useMasterWorks(id: string | undefined) {
  return useQuery({
    queryKey: ["master-works", id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await api.get<WorkPhoto[]>(`/masters/${id}/works`);
      return data;
    },
  });
}

export function useMasterSchedule(id: string | undefined) {
  return useQuery({
    queryKey: ["master-schedule", id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await api.get<ScheduleEntry[]>(`/masters/${id}/schedule`);
      return data;
    },
  });
}

// ── Брони (публичные) ────────────────────────────────────────────────────────

export function useCreateBooking() {
  return useMutation({
    mutationFn: async (req: CreateBookingRequest) => {
      const { data } = await api.post<CreateBookingResult>("/bookings", req);
      return data;
    },
  });
}

export function useManageBooking(token: string | undefined) {
  return useQuery({
    queryKey: ["booking-manage", token],
    enabled: !!token,
    retry: false,
    queryFn: async () => {
      const { data } = await api.get<BookingManage>(`/bookings/manage/${token}`);
      return data;
    },
  });
}

export function useCreateReview(token: string | undefined) {
  return useMutation({
    mutationFn: async (req: { rating: number; comment?: string }) => {
      const { data } = await api.post<Review>(`/reviews/manage/${token}`, req);
      return data;
    },
  });
}

export function useAvailability(
  masterId: string | undefined,
  serviceId: string | undefined,
  date: string | undefined,
) {
  return useQuery({
    queryKey: ["availability", masterId, serviceId, date],
    enabled: !!masterId && !!serviceId && !!date,
    queryFn: async () => {
      const { data } = await api.get<Availability>("/availability", {
        params: { masterId, serviceId, date },
      });
      return data;
    },
  });
}
