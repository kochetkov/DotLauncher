using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DotLauncher.UI
{
    internal class ToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly AppContext appContext;

        public ToolStripRenderer(AppContext appContext) : base(new ThemeColors())
        {
            this.appContext = appContext;
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            if (e.Item is ToolStripMenuItem) { e.ArrowColor = ThemeColors.ForeColor; }
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                e.TextColor = ThemeColors.DisabledColor;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            base.OnRenderMenuItemBackground(e);

            var toolStrip = e.ToolStrip;

            if (e.Item is GameMenuItem gameMenuItem)
            {
                var graphics = e.Graphics;
                var bounds = graphics.VisibleClipBounds;
                var brandMarkBrush = new SolidBrush(gameMenuItem.BrandColor);

                var brandMarkRectF = new RectangleF(
                    bounds.X + 2,
                    bounds.Y,
                    4,
                    bounds.Height
                );
                
                graphics.FillRectangle(brandMarkBrush, brandMarkRectF);

                if (gameMenuItem.Selected)
                {
                    string favoriteChar;
                    Color favoriteColor;

                    if (appContext.IsGameMenuItemFavorited(gameMenuItem))
                    {
                        favoriteChar = "★";
                        favoriteColor = ThemeColors.FavoriteColor;
                    }
                    else
                    {
                        favoriteChar = "☆";
                        favoriteColor = ThemeColors.DisabledColor;
                    }

                    var favoriteMarkBrush = new SolidBrush(favoriteColor);
                    var favoriteMarkFont = SystemFonts.DefaultFont;
                    var favoriteMarkMeasures = graphics.MeasureString(favoriteChar, favoriteMarkFont);
                    var pressedOffset = gameMenuItem.FavoritePressed ? 1 : 0;
                    
                    var favoriteMarkPointF = new PointF(
                        // tooldStrip.Width used here due to a weird behavior of bounds.Width after add/remove menu items
                        bounds.X + toolStrip.Width - favoriteMarkMeasures.Width - 5 + pressedOffset, 
                        bounds.Y + bounds.Height / 2 - favoriteMarkMeasures.Height / 2 + 1 + pressedOffset
                    );

                    var oldTextRenderingHint = graphics.TextRenderingHint;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                    graphics.DrawString(favoriteChar, favoriteMarkFont, favoriteMarkBrush, favoriteMarkPointF, StringFormat.GenericTypographic);
                    graphics.TextRenderingHint = oldTextRenderingHint;
                }
            }
        }
    }
}
