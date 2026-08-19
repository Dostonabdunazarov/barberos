import { useRef } from "react";
import { useTranslation } from "react-i18next";
import { Card } from "./ui/Card";
import { Button } from "./ui/Button";
import { LoadingState, Spinner } from "./ui/misc";
import { useMasterWorks } from "../lib/hooks";
import { useUploadWork, useDeleteWork } from "../lib/staffHooks";
import { apiErrorMessage } from "../lib/api";

const MAX_PHOTOS = 20;

/** Управление портфолио работ мастера: загрузка файлов, превью-сетка, удаление. */
export function WorksManager({ masterId }: { masterId: string }) {
  const { t } = useTranslation();
  const works = useMasterWorks(masterId);
  const upload = useUploadWork();
  const remove = useDeleteWork();
  const inputRef = useRef<HTMLInputElement>(null);

  const count = works.data?.length ?? 0;
  const atLimit = count >= MAX_PHOTOS;

  function onPick(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (file) upload.mutate({ masterId, file });
    // Сброс, чтобы повторный выбор того же файла тоже сработал.
    e.target.value = "";
  }

  return (
    <Card className="p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="font-display text-xl text-fg">{t("works.title")}</h3>
          <p className="mt-1 text-sm text-fg-subtle">{t("works.hint")}</p>
        </div>
        <div className="flex flex-col items-end gap-1">
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            onChange={onPick}
          />
          <Button
            onClick={() => inputRef.current?.click()}
            disabled={upload.isPending || atLimit}
          >
            {upload.isPending && <Spinner className="h-4 w-4" />}
            {upload.isPending ? t("works.uploading") : t("works.upload")}
          </Button>
          <span className="text-xs text-fg-subtle">
            {count} / {MAX_PHOTOS}
          </span>
        </div>
      </div>

      {atLimit && <p className="mt-3 text-sm text-amber-400">{t("works.limitReached")}</p>}
      {upload.isError && (
        <p className="mt-3 text-sm text-red-400">{apiErrorMessage(upload.error)}</p>
      )}

      {works.isLoading ? (
        <LoadingState />
      ) : count > 0 ? (
        <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {works.data!.map((w) => (
            <div key={w.id} className="group relative overflow-hidden rounded-xl border border-white/10">
              <img src={w.url} alt="" loading="lazy" className="aspect-square w-full object-cover" />
              <button
                type="button"
                onClick={() => remove.mutate({ masterId, photoId: w.id })}
                disabled={remove.isPending}
                className="absolute right-2 top-2 rounded-lg bg-black/60 px-2 py-1 text-xs text-white opacity-0 transition-opacity hover:bg-red-600/80 group-hover:opacity-100"
              >
                {t("works.delete")}
              </button>
            </div>
          ))}
        </div>
      ) : (
        <p className="mt-6 text-center text-fg-subtle">{t("works.empty")}</p>
      )}
    </Card>
  );
}
