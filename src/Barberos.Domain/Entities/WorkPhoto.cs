using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Фото работы мастера (портфолио). Файл хранится на сервере, здесь — относительный URL.</summary>
public class WorkPhoto : BaseEntity
{
    public Guid MasterId { get; set; }
    public Master? Master { get; set; }

    /// <summary>Относительный URL файла, напр. <c>/uploads/works/{guid}.jpg</c>.</summary>
    public string Url { get; set; } = null!;

    /// <summary>Порядок отображения в галерее (меньше — раньше).</summary>
    public int SortOrder { get; set; }
}
