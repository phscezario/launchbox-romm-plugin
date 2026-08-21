using System.Drawing;
using System.Windows.Forms;

namespace RommPlugin.UI.Helpers
{
    public static class ListViewProgressRenderer
    {
        private static readonly Font ProgressFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly SolidBrush BarBrush = new SolidBrush(Color.Crimson);
        private static readonly SolidBrush BarBackgroundBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        private static readonly SolidBrush TextBrush = new SolidBrush(Color.White);
        private static readonly StringFormat CenterFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        public static void DrawProgressCell(Graphics g, Rectangle bounds, int percentage, Color barColor)
        {
            g.FillRectangle(BarBackgroundBrush, bounds);

            if (percentage > 0)
            {
                var barWidth = (int)((bounds.Width - 4) * (percentage / 100.0));
                var barRect = new Rectangle(bounds.X + 2, bounds.Y + 2, barWidth, bounds.Height - 4);
                using (var brush = new SolidBrush(barColor))
                {
                    g.FillRectangle(brush, barRect);
                }
            }

            var text = percentage > 0 ? $"{percentage}%" : "--";
            var textRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            g.DrawString(text, ProgressFont, TextBrush, textRect, CenterFormat);
        }

        public static void DrawStatusCell(Graphics g, Rectangle bounds, string status, Color textColor)
        {
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(bounds.X + 4, bounds.Y, bounds.Width - 4, bounds.Height);
                g.DrawString(status, Control.DefaultFont, brush, textRect,
                    new StringFormat { LineAlignment = StringAlignment.Center });
            }
        }
    }
}
