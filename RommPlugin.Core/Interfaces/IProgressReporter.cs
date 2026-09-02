using System.Threading;

namespace RommPlugin.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for reporting progress during long-running operations.
    /// Implementations update a UI progress dialog with title, status, and percentage.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Sets the title of the progress dialog.
        /// </summary>
        /// <param name="title">The title text to display.</param>
        void SetTitle(string title);

        /// <summary>
        /// Sets the current status message of the progress dialog.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        void SetStatus(string message);

        /// <summary>
        /// Sets the progress percentage (0-100).
        /// </summary>
        /// <param name="value">The progress percentage, between 0 and 100.</param>
        void SetProgress(int value);

        /// <summary>
        /// Sets whether the progress bar shows an indeterminate (marquee) state.
        /// </summary>
        /// <param name="value">True to show indeterminate progress; false for determinate percentage.</param>
        void SetIndeterminate(bool value);

        /// <summary>
        /// Gets the cancellation token that signals when the user requests cancellation.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}
