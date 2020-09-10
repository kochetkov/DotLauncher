using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DotLauncher.Properties;

namespace DotLauncher.UI
{
    internal class AppContext : ApplicationContext
    {
        private readonly Registry registry;
        private readonly Dictionary<GameMenuItem, GameDescriptor> gameDescriptorsMap;
        private readonly NotifyIcon notifyIcon;

        private readonly ContextMenuStrip mainMenu;
        private readonly ContextMenuStrip refreshingMenu;
        
        private readonly HashSet<ToolStripItem> dynamicMenuItems;

        private ToolStripItemCollection allGamesMenuItems;

        private readonly Dictionary<ToolStripDropDownMenu, bool> preventClose;
        private int mainMenuWidth;

        public AppContext(Registry registry)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            this.registry = registry;
            gameDescriptorsMap = new Dictionary<GameMenuItem, GameDescriptor>();
            dynamicMenuItems = new HashSet<ToolStripItem>();
            preventClose = new Dictionary<ToolStripDropDownMenu, bool>();

            notifyIcon = new NotifyIcon
            {
                Icon = new Icon(Resources.main_icon, SystemInformation.SmallIconSize),
                // ReSharper disable once LocalizableElement
                Text = $"{Application.ProductName} v{Application.ProductVersion}",
                Visible = true
            };

            // Hack with reflection is needed to workarround https://www.betaarchive.com/wiki/index.php/Microsoft_KB_Archive/135788
            // without  wrapping WinAPI's TrackPopupMenu function
            notifyIcon.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    mi?.Invoke(notifyIcon, null);
                }
            };

            refreshingMenu = new ContextMenuStrip();
            BuildRefreshingMenu();

            mainMenu = new ContextMenuStrip();
            mainMenu.ItemClicked += OnContextMenuItemClicked;
            mainMenu.Closing += OnContextMenuClosing;

            BuildMainMenu();
        }

        public void Run()
        {
            Application.Run(this);
        }

        public bool IsGameMenuItemFavorited(GameMenuItem gameMenuItem)
        {
            Debug.Assert(gameDescriptorsMap.ContainsKey(gameMenuItem));
            return registry.GetGameAddedToFavorites(gameDescriptorsMap[gameMenuItem]);
        }

        private static ToolStripMenuItem AddToolStripMenuItem(ToolStripItemCollection itemsCollection, string text, 
            Action clickHandler = null, bool insertToTop = false)
        {
            var item = clickHandler == null
                ? new ToolStripMenuItem(text, null)
                : new ToolStripMenuItem(text, null, (obj, args) => clickHandler());

            item.ShowShortcutKeys = false;

            if (insertToTop)
            {
                itemsCollection.Insert(0, item);
            }
            else
            {
                itemsCollection.Add(item);
            }

            return item;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static ToolStripLabel AddLabel(ToolStripItemCollection itemsCollection, string text, bool insertToTop = false)
        {
            var label = new ToolStripLabel(text) {ForeColor = ThemeColors.DisabledColor};

            if (insertToTop)
            {
                itemsCollection.Insert(0, label);
            }
            else
            {
                itemsCollection.Add(label);
            }

            label.Enabled = false;

            return label;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static ToolStripSeparator AddMenuSeparator(ToolStripItemCollection itemsCollection, bool insertToTop = false)
        {
            var separator = new ToolStripSeparator();

            if (insertToTop)
            {
                itemsCollection.Insert(0, separator);
            }
            else
            {
                itemsCollection.Add(separator);
            }

            return separator;
        }

        private void BuildRefreshingMenu()
        {
            AddLabel(refreshingMenu.Items, "Refreshing...");
            AddMenuSeparator(refreshingMenu.Items);
            AddExitMenuItem(refreshingMenu.Items);
            ApplyProperties(refreshingMenu);
        }

        private void AddExitMenuItem(ToolStripItemCollection itemsCollection)
        {
            var exitItem = AddToolStripMenuItem(itemsCollection, "Exit", OnExitClicked);
            exitItem.Font = new Font(exitItem.Font, FontStyle.Bold);
        }

        private void BuildMainMenu()
        {
            notifyIcon.ContextMenuStrip = refreshingMenu;
            
            mainMenu.Items.Clear();
            dynamicMenuItems.Clear();
            
            registry.Refresh();

            BuildDynamicItems(clean: true);

            var allGamesItem = AddToolStripMenuItem(mainMenu.Items, "All installed games");
            allGamesItem.DropDown.ItemClicked += OnContextMenuItemClicked;
            allGamesItem.DropDown.Closing += OnContextMenuClosing;

            allGamesMenuItems = allGamesItem.DropDownItems;
            _ = AddGameMenuItems(allGamesMenuItems, registry.InstalledGames).ToList();

            AddMenuSeparator(mainMenu.Items);
            
            AddToolStripMenuItem(mainMenu.Items, "Refresh", OnRefreshClicked);
            AddExitMenuItem(mainMenu.Items);

            ApplyProperties(mainMenu);
            mainMenu.PerformLayout();
            if (mainMenuWidth < mainMenu.Width) { mainMenuWidth = mainMenu.Width; }

            notifyIcon.ContextMenuStrip = mainMenu;
        }

        private void BuildDynamicItems(bool clean)
        {
            if (!clean)
            {
                mainMenu.SuspendLayout();

                foreach (var dynamicMenuItem in dynamicMenuItems)
                {
                    mainMenu.Items.Remove(dynamicMenuItem);
                }

                dynamicMenuItems.Clear();
            }


            if (registry.MostPlayedGames.Count != 0)
            {
                dynamicMenuItems.Add(AddMenuSeparator(mainMenu.Items, insertToTop: true));
                var mpgItems = AddGameMenuItems(mainMenu.Items, registry.MostPlayedGames, insertToTop: true);

                foreach (var mpgItem in mpgItems)
                {
                    dynamicMenuItems.Add(mpgItem);
                }

                dynamicMenuItems.Add(AddLabel(mainMenu.Items, "Most played games:", insertToTop: true));
            }

            if (registry.FavoriteGames.Count != 0)
            {
                dynamicMenuItems.Add(AddMenuSeparator(mainMenu.Items, insertToTop: true));
                var favItems = AddGameMenuItems(mainMenu.Items, registry.FavoriteGames, insertToTop: true);

                foreach (var favItem in favItems)
                {
                    dynamicMenuItems.Add(favItem);
                }

                dynamicMenuItems.Add(AddLabel(mainMenu.Items, "Favorites:", insertToTop: true));
            }

            if (!clean)
            {
                mainMenu.ResumeLayout();
                mainMenu.PerformLayout();
                mainMenu.Refresh();

                if (mainMenuWidth < mainMenu.Width) { mainMenuWidth = mainMenu.Width; }
            }
        }

        private IEnumerable<GameMenuItem> AddGameMenuItems(ToolStripItemCollection itemsCollection, IEnumerable<GameDescriptor> gameDescriptors, 
            bool insertToTop = false)
        {
            foreach (var gameDescriptor in gameDescriptors)
            {
                var libraryBrandColor = registry.GetGameLibraryBrandColor(gameDescriptor);
                var addedToFavorites = registry.GetGameAddedToFavorites(gameDescriptor);

                var gameItem = new GameMenuItem(
                    libraryBrandColor, 
                    addedToFavorites, 
                    gameDescriptor.Name, 
                    null,
                    (obj, args) => OnGameMenuItemClicked(gameDescriptor));

                gameDescriptorsMap.Add(gameItem, gameDescriptor);

                if (insertToTop)
                {
                    itemsCollection.Insert(0, gameItem);
                }
                else
                {
                    itemsCollection.Add(gameItem);
                }

                yield return gameItem;
            }
        }

        private void ApplyProperties(ContextMenuStrip contextMenuStrip)
        {
            void ApplyToDropDown(ToolStripDropDownMenu dropDown)
            {
                dropDown.Renderer = new ToolStripRenderer(this);
                dropDown.ForeColor = ThemeColors.ForeColor;
                dropDown.BackColor = ThemeColors.BackColor;
                dropDown.ShowImageMargin = false;
            }

            ApplyToDropDown(contextMenuStrip);

            foreach (var toolStripMenuItem in contextMenuStrip.Items.OfType<ToolStripMenuItem>()
                .Where(x => x.HasDropDown))
            {
                if (toolStripMenuItem.DropDown is ToolStripDropDownMenu dropDown)
                {
                    ApplyToDropDown(dropDown);
                }
            }
        }

        private void OnContextMenuClosing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            if (sender is ToolStripDropDownMenu dropDownMenu &&
                preventClose.TryGetValue(dropDownMenu, out var cancel))
            {
                if (mainMenu.Equals(dropDownMenu)
                    && e.CloseReason == ToolStripDropDownCloseReason.AppFocusChange
                    && mainMenu.Width < mainMenuWidth)
                {
                    e.Cancel = true;
                    mainMenuWidth = mainMenu.Width;
                    return;
                }

                e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && cancel;
                if (e.Cancel) { preventClose[dropDownMenu] = false; }
            }
        }

        private void OnContextMenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (sender is ToolStripDropDownMenu dropDownMenu)
            {
                if (e.ClickedItem is GameMenuItem gameMenuItem)
                {
                    if (gameMenuItem.FavoriteAreaClicked)
                    {
                        preventClose[dropDownMenu] = true;
                        OnFavoriteAreaClicked(gameMenuItem);
                        return;
                    }
                }

                preventClose[dropDownMenu] = false;
            }
        }

        private void OnFavoriteAreaClicked(GameMenuItem gameMenuItem)
        {
            Debug.Assert(gameDescriptorsMap.ContainsKey(gameMenuItem));
            var gameDescriptor = gameDescriptorsMap[gameMenuItem];
            registry.ChangeGameFavoriteStatus(gameDescriptor);
            BuildDynamicItems(clean: false);
        }

        private void OnGameMenuItemClicked(GameDescriptor gameDescriptor)
        {
            registry.RunGame(gameDescriptor);
            BuildMainMenu();
        }

        private void OnExitClicked()
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void OnRefreshClicked()
        {
            BuildMainMenu();
        }
    }
}
