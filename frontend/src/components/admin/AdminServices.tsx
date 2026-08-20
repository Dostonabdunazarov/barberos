import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useServices } from "../../lib/hooks";
import { useCreateService, useUpdateService } from "../../lib/staffHooks";
import { Card } from "../ui/Card";
import { Button } from "../ui/Button";
import { Input, NumberInput, Field } from "../ui/Input";
import { Modal } from "../ui/Modal";
import { LoadingState, Spinner } from "../ui/misc";
import { apiErrorMessage } from "../../lib/api";
import { useLocale } from "../../i18n/useLocale";
import { formatPrice } from "../../lib/utils";
import type { Service } from "../../types";

interface FormState {
  name: string;
  description: string;
  /** null = поле очищено пользователем; проверяется при отправке. */
  durationMinutes: number | null;
  bufferMinutes: number | null;
  price: number | null;
  isActive: boolean;
}

const MAX_MINUTES = 24 * 60; // совпадает с ServiceValidators на бэкенде

const empty: FormState = {
  name: "",
  description: "",
  durationMinutes: 30,
  bufferMinutes: 0,
  price: 0,
  isActive: true,
};

export function AdminServices() {
  const { t } = useTranslation();
  const locale = useLocale();
  const { data, isLoading } = useServices(true);
  const create = useCreateService();
  const update = useUpdateService();

  const [editing, setEditing] = useState<Service | null>(null);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<FormState>(empty);

  function openCreate() {
    setEditing(null);
    setForm(empty);
    setOpen(true);
  }
  function openEdit(s: Service) {
    setEditing(s);
    setForm({
      name: s.name,
      description: s.description ?? "",
      durationMinutes: s.durationMinutes,
      bufferMinutes: s.bufferMinutes,
      price: s.price,
      isActive: s.isActive,
    });
    setOpen(true);
  }

  const mutation = editing ? update : create;

  // Пустые числовые поля не отправляем на сервер — браузерная валидация
  // required у type="text" их не поймает.
  const { durationMinutes, bufferMinutes, price } = form;
  const numbersFilled = durationMinutes !== null && bufferMinutes !== null && price !== null;

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (durationMinutes === null || bufferMinutes === null || price === null) return;
    const body = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      durationMinutes,
      bufferMinutes,
      price,
    };
    if (editing) {
      update.mutate(
        { id: editing.id, ...body, isActive: form.isActive },
        { onSuccess: () => setOpen(false) },
      );
    } else {
      create.mutate(body, { onSuccess: () => setOpen(false) });
    }
  }

  return (
    <div>
      <div className="mb-5 flex items-center justify-between">
        <h2 className="font-display text-2xl text-fg">{t("admin.servicesTitle")}</h2>
        <Button onClick={openCreate}>{t("admin.addService")}</Button>
      </div>

      {isLoading ? (
        <LoadingState />
      ) : (
        <div className="space-y-3">
          {data?.map((s) => (
            <Card key={s.id} className="flex items-center justify-between p-4">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium text-fg">{s.name}</span>
                  {!s.isActive && (
                    <span className="rounded bg-ink-700 px-2 py-0.5 text-xs text-fg-subtle">
                      {t("common.no")}
                    </span>
                  )}
                </div>
                <span className="text-sm text-fg-subtle">
                  {t("services.duration", { count: s.durationMinutes })} · {formatPrice(s.price, locale)}
                </span>
              </div>
              <Button size="sm" variant="secondary" onClick={() => openEdit(s)}>
                {t("common.edit")}
              </Button>
            </Card>
          ))}
        </div>
      )}

      <Modal open={open} onClose={() => setOpen(false)} title={editing ? t("admin.editService") : t("admin.addService")}>
        <form onSubmit={submit} className="space-y-4">
          <Field label={t("admin.name")}>
            <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          </Field>
          <Field label={t("admin.description")}>
            <Input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </Field>
          <div className="grid grid-cols-2 gap-4">
            <Field label={t("admin.durationMinutes")}>
              <NumberInput
                min={1}
                max={MAX_MINUTES}
                value={form.durationMinutes}
                onChange={(v) => setForm({ ...form, durationMinutes: v })}
              />
            </Field>
            <Field label={t("admin.bufferMinutes")}>
              <NumberInput
                min={0}
                max={MAX_MINUTES}
                value={form.bufferMinutes}
                onChange={(v) => setForm({ ...form, bufferMinutes: v })}
              />
            </Field>
          </div>
          <Field label={t("admin.price")}>
            <NumberInput
              min={0}
              value={form.price}
              onChange={(v) => setForm({ ...form, price: v })}
            />
          </Field>
          {editing && (
            <label className="flex items-center gap-2 text-sm text-fg">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                className="h-4 w-4 accent-accent-500"
              />
              {t("admin.active")}
            </label>
          )}

          {mutation.isError && <p className="text-sm text-red-400">{apiErrorMessage(mutation.error)}</p>}
          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={mutation.isPending || !numbersFilled}>
              {mutation.isPending && <Spinner className="h-4 w-4" />}
              {t("common.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
