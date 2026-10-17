namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Pure ownership rule for the extract folder after a failed install attempt.
    /// A failed attempt must only ever delete the game folder when it is certain
    /// the folder belongs to that attempt; deleting a folder another run created
    /// (or that already existed) is what produced "registered as installed but
    /// gone from disk" ghosts.
    /// </summary>
    public static class InstallFailureCleanup
    {
        /// <summary>
        /// True only when the destination did not exist before the attempt started
        /// <em>and</em> this attempt actually published it (the atomic move
        /// succeeded). Otherwise the folder must be left untouched.
        /// </summary>
        /// <param name="extractDirExistedBeforeAttempt">Directory.Exists result captured before extracting.</param>
        /// <param name="attemptPublishedExtractDir">True once this attempt performed the staging→destination move.</param>
        public static bool ShouldDeleteExtractDir(
            bool extractDirExistedBeforeAttempt,
            bool attemptPublishedExtractDir)
        {
            return !extractDirExistedBeforeAttempt && attemptPublishedExtractDir;
        }
    }
}
