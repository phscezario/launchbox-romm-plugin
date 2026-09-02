using System.Threading;
using RommPlugin.UI.Forms;
using RommPlugin.Core.Interfaces;

namespace RommPlugin.UI.Reporters
{
    /// <summary>
    /// Implements <see cref="IProgressReporter"/> by delegating progress updates to a <see cref="ProgressForm"/>.
    /// </summary>
    public class ProgressFormReporter : IProgressReporter
    {
        private readonly ProgressForm _form;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressFormReporter"/> class.
        /// </summary>
        /// <param name="form">The progress form to report to.</param>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        public ProgressFormReporter(ProgressForm form, CancellationToken cancellationToken)
        {
            _form = form;
            CancellationToken = cancellationToken;
        }

        /// <inheritdoc/>
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc/>
        public void SetTitle(string title) => _form.SetTitle(title);

        /// <inheritdoc/>
        public void SetStatus(string message) => _form.SetStatus(message);

        /// <inheritdoc/>
        public void SetProgress(int value) => _form.SetProgress(value);

        /// <inheritdoc/>
        public void SetIndeterminate(bool value) => _form.SetIndeterminate(value);
    }
}
