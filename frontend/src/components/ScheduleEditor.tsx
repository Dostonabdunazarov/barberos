import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import type { ScheduleEntry, TimeOff } from "../types";
import { Card } from "./ui/Card";
import { Button } from "./ui/Button";
import { Input } from "./ui/Input";
import { Spinner } from "./ui/misc";
import { useMasterSchedule } from "../lib/hooks";
import {
  useSetSchedule,
  useTimeOff,
  useAddTimeOff,
  useRemoveTimeOff,
} from "../lib/staffHooks";
import { apiErrorMessage } from "../lib/api";
import { useLocale } from "../i18n/useLocale";
import { formatDateTime } from "../lib/utils";

// Порядок дней недели пн→вс (System.DayOfWeek: 0=вс).
const DAY_ORDER = [1, 2, 3, 4, 5, 6, 0];

/** "09:00:00" → "09:00" для input[type=time]. */
const toTimeInput = (t: string) => t.slice(0, 5);
/** "09:00" → "09:00:00". */
const toTimeSpan = (t: string) => (t.length === 5 ? `${t}:00` : t);

type DayState = Record<number, { enabled: boolean; start: string; end: string }>;

function buildInitial(entries: ScheduleEntry[]): DayState {
  const state: DayState = {};
  for (const d of DAY_ORDER) {
    const found = entries.find((e) => e.dayOfWeek === d);
    state[d] = found
      ? { enabled: true, start: toTimeInput(found.startTime), end: toTimeInput(found.endTime) }
      : { enabled: false, start: "09:00", end: "18:00" };
  }
  return state;
}

export function ScheduleEditor({ masterId }: { masterId: string }) {
  const { t } = useTranslation();
  const locale = useLocale();
  const schedule = useMasterSchedule(masterId);
  const setSchedule = useSetSchedule();
  const timeOff = useTimeOff(masterId);
  const addTimeOff = useAddTimeOff();
  const removeTimeOff = useRemoveTimeOff();

  const [days, setDays] = useState<DayState>({});

  useEffect(() => {
    if (schedule.data) setDays(buildInitial(schedule.data));
  }, [schedule.data]);

  const [toStart, setToStart] = useState("");
  const [toEnd, setToEnd] = useState("");
  const [toReason, setToReason] = useState("");

  function save() {
    const entries: ScheduleEntry[] = DAY_ORDER.filter((d) => days[d]?.enabled).map((d) => ({
      dayOfWeek: d,
      startTime: toTimeSpan(days[d].start),
      endTime: toTimeSpan(days[d].end),
    }));
    setSchedule.mutate({ masterId, entries });
  }

  function submitTimeOff(e: React.FormEvent) {
    e.preventDefault();
    if (!toStart || !toEnd) return;
    // datetime-local — локальное время; конвертируем в UTC ISO.
    addTimeOff.mutate(
      {
        masterId,
        startAt: new Date(toStart).toISOString(),
        endAt: new Date(toEnd).toISOString(),
        reason: toReason.trim() || undefined,
      },
      {
        onSuccess: () => {
          setToStart("");
          setToEnd("");
          setToReason("");
        },
      },
    );
  }

  return (
    <div className="space-y-8">
      {/* Недельное расписание */}
      <Card className="p-6">
        <h3 className="font-display text-xl text-fg">{t("dashboard.scheduleTitle")}</h3>
        {schedule.isLoading ? (
          <div className="py-6">
            <Spinner className="text-accent-500" />
          </div>
        ) : (
          <div className="mt-4 space-y-3">
            {DAY_ORDER.map((d) => {
              const day = days[d];
              if (!day) return null;
              return (
                <div key={d} className="flex flex-wrap items-center gap-3">
                  <label className="flex w-40 items-center gap-2">
                    <input
                      type="checkbox"
                      checked={day.enabled}
                      onChange={(e) =>
                        setDays((s) => ({ ...s, [d]: { ...s[d], enabled: e.target.checked } }))
                      }
                      className="h-4 w-4 accent-accent-500"
                    />
                    <span className="text-sm text-fg">{t(`dashboard.day.${d}`)}</span>
                  </label>
                  <Input
                    type="time"
                    value={day.start}
                    disabled={!day.enabled}
                    onChange={(e) => setDays((s) => ({ ...s, [d]: { ...s[d], start: e.target.value } }))}
                    className="w-32"
                  />
                  <span className="text-fg-subtle">—</span>
                  <Input
                    type="time"
                    value={day.end}
                    disabled={!day.enabled}
                    onChange={(e) => setDays((s) => ({ ...s, [d]: { ...s[d], end: e.target.value } }))}
                    className="w-32"
                  />
                </div>
              );
            })}
          </div>
        )}

        {setSchedule.isError && (
          <p className="mt-3 text-sm text-red-400">{apiErrorMessage(setSchedule.error)}</p>
        )}
        <Button className="mt-5" onClick={save} disabled={setSchedule.isPending}>
          {setSchedule.isPending && <Spinner className="h-4 w-4" />}
          {t("dashboard.saveSchedule")}
        </Button>
      </Card>

      {/* Time-off */}
      <Card className="p-6">
        <h3 className="font-display text-xl text-fg">{t("dashboard.timeOffTitle")}</h3>

        <form onSubmit={submitTimeOff} className="mt-4 flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs text-fg-subtle">{t("dashboard.from")}</label>
            <Input type="datetime-local" value={toStart} onChange={(e) => setToStart(e.target.value)} required className="w-52" />
          </div>
          <div>
            <label className="mb-1 block text-xs text-fg-subtle">{t("dashboard.to")}</label>
            <Input type="datetime-local" value={toEnd} onChange={(e) => setToEnd(e.target.value)} required className="w-52" />
          </div>
          <div className="flex-1">
            <label className="mb-1 block text-xs text-fg-subtle">{t("dashboard.reason")}</label>
            <Input value={toReason} onChange={(e) => setToReason(e.target.value)} />
          </div>
          <Button type="submit" disabled={addTimeOff.isPending}>
            {t("dashboard.addTimeOff")}
          </Button>
        </form>
        {addTimeOff.isError && (
          <p className="mt-2 text-sm text-red-400">{apiErrorMessage(addTimeOff.error)}</p>
        )}

        <div className="mt-5 space-y-2">
          {timeOff.data?.map((item: TimeOff) => (
            <div
              key={item.id}
              className="flex items-center justify-between rounded-lg bg-ink-800/50 px-4 py-2.5 text-sm"
            >
              <span className="text-fg">
                {formatDateTime(item.startAt, locale)} — {formatDateTime(item.endAt, locale)}
                {item.reason && <span className="text-fg-subtle"> · {item.reason}</span>}
              </span>
              <button
                onClick={() => removeTimeOff.mutate({ masterId, timeOffId: item.id })}
                className="text-fg-subtle transition-colors hover:text-red-400"
              >
                {t("dashboard.delete")}
              </button>
            </div>
          ))}
          {timeOff.data?.length === 0 && <p className="text-sm text-fg-subtle">—</p>}
        </div>
      </Card>
    </div>
  );
}
