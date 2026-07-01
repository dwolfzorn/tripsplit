namespace TripSplit.Client.Services;

public record ToastMessage(Guid Id, string Message, bool IsError);

public class ToastService
{
    public event Action<ToastMessage>? OnToast;

    public void Show(string message, bool isError = false) =>
        OnToast?.Invoke(new ToastMessage(Guid.NewGuid(), message, isError));
}
