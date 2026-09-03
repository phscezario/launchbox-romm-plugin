using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RommPlugin.Services
{
    /// <summary>
    /// Provides image processing utilities for converting images to RGB JPEG format and calculating visible content bounds.
    /// </summary>
    public static class RommImageService
    {
        /// <summary>
        /// Ensures an image file is saved as an RGB JPEG (no alpha channel). If the image has transparency
        /// or uses an indexed pixel format, it is converted to RGB JPEG by compositing onto a black background
        /// and cropping to visible content bounds.
        /// </summary>
        /// <param name="originalPath">The file path of the original image.</param>
        /// <returns>The path of the converted RGB JPEG file. If no conversion was needed, returns the original path.</returns>
        public static string EnsureRgbJpeg(string originalPath)
        {
            using (var img = Image.FromFile(originalPath))
            {
                var format = img.PixelFormat;

                var hasAlpha = (format & PixelFormat.Alpha) != 0;
                var isIndexed = (format & PixelFormat.Indexed) != 0;

                if (hasAlpha || isIndexed)
                {
                    var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

                    using (var src = new Bitmap(img))
                    {
                        Rectangle crop = GetVisibleBounds(src);

                        using (var cropped = src.Clone(crop, src.PixelFormat))
                        using (var bmp = new Bitmap(cropped.Width, cropped.Height))
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.Clear(Color.Black);
                            g.DrawImage(cropped, 0, 0);

                            bmp.Save(temp, ImageFormat.Jpeg);
                        }
                    }

                    return temp;
                }
            }

            return originalPath;
        }

        /// <summary>
        /// Calculates the bounding rectangle of all non-transparent pixels in a bitmap.
        /// Used to crop transparent borders from images with alpha channels.
        /// </summary>
        /// <param name="bmp">The bitmap to analyze for visible content bounds.</param>
        /// <returns>A <see cref="Rectangle"/> encompassing all non-transparent pixels, or the full image bounds if none found.</returns>
        public static Rectangle GetVisibleBounds(Bitmap bmp)
        {
            int minX = bmp.Width, minY = bmp.Height;
            int maxX = -1, maxY = -1;

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                int bytes = Math.Abs(data.Stride) * bmp.Height;
                byte[] buffer = new byte[bytes];

                Marshal.Copy(data.Scan0, buffer, 0, bytes);

                for (int y = 0; y < bmp.Height; y++)
                {
                    int row = y * data.Stride;

                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int index = row + (x * 4);

                        byte alpha = buffer[index + 3];

                        if (alpha > 0)
                        {
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            if (maxX < minX || maxY < minY)
            {
                return new Rectangle(0, 0, bmp.Width, bmp.Height);
            }

            return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }
    }
}
