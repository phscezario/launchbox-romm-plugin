using RommPlugin.Core.Helpers;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Decides whether a finished download may be released to the install pass.
    /// A transfer whose content is a zip that cannot be opened is a
    /// corrupt/truncated archive: releasing it as <c>WaitingInstall</c> only
    /// fails minutes later inside the extractor, after the row has already been
    /// shown as "Installing". It must fail immediately with the corrupt marker
    /// so the existing delete + re-download flow takes over.
    /// Non-zip content (single ROM files, ISOs) is never probed here.
    /// </summary>
    public static class DownloadArchiveDecider
    {
        /// <summary>
        /// True when the completed file can be handed to the install pass.
        /// </summary>
        /// <param name="finalPath">Destination the download was moved to.</param>
        /// <param name="isZipContent">Whether the transferred bytes start with a zip local file header.</param>
        public static bool IsReleasable(string finalPath, bool isZipContent)
        {
            if (!isZipContent) return true;
            return RommArchiveHelper.IsZipReadable(finalPath);
        }
    }
}
