using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Decisions
{
    /// <summary>Partition of a processed batch into installed vs failed ids.</summary>
    public sealed class InstallOutcome
    {
        public InstallOutcome()
        {
            ProcessedIds = new List<int>();
            FailedIds = new List<int>();
        }

        public IList<int> ProcessedIds { get; }
        public IList<int> FailedIds { get; }
    }

    /// <summary>
    /// Pure state-diff for install batches. Mirrors the Game Manager rule:
    /// processed = WaitingInstall turned Installed; failed = WaitingInstall
    /// turned anything else; untouched items stay out of both sets.
    /// </summary>
    public static class InstallOutcomeClassifier
    {
        public static InstallOutcome Classify(
            IReadOnlyDictionary<int, DownloadItem> before,
            IReadOnlyDictionary<int, DownloadItem> after)
        {
            var outcome = new InstallOutcome();
            if (before == null || after == null) return outcome;

            foreach (var kv in before)
            {
                if (kv.Value == null || kv.Value.Status != DownloadStatus.WaitingInstall)
                    continue;
                if (!after.TryGetValue(kv.Key, out var next) || next == null)
                    continue;

                if (next.Status == DownloadStatus.Installed)
                    outcome.ProcessedIds.Add(kv.Key);
                else if (next.Status != DownloadStatus.WaitingInstall)
                    outcome.FailedIds.Add(kv.Key);
            }

            return outcome;
        }
    }
}
