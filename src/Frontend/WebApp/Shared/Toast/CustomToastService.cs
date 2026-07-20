using WebApp.Shared.Toast.Enums;
using WebApp.Shared.Toast.Models;
using Timer = System.Timers.Timer;

namespace WebApp.Shared.Toast;

public class CustomToastService
{
    public event Action? OnChange;
    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts => _toasts;

    public void ShowSuccess(string message) => ShowToast(message, ToastType.Success);
    public void ShowError(string message) => ShowToast(message, ToastType.Error);
    public void ShowInfo(string message) => ShowToast(message, ToastType.Info);
    public void ShowWarning(string message) => ShowToast(message, ToastType.Warning);

    private void ShowToast(string message, ToastType type)
    {
        var toast = new ToastMessage { Message = message, Type = type };
        _toasts.Add(toast);
        OnChange?.Invoke();

        var timer = new Timer(4000);
        timer.Elapsed += async (s, e) =>
        {
            timer.Dispose();
            await RemoveToastAsync(toast.Id);
        };
        timer.AutoReset = false;
        timer.Start();
    }

    public async Task RemoveToastAsync(Guid id)
    {
        var toast = _toasts.Find(t => t.Id == id);
        if (toast != null && !toast.IsClosing)
        {
            toast.IsClosing = true;
            OnChange?.Invoke();

            await Task.Delay(300);

            _toasts.Remove(toast);
            OnChange?.Invoke();
        }
    }
}
