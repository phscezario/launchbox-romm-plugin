using System.Threading;
using RommPlugin.UI.Forms;
using RommPlugin.Core.Interfaces;

namespace RommPlugin.UI.Reporters
{
    public class ProgressFormReporter : IProgressReporter
    {
        private readonly ProgressForm _form;

        public ProgressFormReporter(ProgressForm form, CancellationToken cancellationToken)
        {
            _form = form;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public void SetTitle(string title) => _form.SetTitle(title);
        public void SetStatus(string message) => _form.SetStatus(message);
        public void SetProgress(int value) => _form.SetProgress(value);
        public void SetIndeterminate(bool value) => _form.SetIndeterminate(value);
    }
}
