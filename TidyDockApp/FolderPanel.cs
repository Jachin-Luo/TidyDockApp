using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace TidyDock
{
    public class FolderPanel
    {
        private readonly Popup _popup;
        private readonly Border _border;
        private readonly DockPanel _header;
        private readonly TextBlock _title;
        private readonly ScrollViewer _scroll;
        private readonly StackPanel _itemsPanel;
        private readonly Button _backButton;
        private readonly Button _explorerButton;
        private readonly Button _closeButton;
        private readonly DispatcherTimer _autoCloseTimer;
        private readonly IconCacheService _iconCache;
        private readonly DockConfig _config;
        private readonly Stack<string> _history;
        private ThemePalette _palette;
        private string _currentPath;
        private int _loadVersion;

        public FolderPanel(Window owner, IconCacheService iconCache, DockConfig config)
        {
            _iconCache = iconCache;
            _config = config;
            _history = new Stack<string>();
            _palette = ThemeService.GetPalette(_config.Dock.Theme);

            _popup = new Popup();
            _popup.Placement = PlacementMode.Top;
            _popup.AllowsTransparency = true;
            _popup.StaysOpen = false;

            _border = new Border();
            _border.Width = 420;
            _border.CornerRadius = new CornerRadius(8);
            _border.BorderThickness = new Thickness(1);
            _border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 34,
                ShadowDepth = 8,
                Opacity = 0.26
            };

            var root = new DockPanel();
            _border.Child = root;

            _header = new DockPanel();
            _header.Height = 42;
            _header.LastChildFill = true;
            DockPanel.SetDock(_header, Dock.Top);
            root.Children.Add(_header);

            _backButton = MakeHeaderButton("<");
            _backButton.ToolTip = T("back");
            _backButton.Click += delegate { GoBack(); };
            DockPanel.SetDock(_backButton, Dock.Left);
            _header.Children.Add(_backButton);

            var rightButtons = new StackPanel();
            rightButtons.Orientation = Orientation.Horizontal;
            DockPanel.SetDock(rightButtons, Dock.Right);
            _header.Children.Add(rightButtons);

            _explorerButton = MakeHeaderButton("...");
            _explorerButton.ToolTip = T("openInExplorer");
            _explorerButton.Click += delegate { OpenInExplorer(); };
            rightButtons.Children.Add(_explorerButton);

            _closeButton = MakeHeaderButton("x");
            _closeButton.ToolTip = T("close");
            _closeButton.Click += delegate { Close(); };
            rightButtons.Children.Add(_closeButton);

            _title = new TextBlock();
            _title.VerticalAlignment = VerticalAlignment.Center;
            _title.FontSize = 13;
            _title.FontWeight = FontWeights.SemiBold;
            _title.TextTrimming = TextTrimming.CharacterEllipsis;
            _title.Margin = new Thickness(4, 0, 8, 0);
            _header.Children.Add(_title);

            _scroll = new ScrollViewer();
            _scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _scroll.Padding = new Thickness(8);
            root.Children.Add(_scroll);

            _itemsPanel = new StackPanel();
            _scroll.Content = _itemsPanel;

            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Interval = TimeSpan.FromMilliseconds(300);
            _autoCloseTimer.Tick += delegate
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _border.MouseEnter += delegate { _autoCloseTimer.Stop(); };
            _border.MouseLeave += delegate
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer.Start();
            };
            _popup.Closed += delegate { _autoCloseTimer.Stop(); };

            _popup.Child = _border;
            ApplySettings(_palette);
        }

        public void ApplySettings(ThemePalette palette)
        {
            _palette = palette ?? ThemeService.GetPalette(_config.Dock.Theme);
            _border.MaxHeight = _config.FolderPanel.MaxHeight;
            _scroll.MaxHeight = Math.Max(160, _config.FolderPanel.MaxHeight - 42);
            _border.BorderBrush = ThemeService.Brush(_palette.PanelBorder);
            _border.Background = ThemeService.Brush(_palette.PanelBackground, 0.92);
            _header.Background = ThemeService.Brush(_palette.PanelHeader, _palette.IsDark ? 0.7 : 0.36);
            _title.Foreground = ThemeService.Brush(_palette.Text);
            _backButton.ToolTip = T("back");
            _explorerButton.ToolTip = T("openInExplorer");
            _closeButton.ToolTip = T("close");
            StyleHeaderButton(_backButton);
            StyleHeaderButton(_explorerButton);
            StyleHeaderButton(_closeButton);
        }

        public void Open(string path, UIElement placementTarget)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _history.Clear();
            _popup.PlacementTarget = placementTarget;
            if (_config.Dock.Position == "top")
            {
                _popup.Placement = PlacementMode.Bottom;
            }
            else if (_config.Dock.Position == "left")
            {
                _popup.Placement = PlacementMode.Right;
            }
            else if (_config.Dock.Position == "right")
            {
                _popup.Placement = PlacementMode.Left;
            }
            else
            {
                _popup.Placement = PlacementMode.Top;
            }
            Load(path);
            _popup.IsOpen = true;
        }

        public void Close()
        {
            _loadVersion++;
            _popup.IsOpen = false;
            _itemsPanel.Children.Clear();
            _history.Clear();
        }

        public bool IsOpen
        {
            get { return _popup.IsOpen; }
        }

        private void GoBack()
        {
            if (_history.Count == 0)
            {
                return;
            }

            Load(_history.Pop());
        }

        private void OpenInExplorer()
        {
            if (Directory.Exists(_currentPath))
            {
                Process.Start(new ProcessStartInfo(_currentPath) { UseShellExecute = true });
            }
        }

        private async void Load(string path)
        {
            var loadVersion = ++_loadVersion;
            _currentPath = path;
            _title.Text = Path.GetFileName(path);
            if (string.IsNullOrEmpty(_title.Text))
            {
                _title.Text = path;
            }
            _backButton.IsEnabled = _history.Count > 0;
            _itemsPanel.Children.Clear();
            _itemsPanel.Children.Add(MakeStatus(T("loading")));

            var result = await Task.Run(delegate { return ReadEntries(path); });
            if (loadVersion != _loadVersion || !_popup.IsOpen || _currentPath != path)
            {
                return;
            }

            _itemsPanel.Children.Clear();

            if (result.Error != null)
            {
                _itemsPanel.Children.Add(MakeStatus(result.Error));
                return;
            }

            foreach (var entry in result.Entries)
            {
                if (entry.IsOverflowHint)
                {
                    _itemsPanel.Children.Add(MakeStatus(entry.Name));
                }
                else
                {
                    _itemsPanel.Children.Add(MakeEntryButton(entry));
                }
            }
        }

        private ReadResult ReadEntries(string path)
        {
            var result = new ReadResult();
            result.Entries = new List<FolderEntry>();

            try
            {
                if (!Directory.Exists(path))
                {
                    result.Error = T("folderNotFound");
                    return result;
                }

                var directory = new DirectoryInfo(path);
                var max = _config.FolderPanel.MaxItems;
                var items = directory.GetFileSystemInfos()
                    .Where(delegate(FileSystemInfo info)
                    {
                        if (_config.FolderPanel.ShowHiddenFiles)
                        {
                            return true;
                        }
                        return (info.Attributes & FileAttributes.Hidden) == 0;
                    })
                    .OrderByDescending(delegate(FileSystemInfo info)
                    {
                        return (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                    })
                    .ThenBy(delegate(FileSystemInfo info) { return info.Name; }, StringComparer.CurrentCultureIgnoreCase)
                    .Take(max + 1)
                    .ToList();

                var count = 0;
                foreach (var info in items)
                {
                    if (count >= max)
                    {
                        result.Entries.Add(new FolderEntry
                        {
                            Name = T("tooManyItemsPrefix") + max + T("tooManyItemsSuffix"),
                            IsOverflowHint = true
                        });
                        break;
                    }

                    result.Entries.Add(new FolderEntry
                    {
                        Name = info.Name,
                        FullName = info.FullName,
                        IsDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory
                    });
                    count++;
                }
            }
            catch (UnauthorizedAccessException)
            {
                result.Error = T("folderAccessDenied");
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        private UIElement MakeEntryButton(FolderEntry entry)
        {
            var button = new Button();
            button.Height = 30;
            button.Margin = new Thickness(0, 1, 0, 1);
            button.Padding = new Thickness(10, 0, 10, 0);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.Background = Brushes.Transparent;
            button.Foreground = ThemeService.Brush(_palette.Text);
            button.BorderThickness = new Thickness(0);
            button.ToolTip = entry.Name;
            button.Template = CreatePlainButtonTemplate();

            button.MouseEnter += delegate { button.Background = ThemeService.Brush(_palette.TileHover, _palette.IsDark ? 0.28 : 0.38); };
            button.MouseLeave += delegate { button.Background = Brushes.Transparent; };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var image = new Image();
            image.Width = 18;
            image.Height = 18;
            image.Stretch = Stretch.Uniform;
            image.VerticalAlignment = VerticalAlignment.Center;
            image.Margin = new Thickness(0, 0, 6, 0);
            image.SnapsToDevicePixels = true;
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            image.Source = _iconCache.GetPathIcon(entry.FullName, entry.IsDirectory, 18);
            Grid.SetColumn(image, 0);
            row.Children.Add(image);

            var text = new TextBlock();
            text.Text = entry.Name;
            text.FontSize = 12;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.TextTrimming = TextTrimming.CharacterEllipsis;
            text.Foreground = ThemeService.Brush(_palette.Text);
            if (entry.IsDirectory)
            {
                text.FontWeight = FontWeights.SemiBold;
            }
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            button.Content = row;

            button.Click += delegate
            {
                if (entry.IsDirectory)
                {
                    _history.Push(_currentPath);
                    Load(entry.FullName);
                }
                else
                {
                    Close();
                    Process.Start(new ProcessStartInfo(entry.FullName) { UseShellExecute = true });
                }
            };

            return button;
        }

        private UIElement MakeStatus(string text)
        {
            var block = new TextBlock();
            block.Width = 380;
            block.Margin = new Thickness(8);
            block.Text = text;
            block.FontSize = 12;
            block.TextWrapping = TextWrapping.Wrap;
            block.Foreground = ThemeService.Brush(_palette.MutedText);
            return block;
        }

        private Button MakeHeaderButton(string text)
        {
            var button = new Button();
            button.Content = text;
            button.Width = 30;
            button.Height = 28;
            button.Margin = new Thickness(6, 6, 0, 6);
            button.Padding = new Thickness(0);
            button.FontSize = 14;
            button.BorderThickness = new Thickness(0);
            button.HorizontalContentAlignment = HorizontalAlignment.Center;
            button.VerticalContentAlignment = VerticalAlignment.Center;
            button.Template = CreatePlainButtonTemplate();
            StyleHeaderButton(button);
            return button;
        }

        private ControlTemplate CreatePlainButtonTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new System.Windows.Data.Binding("HorizontalContentAlignment") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, new System.Windows.Data.Binding("VerticalContentAlignment") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.AppendChild(presenter);

            var template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            return template;
        }

        private string T(string key)
        {
            return LocalizationService.T(_config, key);
        }

        private void StyleHeaderButton(Button button)
        {
            if (button == null || _palette == null)
            {
                return;
            }

            button.Background = ThemeService.Brush(_palette.ControlBackground, _palette.IsDark ? 0.42 : 0.52);
            button.Foreground = ThemeService.Brush(_palette.Text);
        }

        private class ReadResult
        {
            public List<FolderEntry> Entries { get; set; }
            public string Error { get; set; }
        }
    }
}
