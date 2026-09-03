using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Storage;

namespace RommPlugin.MenuItems
{
    /// <summary>
    /// Abstract base class for all RomM menu items in LaunchBox and Big Box.
    /// </summary>
    public abstract class RommMenuItem
    {
        private static readonly ConcurrentDictionary<string, Image> _iconCache = new ConcurrentDictionary<string, Image>();

        /// <inheritdoc/>
        protected virtual string IconName => "ico.png";

        /// <inheritdoc/>
        public virtual string Caption => RommConstants.RootCategoryName;

        /// <inheritdoc/>
        public virtual bool ShowInLaunchBox => true;

        /// <inheritdoc/>
        public virtual bool ShowInBigBox => true;

        /// <inheritdoc/>
        public virtual bool AllowInBigBoxWhenLocked => false;

        /// <inheritdoc/>
        public Image IconImage
        {
            get
            {
                if (_iconCache.TryGetValue(IconName, out var img))
                {
                    return img;
                }

                var path = Path.Combine(RommPaths.ImagesFolder, IconName);

                if (!File.Exists(path))
                {
                    return null;
                }

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var ms = new MemoryStream();
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    img = Image.FromStream(ms);
                    _iconCache[IconName] = img;
                }

                return img;
            }
        }

        /// <inheritdoc/>
        public abstract void OnSelected();
    }

}
