using System.Drawing;
using System.Windows.Forms;

namespace DotLauncher.UI
{
    internal class ThemeColors : ProfessionalColorTable
    {
        public static Color PrimaryColor   = Color.FromArgb(0, 0, 0);
        public static Color BackColor      = Color.FromArgb(41, 42, 45);
        public static Color ForeColor      = Color.FromArgb(255, 255, 255);
        public static Color HighLightColor = Color.FromArgb(75, 76, 79);
        public static Color DisabledColor     = Color.FromArgb(139, 142, 145);
        public static Color BorderColor    = Color.FromArgb(56, 59, 62);
        public static Color FavoriteColor  = Color.FromArgb(250, 165, 27);

        public override Color SeparatorLight => BorderColor;
        public override Color SeparatorDark => BorderColor;
        public override Color MenuItemSelected => HighLightColor;
        public override Color MenuItemSelectedGradientBegin => HighLightColor;
        public override Color MenuItemSelectedGradientEnd => HighLightColor;
        public override Color MenuItemBorder { get; } = HighLightColor;
        public override Color MenuBorder { get; } = BorderColor;
    }
}