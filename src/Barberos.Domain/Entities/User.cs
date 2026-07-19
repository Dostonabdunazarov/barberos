using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>
/// Сотрудник барбершопа (мастер или админ). Вход по email + паролю.
/// Клиенты пользователями НЕ являются — их данные хранятся в брони (гостевая бронь).
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Name { get; set; }
    public UserRole Role { get; set; } = UserRole.Master;
    public bool IsActive { get; set; } = true;
}
