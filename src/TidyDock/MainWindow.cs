using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace TidyDock
{
    public class MainWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly IconCacheService _iconCache;
        private readonly DockConfig _config;
        private readonly Border _dockBorder;
        private readonly StackPanel _itemsPanel;
        private FolderPanel _folderPanel;
        private readonly DispatcherTimer _hideTimer;
        private readonly DispatcherTimer _saveTimer;
        private readonly DispatcherTimer _showDesktopRestoreTimer;
        private int _showDesktopRestoreAttempts;
        private ThemePalette _palette;
        private Window _hotZone;
        private DockItem _pressedItem;
        private Point _pressPoint;
        private Window _dragGhostWindow;
        private Button _dragSourceButton;
        private SettingsWindow _settingsWindow;
        public Action<bool> TrayVisibilitySetter { get; set; }
        public Action TrayTextRefresher { get; set; }

        public MainWindow(SettingsService settingsService, DockConfig config)
        {
            _settingsService = settingsService;
            _config = config;
            _iconCache = new IconCacheService(settingsService.IconCacheDirectory);
            _palette = ThemeService.GetPalette(_config.Dock.Theme);

            Title = "TidyDock";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = false;
            Topmost = _config.Dock.AlwaysOnTop;
            AllowDrop = true;

            _dockBorder = new Border();
            _dockBorder.SnapsToDevicePixels = true;
            _dockBorder.BorderThickness = new Thickness(1);
            _dockBorder.BorderBrush = Brushes.Transparent;
            _dockBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 30,
                ShadowDepth = 10,
                Direction = 270,
                Opacity = 0.28
            };
            Content = _dockBorder;

            _itemsPanel = new StackPanel();
            _itemsPanel.AllowDrop = true;
            _itemsPanel.Drop += OnDockDrop;
            _itemsPanel.DragOver += OnDockDragOver;
            _dockBorder.Child = _itemsPanel;

            _hideTimer = new DispatcherTimer();
            _hideTimer.Interval = TimeSpan.FromMilliseconds(650);
            _hideTimer.Tick += delegate
            {
                _hideTimer.Stop();
                if (_config.Dock.AutoHide && !IsMouseOver)
                {
                    HideForAutoHide();
                }
            };

            MouseLeave += delegate
            {
                if (_config.Dock.AutoHide)
                {
                    _hideTimer.Stop();
                    _hideTimer.Start();
                }
            };

            MouseEnter += delegate { _hideTimer.Stop(); };

            _saveTimer = new DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(450);
            _saveTimer.Tick += delegate
            {
                _saveTimer.Stop();
                _settingsService.Save(_config);
            };

            _showDesktopRestoreTimer = new DispatcherTimer();
            _showDesktopRestoreTimer.Interval = TimeSpan.FromMilliseconds(220);
            _showDesktopRestoreTimer.Tick += delegate { RestoreAfterShowDesktop(); };

            Loaded += delegate
            {
                ApplySettings();
            };

            DragOver += OnDockDragOver;
            Drop += OnDockDrop;
            PreviewKeyDown += OnPreviewKeyDown;
            StateChanged += OnWindowStateChanged;
            SourceInitialized += delegate { HideFromAltTab(this); };
        }

        public void ShowSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsVisible)
            {
                _settingsWindow = new SettingsWindow(this, _config);
                _settingsWindow.Owner = this;
                _settingsWindow.Closed += delegate { _settingsWindow = null; };
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        public void SaveConfig()
        {
            if (_saveTimer != null)
            {
                _saveTimer.Stop();
            }
            _settingsService.Save(_config);
        }

        public void ResetToDefaults()
        {
            var defaults = _settingsService.CreateDefaultConfig();
            _config.Dock = defaults.Dock;
            _config.FolderPanel = defaults.FolderPanel;
            _config.Items = defaults.Items;
            ApplySettings();
        }

        public void ClearIconCache()
        {
            _settingsService.ClearIconCache();
            RenderDock();
        }

        public void OpenConfigFolder()
        {
            _settingsService.EnsureDirectories();
            Process.Start(new ProcessStartInfo(_settingsService.AppDirectory) { UseShellExecute = true });
        }

        public void OpenLogFolder()
        {
            _settingsService.EnsureDirectories();
            Process.Start(new ProcessStartInfo(_settingsService.LogDirectory) { UseShellExecute = true });
        }

        public void ShowAbout()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var message =
                "TidyDock " + version + Environment.NewLine +
                Environment.NewLine +
                "\u4f4e\u5360\u7528\u3001\u7eaf\u624b\u52a8\u914d\u7f6e\u7684 Windows \u684c\u9762 Dock\u3002" + Environment.NewLine +
                "\u4e0d\u626b\u63cf\u684c\u9762\uff0c\u4e0d\u76d1\u63a7\u8fdb\u7a0b\uff0c\u4e0d\u540e\u53f0\u7d22\u5f15\uff0c\u4e0d\u8054\u7f51\u3002" + Environment.NewLine +
                Environment.NewLine +
                "\u914d\u7f6e\u76ee\u5f55\uff1a" + Environment.NewLine +
                _settingsService.AppDirectory;
            MessageBox.Show(message, "\u5173\u4e8e TidyDock", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string T(string key)
        {
            return LocalizationService.T(_config, key);
        }

        public void ToggleDockVisibility()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
                PositionWindow();
            }
        }

        public void SetTrayIconVisible(bool visible)
        {
            _config.Dock.ShowTrayIcon = visible;
            if (TrayVisibilitySetter != null)
            {
                TrayVisibilitySetter(visible);
            }
            SaveConfig();
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                return;
            }

            ScheduleShowDesktopRestore();
        }

        private void ScheduleShowDesktopRestore()
        {
            _showDesktopRestoreAttempts = 0;
            _showDesktopRestoreTimer.Stop();
            _showDesktopRestoreTimer.Start();
        }

        private void RestoreAfterShowDesktop()
        {
            _showDesktopRestoreAttempts++;

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            if (!IsVisible)
            {
                Show();
            }

            PositionWindow();

            if (_showDesktopRestoreAttempts >= 4)
            {
                _showDesktopRestoreTimer.Stop();
            }
        }

        public void ApplySettings()
        {
            _palette = ThemeService.GetPalette(_config.Dock.Theme);
            Topmost = _config.Dock.AlwaysOnTop;
            _dockBorder.CornerRadius = new CornerRadius(_config.Dock.CornerRadius);
            _dockBorder.Padding = GetDockPadding();
            if (_config.Dock.Opacity <= 0)
            {
                _dockBorder.Background = Brushes.Transparent;
                _dockBorder.BorderThickness = new Thickness(0);
                _dockBorder.BorderBrush = Brushes.Transparent;
                _dockBorder.Effect = null;
            }
            else
            {
                _dockBorder.BorderThickness = new Thickness(1);
                _dockBorder.BorderBrush = CreateDockBorderBrush();
                _dockBorder.Background = CreateDockGlassBrush(_config.Dock.Opacity);
                ApplyDockShadow();
            }

            if (IsVertical())
            {
                _itemsPanel.Orientation = Orientation.Vertical;
            }
            else
            {
                _itemsPanel.Orientation = Orientation.Horizontal;
            }

            if (_folderPanel != null)
            {
                _folderPanel.ApplySettings(_palette);
            }
            if (TrayTextRefresher != null)
            {
                TrayTextRefresher();
            }
            RenderDock();
            UpdateLayout();
            PositionWindow();
            UpdateHotZone();
            ScheduleSaveConfig();
        }

        private void ScheduleSaveConfig()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private Thickness GetDockPadding()
        {
            if (IsVertical())
            {
                return new Thickness(10, 14, 10, 14);
            }
            return new Thickness(14, 10, 14, 10);
        }

        private void ApplyDockShadow()
        {
            _dockBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = _palette.Shadow,
                BlurRadius = _palette.IsDark ? 34 : 30,
                ShadowDepth = IsVertical() ? 8 : 10,
                Direction = GetDockShadowDirection(),
                Opacity = _palette.IsDark ? 0.34 : 0.22
            };
        }

        private double GetDockShadowDirection()
        {
            if (_config.Dock.Position == "top")
            {
                return 90;
            }
            if (_config.Dock.Position == "left")
            {
                return 180;
            }
            if (_config.Dock.Position == "right")
            {
                return 0;
            }
            return 270;
        }

        private Brush CreateDockGlassBrush(double opacity)
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = IsVertical() ? new Point(1, 0) : new Point(0, 1);

            var highlight = _palette.IsDark ? Color.FromRgb(82, 94, 116) : Color.FromRgb(255, 255, 255);
            var lowlight = _palette.IsDark ? Color.FromRgb(22, 27, 35) : Color.FromRgb(225, 235, 244);

            brush.GradientStops.Add(new GradientStop(WithOpacity(highlight, opacity * (_palette.IsDark ? 0.46 : 0.78)), 0));
            brush.GradientStops.Add(new GradientStop(WithOpacity(_palette.DockBackground, opacity), 0.42));
            brush.GradientStops.Add(new GradientStop(WithOpacity(lowlight, opacity * (_palette.IsDark ? 0.72 : 0.55)), 1));
            brush.Freeze();
            return brush;
        }

        private Brush CreateDockBorderBrush()
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = IsVertical() ? new Point(1, 0) : new Point(0, 1);

            var top = _palette.IsDark ? Color.FromRgb(120, 134, 158) : Color.FromRgb(255, 255, 255);
            var bottom = _palette.IsDark ? Color.FromRgb(46, 55, 68) : Color.FromRgb(194, 209, 222);
            brush.GradientStops.Add(new GradientStop(WithOpacity(top, _palette.IsDark ? 0.42 : 0.78), 0));
            brush.GradientStops.Add(new GradientStop(WithOpacity(_palette.DockBorder, _palette.IsDark ? 0.26 : 0.54), 0.5));
            brush.GradientStops.Add(new GradientStop(WithOpacity(bottom, _palette.IsDark ? 0.34 : 0.58), 1));
            brush.Freeze();
            return brush;
        }

        private Brush CreateIconHoverBrush()
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            var highlight = _palette.IsDark ? Color.FromRgb(118, 133, 158) : Color.FromRgb(255, 255, 255);
            var baseColor = _palette.IsDark ? _palette.TileHover : _palette.ControlBackground;
            brush.GradientStops.Add(new GradientStop(WithOpacity(highlight, _palette.IsDark ? 0.32 : 0.72), 0));
            brush.GradientStops.Add(new GradientStop(WithOpacity(baseColor, _palette.IsDark ? 0.18 : 0.52), 1));
            brush.Freeze();
            return brush;
        }

        private static Color WithOpacity(Color color, double opacity)
        {
            opacity = Math.Max(0, Math.Min(1, opacity));
            return Color.FromArgb(
                (byte)(opacity * 255),
                color.R,
                color.G,
                color.B);
        }

        private void RenderDock()
        {
            _itemsPanel.Children.Clear();
            var gap = _config.Dock.IconGap;

            foreach (var item in _config.Items)
            {
                if (item.IsSeparator)
                {
                    _itemsPanel.Children.Add(CreateSeparator());
                }
                else
                {
                    _itemsPanel.Children.Add(CreateDockButton(item, gap));
                }
            }

            if (_config.Items.Count == 0)
            {
                _itemsPanel.Children.Add(CreateEmptyHint());
            }
        }

        private UIElement CreateSeparator()
        {
            var line = new Border();
            line.Margin = IsVertical() ? new Thickness(6, 4, 6, 4) : new Thickness(4, 6, 4, 6);
            line.Background = ThemeService.Brush(_palette.MutedText, 0.24);
            if (IsVertical())
            {
                line.Height = 1;
                line.Width = Math.Max(30, _config.Dock.IconSize - 8);
            }
            else
            {
                line.Width = 1;
                line.Height = Math.Max(30, _config.Dock.IconSize - 8);
            }
            return line;
        }

        private UIElement CreateEmptyHint()
        {
            var button = new Button();
            button.Content = _config.Dock.EditMode ? T("dropHint") : T("editModeHint");
            button.Height = 36;
            button.Padding = new Thickness(14, 0, 14, 0);
            button.BorderThickness = new Thickness(0);
            button.Background = ThemeService.Brush(_palette.ControlBackground, 0.55);
            button.Foreground = ThemeService.Brush(_palette.Text);
            button.Click += delegate { ShowDockMenu(button); };
            return button;
        }

        private Button CreateDockButton(DockItem item, int gap)
        {
            var button = new Button();
            var showLabel = _config.Dock.ShowItemLabels;
            button.Width = showLabel ? Math.Max(76, _config.Dock.IconSize + 8) : _config.Dock.IconSize + 8;
            button.Height = showLabel ? _config.Dock.IconSize + 24 : _config.Dock.IconSize + 8;
            button.Margin = IsVertical() ? new Thickness(0, gap / 2, 0, gap / 2) : new Thickness(gap / 2, 0, gap / 2, 0);
            button.Padding = new Thickness(0);
            button.BorderThickness = new Thickness(0);
            button.Background = Brushes.Transparent;
            button.ToolTip = item.Name;
            button.Tag = item;
            button.AllowDrop = true;
            button.RenderTransformOrigin = new Point(0.5, 0.5);
            button.Template = CreatePlainButtonTemplate();

            var stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.VerticalAlignment = VerticalAlignment.Center;
            stack.Orientation = Orientation.Vertical;

            var iconShell = new Border();
            iconShell.Width = _config.Dock.IconSize;
            iconShell.Height = _config.Dock.IconSize;
            iconShell.CornerRadius = new CornerRadius(Math.Max(10, _config.Dock.IconSize / 4.8));
            iconShell.BorderThickness = new Thickness(1);
            iconShell.BorderBrush = Brushes.Transparent;
            iconShell.Background = Brushes.Transparent;
            iconShell.SnapsToDevicePixels = true;
            iconShell.Margin = showLabel ? new Thickness(0, 0, 0, 2) : new Thickness(0);

            var image = new Image();
            image.Width = _config.Dock.IconSize - 6;
            image.Height = _config.Dock.IconSize - 6;
            image.Margin = new Thickness(3);
            image.Stretch = Stretch.Uniform;
            image.SnapsToDevicePixels = true;
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            image.Source = _iconCache.GetIcon(item, _config.Dock.IconSize);
            iconShell.Child = image;
            stack.Children.Add(iconShell);
            button.Resources["IconShell"] = iconShell;

            if (showLabel)
            {
                var label = new TextBlock();
                label.Text = item.Name;
                label.Width = Math.Max(68, _config.Dock.IconSize);
                label.FontSize = 11;
                label.TextAlignment = TextAlignment.Center;
                label.TextTrimming = TextTrimming.CharacterEllipsis;
                label.Foreground = ThemeService.Brush(_palette.Text);
                stack.Children.Add(label);
            }

            button.Content = stack;

            button.Click += delegate { ActivateItem(item, button); };
            button.MouseEnter += delegate { StyleDockButton(button, true); AnimateDockItem(button, true); };
            button.MouseLeave += delegate { StyleDockButton(button, false); AnimateDockItem(button, false); };
            button.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                _pressedItem = item;
                _pressPoint = e.GetPosition(this);
            };
            button.PreviewMouseMove += OnDockButtonMouseMove;
            button.GiveFeedback += OnDockGiveFeedback;
            button.DragOver += OnDockDragOver;
            button.Drop += OnDockDrop;
            button.ContextMenu = CreateItemMenu(item);

            return button;
        }

        private void StyleDockButton(Button button, bool active)
        {
            var shell = button.Resources["IconShell"] as Border;
            if (shell == null)
            {
                return;
            }

            if (active)
            {
                shell.Background = CreateIconHoverBrush();
                shell.BorderBrush = ThemeService.Brush(_palette.DockBorder, _palette.IsDark ? 0.24 : 0.5);
            }
            else
            {
                shell.Background = Brushes.Transparent;
                shell.BorderBrush = Brushes.Transparent;
            }
        }

        private ContextMenu CreateItemMenu(DockItem item)
        {
            var menu = new ContextMenu();
            var open = new MenuItem { Header = T("open") };
            open.Click += delegate { ActivateItem(item, null); };
            menu.Items.Add(open);

            if (_config.Dock.EditMode)
            {
                var rename = new MenuItem { Header = T("rename") };
                rename.Click += delegate { RenameItem(item); };
                menu.Items.Add(rename);

                if (item.Type != "settings")
                {
                    var target = new MenuItem { Header = T("editTarget") };
                    target.Click += delegate { EditTarget(item); };
                    menu.Items.Add(target);
                }

                var icon = new MenuItem { Header = T("changeIcon") };
                icon.Click += delegate { ChangeIcon(item); };
                menu.Items.Add(icon);
            }

            var show = new MenuItem { Header = item.Type == "folder" ? T("openInExplorer") : T("showInExplorer") };
            show.Click += delegate { ShowInExplorer(item); };
            menu.Items.Add(show);

            if (_config.Dock.EditMode)
            {
                var remove = new MenuItem { Header = T("removeFromDock") };
                remove.Click += delegate { RemoveItem(item); };
                menu.Items.Add(remove);
            }

            menu.Items.Add(new Separator());
            AddDockMenuItems(menu);
            return menu;
        }

        private void AddDockMenuItems(ItemsControl menu)
        {
            var editMode = new MenuItem { Header = T("editMode"), IsCheckable = true, IsChecked = _config.Dock.EditMode };
            editMode.Click += delegate
            {
                _config.Dock.EditMode = editMode.IsChecked;
                ApplySettings();
            };
            menu.Items.Add(editMode);
            menu.Items.Add(new Separator());

            var addUrl = new MenuItem { Header = T("addUrl") };
            addUrl.Click += delegate { AddUrl(); };
            addUrl.IsEnabled = _config.Dock.EditMode;
            menu.Items.Add(addUrl);

            var addSeparator = new MenuItem { Header = T("addSeparator") };
            addSeparator.Click += delegate { AddSeparator(); };
            addSeparator.IsEnabled = _config.Dock.EditMode;
            menu.Items.Add(addSeparator);

            var settings = new MenuItem { Header = T("settings") };
            settings.Click += delegate { ShowSettings(); };
            menu.Items.Add(settings);

            var hide = new MenuItem { Header = T("hideDock") };
            hide.Click += delegate { ToggleDockVisibility(); };
            menu.Items.Add(hide);

            var about = new MenuItem { Header = T("about") };
            about.Click += delegate { ShowAbout(); };
            menu.Items.Add(about);
        }

        private void ShowDockMenu(FrameworkElement target)
        {
            var menu = new ContextMenu();
            AddDockMenuItems(menu);
            menu.PlacementTarget = target;
            menu.IsOpen = true;
        }

        private void ActivateItem(DockItem item, UIElement placementTarget)
        {
            if (item == null)
            {
                return;
            }

            if (item.Type == "settings")
            {
                ShowSettings();
                return;
            }

            if (item.Type == "folder")
            {
                if (!Directory.Exists(item.Target))
                {
                    MessageBox.Show(T("folderMissing") + item.Target, "TidyDock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                EnsureFolderPanel().Open(item.Target, placementTarget ?? this);
                return;
            }

            try
            {
                if (item.Type == "url")
                {
                    Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
                    return;
                }

                if (!File.Exists(item.Target) && !Directory.Exists(item.Target))
                {
                    MessageBox.Show(T("targetMissing") + item.Target, "TidyDock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TidyDock", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameItem(DockItem item)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var dialog = new InputDialog(_config, T("rename"), T("name"), item.Name);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                item.Name = dialog.Value;
                SaveConfig();
                RenderDock();
            }
        }

        private void EditTarget(DockItem item)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var dialog = new InputDialog(_config, T("editTarget"), T("targetPathOrUrl"), item.Target);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                item.Target = dialog.Value;
                SaveConfig();
                RenderDock();
            }
        }

        private void ChangeIcon(DockItem item)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.Filter = T("iconFilter");
            if (dialog.ShowDialog() == true)
            {
                item.Icon = dialog.FileName;
                SaveConfig();
                RenderDock();
            }
        }

        private void ShowInExplorer(DockItem item)
        {
            if (item.Type == "url" || item.Type == "settings")
            {
                return;
            }

            if (Directory.Exists(item.Target))
            {
                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
            }
            else if (File.Exists(item.Target))
            {
                Process.Start("explorer.exe", "/select,\"" + item.Target + "\"");
            }
            else
            {
                MessageBox.Show(T("targetMissing") + item.Target, "TidyDock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveItem(DockItem item)
        {
            if (!RequireEditMode())
            {
                return;
            }

            _config.Items.Remove(item);
            SaveConfig();
            RenderDock();
            PositionWindow();
        }

        private void AddUrl()
        {
            if (!RequireEditMode())
            {
                return;
            }

            var dialog = new InputDialog(_config, T("addUrl"), "URL", "https://");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var url = dialog.Value;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }
                var nameDialog = new InputDialog(_config, T("addUrl"), T("name"), url);
                nameDialog.Owner = this;
                if (nameDialog.ShowDialog() == true)
                {
                    AddDockItem("url", nameDialog.Value, url, null);
                }
            }
        }

        private void AddSeparator()
        {
            if (!RequireEditMode())
            {
                return;
            }

            _config.Items.Add(new DockItem
            {
                Id = SettingsService.NewId(),
                Type = "separator",
                Name = "\u5206\u9694\u7b26"
            });
            SaveConfig();
            RenderDock();
            PositionWindow();
        }

        public void AddDockItem(string type, string name, string target, int? insertIndex)
        {
            if (!_config.Dock.EditMode)
            {
                return;
            }

            var id = SettingsService.NewId();
            var itemTarget = target;
            if (type == "app" && string.Equals(Path.GetExtension(target), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    itemTarget = _settingsService.ImportShortcut(target, id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(T("shortcutImportFailed") + ex.Message, "TidyDock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var item = new DockItem
            {
                Id = id,
                Type = type,
                Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileName(target) : name,
                Target = itemTarget
            };

            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= _config.Items.Count)
            {
                _config.Items.Insert(insertIndex.Value, item);
            }
            else
            {
                _config.Items.Add(item);
            }

            SaveConfig();
            RenderDock();
            PositionWindow();
        }

        public void AddPath(string path, int? insertIndex)
        {
            if (!_config.Dock.EditMode)
            {
                return;
            }

            if (Directory.Exists(path))
            {
                AddDockItem("folder", Path.GetFileName(path), path, insertIndex);
                return;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".exe" || extension == ".lnk")
            {
                AddDockItem("app", Path.GetFileNameWithoutExtension(path), path, insertIndex);
            }
            else
            {
                AddDockItem("file", Path.GetFileName(path), path, insertIndex);
            }
        }

        private void OnDockButtonMouseMove(object sender, MouseEventArgs e)
        {
            if (!_config.Dock.EditMode)
            {
                return;
            }

            if (_pressedItem == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var point = e.GetPosition(this);
            if (Math.Abs(point.X - _pressPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(point.Y - _pressPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var item = _pressedItem;
                var button = sender as Button;
                _pressedItem = null;
                if (item == null)
                {
                    return;
                }
                StartDockDrag(button, item);
                try
                {
                    DragDrop.DoDragDrop((DependencyObject)sender, new DataObject("TidyDockItemId", item.Id), DragDropEffects.Move);
                }
                finally
                {
                    EndDockDrag();
                }
            }
        }

        private void StartDockDrag(Button sourceButton, DockItem item)
        {
            if (item == null)
            {
                return;
            }

            _dragSourceButton = sourceButton;
            if (_dragSourceButton != null)
            {
                _dragSourceButton.Opacity = 0.24;
            }

            CreateDockDragGhost(item);
            UpdateDockDragGhost();
        }

        private void EndDockDrag()
        {
            if (_dragSourceButton != null)
            {
                _dragSourceButton.Opacity = 1.0;
                _dragSourceButton = null;
            }

            if (_dragGhostWindow != null)
            {
                _dragGhostWindow.Close();
                _dragGhostWindow = null;
            }
        }

        private void CreateDockDragGhost(DockItem item)
        {
            if (item == null)
            {
                return;
            }

            var size = _config.Dock.IconSize;
            var image = new Image();
            image.Width = Math.Max(24, size - 8);
            image.Height = Math.Max(24, size - 8);
            image.Stretch = Stretch.Uniform;
            image.Source = _iconCache.GetIcon(item, size);
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            var border = new Border();
            border.Width = size;
            border.Height = size;
            border.Background = Brushes.Transparent;
            border.Child = image;

            _dragGhostWindow = new Window();
            _dragGhostWindow.WindowStyle = WindowStyle.None;
            _dragGhostWindow.AllowsTransparency = true;
            _dragGhostWindow.Background = Brushes.Transparent;
            _dragGhostWindow.ResizeMode = ResizeMode.NoResize;
            _dragGhostWindow.ShowInTaskbar = false;
            _dragGhostWindow.ShowActivated = false;
            _dragGhostWindow.Topmost = true;
            _dragGhostWindow.IsHitTestVisible = false;
            _dragGhostWindow.Width = size;
            _dragGhostWindow.Height = size;
            _dragGhostWindow.Opacity = 0.84;
            _dragGhostWindow.Content = border;
            _dragGhostWindow.SourceInitialized += delegate { MakeDragGhostClickThrough(_dragGhostWindow); };
            _dragGhostWindow.Show();
        }

        private void MakeDragGhostClickThrough(Window window)
        {
            if (window == null)
            {
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var style = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        }

        private void HideFromAltTab(Window window)
        {
            if (window == null)
            {
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var style = GetWindowLong(handle, GWL_EXSTYLE);
            style = (style | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
            SetWindowLong(handle, GWL_EXSTYLE, style);
        }

        private void OnDockGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (_dragGhostWindow == null)
            {
                return;
            }

            UpdateDockDragGhost();
            e.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }

        private void UpdateDockDragGhost()
        {
            if (_dragGhostWindow == null)
            {
                return;
            }

            POINT cursor;
            if (!GetCursorPos(out cursor))
            {
                return;
            }

            var point = new Point(cursor.X, cursor.Y);
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                point = source.CompositionTarget.TransformFromDevice.Transform(point);
            }

            _dragGhostWindow.Left = point.X - _dragGhostWindow.Width / 2;
            _dragGhostWindow.Top = point.Y - _dragGhostWindow.Height / 2;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _folderPanel != null && _folderPanel.IsOpen)
            {
                _folderPanel.Close();
                e.Handled = true;
            }
        }

        private FolderPanel EnsureFolderPanel()
        {
            if (_folderPanel == null)
            {
                _folderPanel = new FolderPanel(this, _iconCache, _config);
                _folderPanel.ApplySettings(_palette);
            }

            return _folderPanel;
        }

        private void OnDockDragOver(object sender, DragEventArgs e)
        {
            if (!_config.Dock.EditMode)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent("TidyDockItemId"))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void OnDockDrop(object sender, DragEventArgs e)
        {
            if (!_config.Dock.EditMode)
            {
                e.Handled = true;
                return;
            }

            var index = GetDropIndex(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    foreach (var path in files)
                    {
                        AddPath(path, index);
                        if (index.HasValue)
                        {
                            index = index.Value + 1;
                        }
                    }
                }
            }
            else if (e.Data.GetDataPresent("TidyDockItemId"))
            {
                var id = e.Data.GetData("TidyDockItemId") as string;
                MoveItem(id, index);
            }

            e.Handled = true;
        }

        private int? GetDropIndex(DragEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                var button = source as Button;
                if (button != null && button.Tag is DockItem)
                {
                    var item = (DockItem)button.Tag;
                    var baseIndex = _config.Items.IndexOf(item);
                    if (baseIndex < 0)
                    {
                        return null;
                    }

                    var point = e.GetPosition(button);
                    var after = IsVertical() ? point.Y > button.ActualHeight / 2 : point.X > button.ActualWidth / 2;
                    return after ? baseIndex + 1 : baseIndex;
                }
                source = VisualTreeHelper.GetParent(source);
            }
            return null;
        }

        private void MoveItem(string id, int? index)
        {
            if (!_config.Dock.EditMode)
            {
                return;
            }

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var item = _config.Items.FirstOrDefault(delegate(DockItem candidate) { return candidate.Id == id; });
            if (item == null)
            {
                return;
            }

            var oldIndex = _config.Items.IndexOf(item);
            _config.Items.Remove(item);

            var newIndex = index.HasValue ? index.Value : _config.Items.Count;
            if (newIndex > oldIndex)
            {
                newIndex--;
            }
            if (newIndex < 0)
            {
                newIndex = 0;
            }
            if (newIndex > _config.Items.Count)
            {
                newIndex = _config.Items.Count;
            }

            _config.Items.Insert(newIndex, item);
            SaveConfig();
            RenderDock();
            PositionWindow();
        }

        private bool RequireEditMode()
        {
            if (_config.Dock.EditMode)
            {
                return true;
            }

            MessageBox.Show(T("editModeRequired"), "TidyDock", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private void AnimateDockItem(Button button, bool active)
        {
            var group = button.RenderTransform as TransformGroup;
            if (group == null)
            {
                group = new TransformGroup();
                group.Children.Add(new ScaleTransform(1, 1));
                group.Children.Add(new TranslateTransform(0, 0));
                button.RenderTransform = group;
            }

            var scale = (ScaleTransform)group.Children[0];
            var translate = (TranslateTransform)group.Children[1];
            var toScale = active ? _config.Dock.Magnification : 1.0;
            double toX = 0;
            double toY = 0;

            if (active)
            {
                if (_config.Dock.Position == "top")
                {
                    toY = 10;
                }
                else if (_config.Dock.Position == "left")
                {
                    toX = 10;
                }
                else if (_config.Dock.Position == "right")
                {
                    toX = -10;
                }
                else
                {
                    toY = -10;
                }
            }

            var duration = TimeSpan.FromMilliseconds(120);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(toScale, duration));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(toScale, duration));
            translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(toX, duration));
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(toY, duration));
        }

        private ControlTemplate CreatePlainButtonTemplate()
        {
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

            var template = new ControlTemplate(typeof(Button));
            template.VisualTree = presenter;
            return template;
        }

        private void PositionWindow()
        {
            UpdateLayout();
            var work = GetDockWorkArea();
            var margin = 18.0;
            if (_config.Dock.Position == "top")
            {
                Left = work.Left + (work.Width - ActualWidth) / 2;
                Top = work.Top + margin;
            }
            else if (_config.Dock.Position == "left")
            {
                Left = work.Left + margin;
                Top = work.Top + (work.Height - ActualHeight) / 2;
            }
            else if (_config.Dock.Position == "right")
            {
                Left = work.Right - ActualWidth - margin;
                Top = work.Top + (work.Height - ActualHeight) / 2;
            }
            else
            {
                Left = work.Left + (work.Width - ActualWidth) / 2;
                Top = work.Bottom - ActualHeight - margin;
            }
        }

        private bool IsVertical()
        {
            return _config.Dock.Position == "left" || _config.Dock.Position == "right";
        }

        private void HideForAutoHide()
        {
            Hide();
            UpdateHotZone();
            if (_hotZone != null)
            {
                _hotZone.Show();
            }
        }

        private void ShowFromAutoHide()
        {
            if (_hotZone != null)
            {
                _hotZone.Hide();
            }
            Show();
            PositionWindow();
        }

        private void UpdateHotZone()
        {
            if (_hotZone == null)
            {
                _hotZone = new Window();
                _hotZone.WindowStyle = WindowStyle.None;
                _hotZone.AllowsTransparency = true;
                _hotZone.Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255));
                _hotZone.ResizeMode = ResizeMode.NoResize;
                _hotZone.ShowInTaskbar = false;
                _hotZone.Topmost = true;
                _hotZone.SourceInitialized += delegate { HideFromAltTab(_hotZone); };
                _hotZone.MouseEnter += delegate { ShowFromAutoHide(); };
            }

            var work = GetDockWorkArea();
            if (_config.Dock.Position == "top")
            {
                _hotZone.Left = work.Left;
                _hotZone.Top = work.Top;
                _hotZone.Width = work.Width;
                _hotZone.Height = 4;
            }
            else if (_config.Dock.Position == "left")
            {
                _hotZone.Left = work.Left;
                _hotZone.Top = work.Top;
                _hotZone.Width = 4;
                _hotZone.Height = work.Height;
            }
            else if (_config.Dock.Position == "right")
            {
                _hotZone.Left = work.Right - 4;
                _hotZone.Top = work.Top;
                _hotZone.Width = 4;
                _hotZone.Height = work.Height;
            }
            else
            {
                _hotZone.Left = work.Left;
                _hotZone.Top = work.Bottom - 4;
                _hotZone.Width = work.Width;
                _hotZone.Height = 4;
            }

            if (!_config.Dock.AutoHide)
            {
                _hotZone.Hide();
            }
        }

        private Rect GetDockWorkArea()
        {
            var screens = Forms.Screen.AllScreens;
            Forms.Screen selected = null;
            if (!string.IsNullOrEmpty(_config.Dock.Display) && _config.Dock.Display != "primary")
            {
                foreach (var screen in screens)
                {
                    if (screen.DeviceName == _config.Dock.Display)
                    {
                        selected = screen;
                        break;
                    }
                }
            }

            if (selected == null)
            {
                selected = Forms.Screen.PrimaryScreen;
            }

            var area = selected.WorkingArea;
            return new Rect(area.Left, area.Top, area.Width, area.Height);
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
