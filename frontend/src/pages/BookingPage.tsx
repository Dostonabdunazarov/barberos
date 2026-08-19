import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useLocation } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { motion, AnimatePresence } from "framer-motion";
import { useServices, useMasters, useAvailability, useCreateBooking } from "../lib/hooks";
import { Card } from "../components/ui/Card";
import { Button } from "../components/ui/Button";
import { Input, Field } from "../components/ui/Input";
import { LoadingState, Spinner } from "../components/ui/misc";
import { apiErrorMessage } from "../lib/api";
import { useLocale } from "../i18n/useLocale";
import { formatPrice, formatTime, toShopDateString } from "../lib/utils";
import { BookingSuccess } from "../components/BookingSuccess";
import type { Master, Service } from "../types";

type Step = 0 | 1 | 2 | 3;

/** Следующие 14 дней (объекты Date, полдень чтобы избежать краёв TZ). */
function nextDays(count: number): Date[] {
  const today = new Date();
  return Array.from({ length: count }, (_, i) => {
    const d = new Date(today);
    d.setDate(today.getDate() + i);
    d.setHours(12, 0, 0, 0);
    return d;
  });
}

const detailsSchema = z.object({
  guestName: z.string().trim().min(2).max(100),
  guestPhone: z.string().trim().min(5).max(30),
});
type DetailsForm = z.infer<typeof detailsSchema>;

export function BookingPage() {
  const { t } = useTranslation();
  const locale = useLocale();
  const location = useLocation();
  const initial = (location.state ?? {}) as { serviceId?: string; masterId?: string };

  const services = useServices();
  const masters = useMasters();
  const createBooking = useCreateBooking();

  const [step, setStep] = useState<Step>(initial.serviceId ? 1 : 0);
  const [serviceId, setServiceId] = useState<string | undefined>(initial.serviceId);
  const [masterId, setMasterId] = useState<string | undefined>(initial.masterId);
  const [date, setDate] = useState<Date>(() => nextDays(1)[0]);
  const [startAt, setStartAt] = useState<string | undefined>();

  const days = useMemo(() => nextDays(14), []);
  const dateStr = toShopDateString(date);
  const availability = useAvailability(
    step === 2 ? masterId : undefined,
    step === 2 ? serviceId : undefined,
    step === 2 ? dateStr : undefined,
  );

  const service = services.data?.find((s) => s.id === serviceId);
  // Мастера, оказывающие выбранную услугу.
  const eligibleMasters =
    masters.data?.filter((m) => !serviceId || m.serviceIds.includes(serviceId)) ?? [];
  const master = masters.data?.find((m) => m.id === masterId);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<DetailsForm>({ resolver: zodResolver(detailsSchema) });

  if (createBooking.isSuccess && createBooking.data) {
    return (
      <BookingSuccess
        result={createBooking.data}
        service={service}
        master={master}
        startAt={startAt!}
      />
    );
  }

  function pickService(s: Service) {
    setServiceId(s.id);
    setMasterId(undefined);
    setStartAt(undefined);
    setStep(1);
  }
  function pickMaster(m: Master) {
    setMasterId(m.id);
    setStartAt(undefined);
    setStep(2);
  }
  function pickSlot(iso: string) {
    setStartAt(iso);
    setStep(3);
  }

  function submitDetails(values: DetailsForm) {
    if (!serviceId || !masterId || !startAt) return;
    createBooking.mutate({ ...values, serviceId, masterId, startAt });
  }

  const stepLabels = [
    t("booking.stepService"),
    t("booking.stepMaster"),
    t("booking.stepSlot"),
    t("booking.stepDetails"),
  ];

  return (
    <div className="mx-auto max-w-3xl">
      <h1 className="font-display text-3xl text-fg sm:text-4xl">{t("booking.title")}</h1>

      {/* Прогресс шагов */}
      <div className="mt-6 flex items-center gap-2">
        {stepLabels.map((label, i) => (
          <div key={i} className="flex flex-1 flex-col gap-1.5">
            <div
              className={`h-1 rounded-full transition-colors ${
                i <= step ? "bg-accent-500" : "bg-ink-700"
              }`}
            />
            <span
              className={`text-xs ${i === step ? "text-accent-400" : "text-fg-subtle"}`}
            >
              {label}
            </span>
          </div>
        ))}
      </div>

      <AnimatePresence mode="wait">
        <motion.div
          key={step}
          initial={{ opacity: 0, x: 16 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -16 }}
          transition={{ duration: 0.2 }}
          className="mt-8"
        >
          {/* Шаг 0: услуга */}
          {step === 0 && (
            <div>
              <h2 className="mb-4 text-lg text-fg-muted">{t("booking.chooseService")}</h2>
              {services.isLoading ? (
                <LoadingState />
              ) : (
                <div className="grid gap-4 sm:grid-cols-2">
                  {services.data?.map((s) => (
                    <button key={s.id} onClick={() => pickService(s)} className="text-left">
                      <Card className="p-5 transition-colors hover:border-accent-500/40">
                        <div className="flex items-center justify-between">
                          <h3 className="font-display text-lg text-fg">{s.name}</h3>
                          <span className="text-accent-400">{formatPrice(s.price, locale)}</span>
                        </div>
                        <p className="mt-1 text-sm text-fg-subtle">
                          {t("services.duration", { count: s.durationMinutes })}
                        </p>
                      </Card>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Шаг 1: мастер */}
          {step === 1 && (
            <div>
              <h2 className="mb-4 text-lg text-fg-muted">{t("booking.chooseMaster")}</h2>
              {masters.isLoading ? (
                <LoadingState />
              ) : (
                <div className="grid gap-4 sm:grid-cols-2">
                  {eligibleMasters.map((m) => (
                    <button key={m.id} onClick={() => pickMaster(m)} className="text-left">
                      <Card className="flex items-center gap-4 p-4 transition-colors hover:border-accent-500/40">
                        <div className="h-14 w-14 shrink-0 overflow-hidden rounded-full bg-ink-800">
                          {m.photoUrl ? (
                            <img src={m.photoUrl} alt={m.name} className="h-full w-full object-cover" />
                          ) : (
                            <div className="flex h-full w-full items-center justify-center font-display text-xl text-ink-600">
                              {m.name.charAt(0)}
                            </div>
                          )}
                        </div>
                        <span className="font-display text-lg text-fg">{m.name}</span>
                      </Card>
                    </button>
                  ))}
                  {eligibleMasters.length === 0 && (
                    <p className="text-fg-subtle">{t("masters.empty")}</p>
                  )}
                </div>
              )}
              <Button variant="ghost" className="mt-6" onClick={() => setStep(0)}>
                ← {t("booking.back")}
              </Button>
            </div>
          )}

          {/* Шаг 2: дата + слот */}
          {step === 2 && (
            <div>
              <h2 className="mb-4 text-lg text-fg-muted">{t("booking.chooseSlot")}</h2>

              <div className="mb-6 flex gap-2 overflow-x-auto pb-2">
                {days.map((d) => {
                  const active = toShopDateString(d) === dateStr;
                  return (
                    <button
                      key={d.toISOString()}
                      onClick={() => {
                        setDate(d);
                        setStartAt(undefined);
                      }}
                      className={`shrink-0 rounded-xl border px-3 py-2 text-center text-sm transition-colors ${
                        active
                          ? "border-accent-500 bg-accent-500/15 text-accent-300"
                          : "border-white/10 text-fg-muted hover:border-white/20"
                      }`}
                    >
                      <div className="font-medium">
                        {new Intl.DateTimeFormat(locale, { day: "numeric", month: "short" }).format(d)}
                      </div>
                      <div className="text-xs opacity-70">
                        {new Intl.DateTimeFormat(locale, { weekday: "short" }).format(d)}
                      </div>
                    </button>
                  );
                })}
              </div>

              {availability.isLoading ? (
                <div className="flex items-center gap-2 py-8 text-fg-muted">
                  <Spinner className="text-accent-500" /> {t("booking.loadingSlots")}
                </div>
              ) : availability.data && availability.data.slots.length > 0 ? (
                <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">
                  {availability.data.slots.map((slot) => (
                    <button
                      key={slot.startAt}
                      onClick={() => pickSlot(slot.startAt)}
                      className="rounded-lg border border-white/10 py-2.5 text-sm text-fg transition-colors hover:border-accent-500 hover:bg-accent-500/10"
                    >
                      {formatTime(slot.startAt, locale)}
                    </button>
                  ))}
                </div>
              ) : (
                <p className="py-8 text-fg-subtle">{t("booking.noSlots")}</p>
              )}

              <Button variant="ghost" className="mt-6" onClick={() => setStep(1)}>
                ← {t("booking.back")}
              </Button>
            </div>
          )}

          {/* Шаг 3: данные */}
          {step === 3 && (
            <div className="max-w-md">
              <h2 className="mb-4 text-lg text-fg-muted">{t("booking.stepDetails")}</h2>

              {/* Сводка */}
              <Card className="mb-6 space-y-1 p-4 text-sm">
                <div className="flex justify-between">
                  <span className="text-fg-subtle">{t("booking.stepService")}</span>
                  <span className="text-fg">{service?.name}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-fg-subtle">{t("booking.stepMaster")}</span>
                  <span className="text-fg">{master?.name}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-fg-subtle">{t("booking.stepSlot")}</span>
                  <span className="text-fg">
                    {startAt &&
                      `${new Intl.DateTimeFormat(locale, { day: "numeric", month: "short" }).format(
                        new Date(startAt),
                      )}, ${formatTime(startAt, locale)}`}
                  </span>
                </div>
                {service && (
                  <div className="flex justify-between border-t border-white/5 pt-1">
                    <span className="text-fg-subtle">{t("manage.price")}</span>
                    <span className="text-accent-400">{formatPrice(service.price, locale)}</span>
                  </div>
                )}
              </Card>

              <form onSubmit={handleSubmit(submitDetails)} className="space-y-4">
                <Field label={t("booking.name")} error={errors.guestName && t("common.required")}>
                  <Input placeholder={t("booking.namePlaceholder")} {...register("guestName")} />
                </Field>
                <Field label={t("booking.phone")} error={errors.guestPhone && t("common.required")}>
                  <Input
                    type="tel"
                    placeholder={t("booking.phonePlaceholder")}
                    {...register("guestPhone")}
                  />
                </Field>

                {createBooking.isError && (
                  <p className="text-sm text-red-400">{apiErrorMessage(createBooking.error)}</p>
                )}

                <div className="flex gap-3 pt-2">
                  <Button type="button" variant="ghost" onClick={() => setStep(2)}>
                    ← {t("booking.back")}
                  </Button>
                  <Button type="submit" disabled={createBooking.isPending} className="flex-1">
                    {createBooking.isPending && <Spinner className="h-4 w-4" />}
                    {t("booking.confirm")}
                  </Button>
                </div>
              </form>
            </div>
          )}
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
