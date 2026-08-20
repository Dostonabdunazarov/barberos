import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMasters, useServices } from "../../lib/hooks";
import { useCreateMaster, useUpdateMaster, useUploadMasterPhoto } from "../../lib/staffHooks";
import { Card } from "../ui/Card";
import { Button } from "../ui/Button";
import { Input, Textarea, Field } from "../ui/Input";
import { Modal } from "../ui/Modal";
import { LoadingState, Spinner } from "../ui/misc";
import { apiErrorMessage } from "../../lib/api";
import type { Master } from "../../types";

interface FormState {
  name: string;
  bio: string;
  photoUrl: string;
  publicPhone: string;
  isActive: boolean;
  serviceIds: string[];
  loginEmail: string;
  loginPassword: string;
}

const empty: FormState = {
  name: "",
  bio: "",
  photoUrl: "",
  publicPhone: "",
  isActive: true,
  serviceIds: [],
  loginEmail: "",
  loginPassword: "",
};

export function AdminMasters() {
  const { t } = useTranslation();
  const { data, isLoading } = useMasters(true);
  const services = useServices(true);
  const create = useCreateMaster();
  const update = useUpdateMaster();
  const uploadPhoto = useUploadMasterPhoto();

  const [editing, setEditing] = useState<Master | null>(null);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<FormState>(empty);
  const photoInputRef = useRef<HTMLInputElement>(null);

  function onPickPhoto(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ""; // сброс — повторный выбор того же файла тоже сработает
    if (!file || !editing) return;
    uploadPhoto.mutate(
      { masterId: editing.id, file },
      {
        onSuccess: (m) => {
          setForm((f) => ({ ...f, photoUrl: m.photoUrl ?? "" }));
          setEditing(m);
        },
      },
    );
  }

  function openCreate() {
    setEditing(null);
    setForm(empty);
    setOpen(true);
  }
  function openEdit(m: Master) {
    setEditing(m);
    setForm({
      name: m.name,
      bio: m.bio ?? "",
      photoUrl: m.photoUrl ?? "",
      publicPhone: m.publicPhone ?? "",
      isActive: m.isActive,
      serviceIds: m.serviceIds,
      loginEmail: "",
      loginPassword: "",
    });
    setOpen(true);
  }

  function toggleService(id: string) {
    setForm((f) => ({
      ...f,
      serviceIds: f.serviceIds.includes(id)
        ? f.serviceIds.filter((x) => x !== id)
        : [...f.serviceIds, id],
    }));
  }

  const mutation = editing ? update : create;

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const common = {
      name: form.name.trim(),
      bio: form.bio.trim() || null,
      photoUrl: form.photoUrl.trim() || null,
      publicPhone: form.publicPhone.trim() || null,
      serviceIds: form.serviceIds,
    };
    // Поля учётки шлём и при создании, и при редактировании — пустые не трогают учётку.
    const account = {
      loginEmail: form.loginEmail.trim() || null,
      loginPassword: form.loginPassword || null,
    };
    if (editing) {
      update.mutate(
        { id: editing.id, ...common, isActive: form.isActive, ...account },
        { onSuccess: () => setOpen(false) },
      );
    } else {
      create.mutate({ ...common, ...account }, { onSuccess: () => setOpen(false) });
    }
  }

  const hasAccount = !!editing?.userId;

  return (
    <div>
      <div className="mb-5 flex items-center justify-between">
        <h2 className="font-display text-2xl text-fg">{t("admin.mastersTitle")}</h2>
        <Button onClick={openCreate}>{t("admin.addMaster")}</Button>
      </div>

      {isLoading ? (
        <LoadingState />
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {data?.map((m) => (
            <Card key={m.id} className="flex items-center gap-3 p-4">
              <div className="h-12 w-12 shrink-0 overflow-hidden rounded-full bg-ink-800">
                {m.photoUrl ? (
                  <img src={m.photoUrl} alt={m.name} className="h-full w-full object-cover" />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-lg text-ink-600">
                    {m.name.charAt(0)}
                  </div>
                )}
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="truncate font-medium text-fg">{m.name}</span>
                  {!m.isActive && (
                    <span className="rounded bg-ink-700 px-1.5 py-0.5 text-xs text-fg-subtle">
                      {t("common.no")}
                    </span>
                  )}
                </div>
                <span className="text-xs text-fg-subtle">
                  {m.serviceIds.length} {t("nav.services").toLowerCase()}
                </span>
              </div>
              <Button size="sm" variant="secondary" onClick={() => openEdit(m)}>
                {t("common.edit")}
              </Button>
            </Card>
          ))}
        </div>
      )}

      <Modal open={open} onClose={() => setOpen(false)} title={editing ? t("admin.editMaster") : t("admin.addMaster")}>
        <form onSubmit={submit} className="space-y-4">
          <Field label={t("admin.name")}>
            <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          </Field>
          <Field label={t("admin.bio")}>
            <Textarea value={form.bio} onChange={(e) => setForm({ ...form, bio: e.target.value })} />
          </Field>
          <Field label={t("admin.publicPhone")}>
            <Input
              type="tel"
              inputMode="tel"
              autoComplete="off"
              placeholder="+998 90 123-45-67"
              value={form.publicPhone}
              onChange={(e) => setForm({ ...form, publicPhone: e.target.value })}
            />
            <p className="mt-1 text-xs text-fg-subtle">{t("admin.publicPhoneHint")}</p>
          </Field>
          <Field label={t("admin.photo")}>
            <div className="flex items-center gap-4">
              <div className="h-16 w-16 shrink-0 overflow-hidden rounded-full bg-ink-800">
                {form.photoUrl ? (
                  <img src={form.photoUrl} alt="" className="h-full w-full object-cover" />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-xl text-ink-600">
                    {form.name.charAt(0) || "?"}
                  </div>
                )}
              </div>
              <div className="flex flex-col gap-1">
                <input
                  ref={photoInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  className="hidden"
                  onChange={onPickPhoto}
                />
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={!editing || uploadPhoto.isPending}
                  onClick={() => photoInputRef.current?.click()}
                >
                  {uploadPhoto.isPending && <Spinner className="h-4 w-4" />}
                  {uploadPhoto.isPending ? t("works.uploading") : t("admin.uploadPhoto")}
                </Button>
                {!editing && (
                  <span className="text-xs text-fg-subtle">{t("admin.photoAfterSave")}</span>
                )}
                {uploadPhoto.isError && (
                  <span className="text-xs text-red-400">{apiErrorMessage(uploadPhoto.error)}</span>
                )}
              </div>
            </div>
          </Field>

          <Field label={t("admin.servicesOf")}>
            <div className="flex flex-wrap gap-2">
              {services.data?.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => toggleService(s.id)}
                  className={`rounded-lg border px-3 py-1.5 text-sm transition-colors ${
                    form.serviceIds.includes(s.id)
                      ? "border-accent-500 bg-accent-500/15 text-accent-300"
                      : "border-white/10 text-fg-muted hover:border-white/20"
                  }`}
                >
                  {s.name}
                </button>
              ))}
            </div>
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

          {/* Учётная запись мастера (логин/пароль) — доступна и при создании, и при редактировании */}
          <div className="space-y-3 rounded-xl border border-white/5 p-4">
            <p className="text-sm font-medium text-fg-muted">{t("admin.account")}</p>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label={hasAccount ? t("admin.changeEmail") : t("admin.loginEmail")}>
                <Input
                  type="email"
                  autoComplete="off"
                  value={form.loginEmail}
                  onChange={(e) => setForm({ ...form, loginEmail: e.target.value })}
                />
              </Field>
              <Field
                label={hasAccount ? t("admin.newPassword") : t("admin.loginPassword")}
                error={undefined}
              >
                <Input
                  type="text"
                  autoComplete="off"
                  value={form.loginPassword}
                  onChange={(e) => setForm({ ...form, loginPassword: e.target.value })}
                />
              </Field>
            </div>
            <p className="text-xs text-fg-subtle">
              {hasAccount ? t("admin.newPasswordHint") : t("admin.createAccountHint")}
            </p>
          </div>

          {mutation.isError && <p className="text-sm text-red-400">{apiErrorMessage(mutation.error)}</p>}
          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending && <Spinner className="h-4 w-4" />}
              {t("common.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
