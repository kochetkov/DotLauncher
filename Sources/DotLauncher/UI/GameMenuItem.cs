using System;
using System.Drawing;
using System.Windows.Forms;

namespace DotLauncher.UI
{
    internal class GameMenuItem : ToolStripMenuItem
    {
        private const uint FavoriteAreaSize = 30;

        public Color BrandColor { get; }
        public bool FavoriteAreaClicked { get; private set; }
        public bool FavoritePressed { get; private set; }

        private bool addedToFavorites;

        public GameMenuItem(Color brandColor, bool addedToFavorites, string text, Image image, EventHandler handler) 
            : base(text, image, handler)
        {
            BrandColor = brandColor;
            this.addedToFavorites = addedToFavorites;
            ShowShortcutKeys = false;
            AutoToolTip = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.X > this.Size.Width - FavoriteAreaSize)
            {
                FavoriteAreaClicked = true;
                FavoritePressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.X > this.Size.Width - FavoriteAreaSize)
            {
                FavoritePressed = false;
                Invalidate();
            }

            base.OnMouseUp(e);
        }

        protected override void OnClick(EventArgs e)
        {
            if (FavoriteAreaClicked)
            {
                FavoriteAreaClicked = false;
                addedToFavorites = !addedToFavorites;
                Invalidate();
                return;
            }

            base.OnClick(e);
        }
    }
}
