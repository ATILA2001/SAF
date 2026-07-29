using Radzen;

namespace SAF.Shared;

public class NotificationHelper(NotificationService notificationService) : INotificationHelper
{
    // Centralizamos duraciones para facilitar ajustes.
    private const double DurationSuccess = 4000;
    private const double DurationInfo = 6000;
    private const double DurationWarning = 8000;
    private const double DurationError = -1; // Negativos o null lo hacen persistente.

    public void ShowSuccess(string message, string title = "Éxito")
        => Notify(NotificationSeverity.Success, title, message, DurationSuccess);

    public void ShowInfo(string message, string title = "Información")
        => Notify(NotificationSeverity.Info, title, message, DurationInfo);

    public void ShowWarning(string message, string title = "Advertencia")
        => Notify(NotificationSeverity.Warning, title, message, DurationWarning);

    public void ShowError(string message, string title = "Error")
        => Notify(NotificationSeverity.Error, title, message, DurationError);

    private void Notify(NotificationSeverity severity, string summary, string detail, double? duration)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary,
            Detail = detail,
            Duration = duration,
            Payload = DateTime.Now // Agregar más info útil para logs o debugging.
        });
    }
}
