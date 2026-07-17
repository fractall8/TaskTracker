using WebApp.Shared.Toast.Enums;

namespace WebApp.Shared.Toast.Models;

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public bool IsClosing { get; set; }
}
