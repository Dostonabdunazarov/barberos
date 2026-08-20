import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useUpdateMasterContact } from "../lib/staffHooks";
import { Card } from "./ui/Card";
import { Button } from "./ui/Button";
import { Input, Field } from "./ui/Input";
import { Spinner } from "./ui/misc";
import { apiErrorMessage } from "../lib/api";
import type { Master } from "../types";

/**
 * Редактор публичного контакта мастера (кабинет).
 * Номер виден всем на витрине, поэтому пустое значение — законный сценарий:
 * так мастер убирает контакт с сайта.
 */
export function ContactEditor({ master }: { master: Master }) {
  const { t } = useTranslation();
  const update = useUpdateMasterContact();
  const [phone, setPhone] = useState(master.publicPhone ?? "");

  const saved = master.publicPhone ?? "";
  const dirty = phone.trim() !== saved;

  function submit(e: React.FormEvent) {
    e.preventDefault();
    update.mutate({ masterId: master.id, publicPhone: phone.trim() || null });
  }

  return (
    <Card className="max-w-md p-5">
      <h3 className="font-display text-lg text-fg">{t("dashboard.contactTitle")}</h3>
      <form onSubmit={submit} className="mt-4 space-y-3">
        <Field label={t("admin.publicPhone")}>
          <Input
            type="tel"
            inputMode="tel"
            autoComplete="tel"
            placeholder="+998 90 123-45-67"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
        </Field>
        <p className="text-xs text-fg-subtle">{t("dashboard.contactHint")}</p>

        {update.isError && <p className="text-sm text-red-400">{apiErrorMessage(update.error)}</p>}
        {update.isSuccess && !dirty && (
          <p className="text-sm text-emerald-400">{t("dashboard.contactSaved")}</p>
        )}

        <Button type="submit" disabled={update.isPending || !dirty}>
          {update.isPending && <Spinner className="h-4 w-4" />}
          {t("dashboard.saveContact")}
        </Button>
      </form>
    </Card>
  );
}
