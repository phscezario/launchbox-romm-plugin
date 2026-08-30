using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Storage;

namespace RommPlugin.MenuItems
{
    public abstract class RommMenuItem
    {
        private static readonly ConcurrentDictionary<string, Image> _iconCache = new ConcurrentDictionary<string, Image>();

        protected virtual string IconName => "ico.png";

        public virtual string Caption => RommConstants.RootCategoryName;
        public virtual bool ShowInLaunchBox => true;
        public virtual bool ShowInBigBox => true;
        public virtual bool AllowInBigBoxWhenLocked => false;

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

        public abstract void OnSelected();
    }

}
