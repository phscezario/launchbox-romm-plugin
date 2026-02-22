namespace RommPlugin.Core.Interfaces
{
    public interface IProgressReporter
    {
        void SetTitle(string title);
        void SetStatus(string message);
        void SetProgress(int value);
        void SetIndeterminate(bool value);
    }
}
