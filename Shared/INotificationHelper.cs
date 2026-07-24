namespace SAF.Shared
{
    public interface INotificationHelper
    {
        void ShowError(string message, string title = "Error");
        void ShowSuccess(string message, string title = "Éxito");
        void ShowInfo(string message, string title = "Información");
        void ShowWarning(string message, string title = "Advertencia");
    }
}
