using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace RommPlugin.MenuItems
{
    public abstract class RommMenuItem
    {
        private static readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>();

        protected virtual string IconName => "ico.png";

        public virtual string Caption => "RomM";
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

                var path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Plugins",
                    "RomM LaunchBox Integration",
                    "Images",
                    IconName
                );

                if (!File.Exists(path))
                {
                    return null;
                }

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    img = Image.FromStream(fs);
                    _iconCache[IconName] = img;
                }

                return img;
            }
        }

        public abstract void OnSelected();
    }

}
