using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace TidyDock
{
    public class SettingsWindow : Window
    {
        private readonly MainWindow _mainWindow;
        private readonly DockConfig _config;
        private ListBox _itemsList;
        private ScrollViewer _root;
        private ThemePalette _palette;
        private bool _ready;

        public SettingsWindow(MainWindow mainWindow, DockConfig config)
        {
            _mainWindow = mainWindow;
            _config = config;
            _palette = ThemeService.GetPalette(_config.Dock.Theme);

            Title = T("settingsTitle");
            Width = 500;
            Height = 720;
            ResizeMode = ResizeMode.CanResize;
            MinWidth = 440;
            MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ApplyWindowTheme();

            _root = new ScrollViewer();
            _root.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            var panel = new StackPanel();
            panel.Margin = new Thickness(18, 16, 18, 18);
            _root.Content = panel;
            Content = _root;

            AddAppearanceSettings(MakeSection(panel, T("appearance")));

            AddBehaviorSettings(MakeSection(panel, T("behavior")));

            AddItemManager(MakeSection(panel, T("dockItems")));

            var close = MakeSmallButton(T("close"), delegate { Close(); });
            close.Tag = "primary";
            close.Height = 34;
            close.MinWidth = 96;
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.Margin = new Thickness(0, 14, 0, 0);
            panel.Children.Add(close);

            RefreshItems();
            ApplyWindowTheme();
            _ready = true;
            PreviewKeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs e)
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };
        }

        private void AddAppearanceSettings(Panel panel)
        {
            var theme = new ComboBox();
            theme.Items.Add(new ThemeOption("system", "\u8ddf\u968f\u7cfb\u7edf"));
            theme.Items.Add(new ThemeOption("light", "\u6d45\u8272"));
            theme.Items.Add(new ThemeOption("dark", "\u6df1\u8272"));
            SelectTheme(theme);
            theme.SelectionChanged += delegate
            {
                if (!_ready) return;
                var option = theme.SelectedItem as ThemeOption;
                if (option != null)
                {
                    _config.Dock.Theme = option.Value;
                    ApplyWindowTheme();
                    Apply();
                }
            };
            panel.Children.Add(MakeField("\u4e3b\u9898", theme));

            var display = new ComboBox();
            display.Items.Add(new DisplayOption("primary", "\u4e3b\u663e\u793a\u5668"));
            foreach (var screen in Forms.Screen.AllScreens)
            {
                display.Items.Add(new DisplayOption(screen.DeviceName, screen.DeviceName + " " + screen.Bounds.Width + "x" + screen.Bounds.Height));
            }
            SelectDisplay(display);
            display.SelectionChanged += delegate
            {
                if (!_ready) return;
                var option = display.SelectedItem as DisplayOption;
                if (option != null)
                {
                    _config.Dock.Display = option.Value;
                    Apply();
                }
            };
            panel.Children.Add(MakeField("\u663e\u793a\u5668", display));

            var position = new ComboBox();
            position.Items.Add("bottom");
            position.Items.Add("top");
            position.Items.Add("left");
            position.Items.Add("right");
            position.SelectedItem = _config.Dock.Position;
            position.SelectionChanged += delegate
            {
                if (!_ready) return;
                _config.Dock.Position = position.SelectedItem as string;
                Apply();
            };
            panel.Children.Add(MakeField("\u4f4d\u7f6e", position));

            var size = MakeSlider(36, 76, _config.Dock.IconSize);
            size.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.Dock.IconSize = (int)size.Value;
                Apply();
            };
            panel.Children.Add(MakeField("\u56fe\u6807\u5927\u5c0f", size));

            var gap = MakeSlider(0, 24, _config.Dock.IconGap);
            gap.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.Dock.IconGap = (int)gap.Value;
                Apply();
            };
            panel.Children.Add(MakeField("\u56fe\u6807\u95f4\u8ddd", gap));

            var opacity = MakeSlider(0, 100, _config.Dock.Opacity * 100);
            opacity.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.Dock.Opacity = opacity.Value / 100.0;
                Apply();
            };
            panel.Children.Add(MakeField("\u900f\u660e\u5ea6", opacity));

            var radius = MakeSlider(6, 34, _config.Dock.CornerRadius);
            radius.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.Dock.CornerRadius = (int)radius.Value;
                Apply();
            };
            panel.Children.Add(MakeField("\u5706\u89d2", radius));

            var magnify = MakeSlider(100, 180, _config.Dock.Magnification * 100);
            magnify.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.Dock.Magnification = magnify.Value / 100.0;
                Apply();
            };
            panel.Children.Add(MakeField("\u653e\u5927\u500d\u7387", magnify));

            var showLabels = new CheckBox();
            showLabels.Content = T("showItemLabels");
            showLabels.IsChecked = _config.Dock.ShowItemLabels;
            showLabels.Margin = new Thickness(0, 4, 0, 8);
            showLabels.Checked += delegate { if (_ready) { _config.Dock.ShowItemLabels = true; Apply(); } };
            showLabels.Unchecked += delegate { if (_ready) { _config.Dock.ShowItemLabels = false; Apply(); } };
            panel.Children.Add(showLabels);
        }

        private void AddBehaviorSettings(Panel panel)
        {
            var language = new ComboBox();
            language.Items.Add(new LanguageOption("zh-CN", "\u7b80\u4f53\u4e2d\u6587"));
            language.Items.Add(new LanguageOption("en-US", "English"));
            SelectLanguage(language);
            language.SelectionChanged += delegate
            {
                if (!_ready) return;
                var option = language.SelectedItem as LanguageOption;
                if (option != null)
                {
                    _config.Dock.Language = option.Value;
                    Apply();
                }
            };
            panel.Children.Add(MakeField(T("language"), language));

            var autoHide = new CheckBox();
            autoHide.Content = T("autoHide");
            autoHide.IsChecked = _config.Dock.AutoHide;
            autoHide.Margin = new Thickness(0, 4, 0, 8);
            autoHide.Checked += delegate { if (_ready) { _config.Dock.AutoHide = true; Apply(); } };
            autoHide.Unchecked += delegate { if (_ready) { _config.Dock.AutoHide = false; Apply(); } };
            panel.Children.Add(autoHide);

            var editMode = new CheckBox();
            editMode.Content = T("editMode");
            editMode.IsChecked = _config.Dock.EditMode;
            editMode.Margin = new Thickness(0, 4, 0, 8);
            editMode.Checked += delegate { if (_ready) { _config.Dock.EditMode = true; Apply(); } };
            editMode.Unchecked += delegate { if (_ready) { _config.Dock.EditMode = false; Apply(); } };
            panel.Children.Add(editMode);

            var startVisible = new CheckBox();
            startVisible.Content = T("startVisible");
            startVisible.IsChecked = _config.Dock.StartVisible;
            startVisible.Margin = new Thickness(0, 4, 0, 8);
            startVisible.Checked += delegate { if (_ready) { _config.Dock.StartVisible = true; Apply(); } };
            startVisible.Unchecked += delegate { if (_ready) { _config.Dock.StartVisible = false; Apply(); } };
            panel.Children.Add(startVisible);

            var trayIcon = new CheckBox();
            trayIcon.Content = T("showTrayIcon");
            trayIcon.IsChecked = _config.Dock.ShowTrayIcon;
            trayIcon.Margin = new Thickness(0, 4, 0, 8);
            trayIcon.Checked += delegate
            {
                if (_ready)
                {
                    _mainWindow.SetTrayIconVisible(true);
                }
            };
            trayIcon.Unchecked += delegate
            {
                if (!_ready) return;
                if (!_config.Dock.StartVisible)
                {
                    MessageBox.Show(
                        T("traySafety"),
                        "TidyDock",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    trayIcon.IsChecked = true;
                    return;
                }
                _mainWindow.SetTrayIconVisible(false);
            };
            panel.Children.Add(trayIcon);

            var topMost = new CheckBox();
            topMost.Content = T("alwaysOnTop");
            topMost.IsChecked = _config.Dock.AlwaysOnTop;
            topMost.Margin = new Thickness(0, 4, 0, 8);
            topMost.Checked += delegate { if (_ready) { _config.Dock.AlwaysOnTop = true; Apply(); } };
            topMost.Unchecked += delegate { if (_ready) { _config.Dock.AlwaysOnTop = false; Apply(); } };
            panel.Children.Add(topMost);

            var startup = new CheckBox();
            startup.Content = T("startWithWindows");
            startup.IsChecked = StartupService.IsEnabled();
            startup.Margin = new Thickness(0, 4, 0, 8);
            startup.Checked += delegate
            {
                if (_ready)
                {
                    _config.Dock.StartWithWindows = true;
                    StartupService.SetEnabled(true);
                    Apply();
                }
            };
            startup.Unchecked += delegate
            {
                if (_ready)
                {
                    _config.Dock.StartWithWindows = false;
                    StartupService.SetEnabled(false);
                    Apply();
                }
            };
            panel.Children.Add(startup);

            var maxItems = MakeSlider(50, 600, _config.FolderPanel.MaxItems);
            maxItems.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.FolderPanel.MaxItems = (int)maxItems.Value;
                Apply();
            };
            panel.Children.Add(MakeField("\u6587\u4ef6\u5939\u6700\u5927\u5c55\u793a\u6570", maxItems));

            var maxHeight = MakeSlider(260, 640, _config.FolderPanel.MaxHeight);
            maxHeight.ValueChanged += delegate
            {
                if (!_ready) return;
                _config.FolderPanel.MaxHeight = (int)maxHeight.Value;
                Apply();
            };
            panel.Children.Add(MakeField("\u6587\u4ef6\u5939\u9762\u677f\u6700\u5927\u9ad8\u5ea6", maxHeight));

            var hidden = new CheckBox();
            hidden.Content = "\u663e\u793a\u9690\u85cf\u6587\u4ef6";
            hidden.IsChecked = _config.FolderPanel.ShowHiddenFiles;
            hidden.Margin = new Thickness(0, 4, 0, 8);
            hidden.Checked += delegate { if (_ready) { _config.FolderPanel.ShowHiddenFiles = true; Apply(); } };
            hidden.Unchecked += delegate { if (_ready) { _config.FolderPanel.ShowHiddenFiles = false; Apply(); } };
            panel.Children.Add(hidden);

            var maintenance = new WrapPanel();
            maintenance.Margin = new Thickness(0, 8, 0, 8);
            maintenance.Children.Add(MakeSmallButton(T("openConfigFolder"), OpenConfigFolder));
            maintenance.Children.Add(MakeSmallButton(T("openLogFolder"), OpenLogFolder));
            maintenance.Children.Add(MakeSmallButton(T("clearIconCache"), ClearIconCache));
            maintenance.Children.Add(MakeSmallButton(T("resetConfig"), ResetConfig));
            maintenance.Children.Add(MakeSmallButton(T("about"), ShowAbout));
            maintenance.Children.Add(MakeSmallButton(T("exitApp"), ExitApp));
            panel.Children.Add(maintenance);
        }

        private void AddItemManager(Panel panel)
        {
            _itemsList = new ListBox();
            _itemsList.Height = 168;
            _itemsList.Margin = new Thickness(0, 0, 0, 8);
            panel.Children.Add(_itemsList);

            var addRow = new WrapPanel();
            addRow.Margin = new Thickness(0, 0, 0, 8);
            addRow.Children.Add(MakeSmallButton(T("addAppFile"), AddAppOrFile));
            addRow.Children.Add(MakeSmallButton(T("addFolder"), AddFolder));
            addRow.Children.Add(MakeSmallButton(T("addUrl"), AddUrl));
            addRow.Children.Add(MakeSmallButton(T("addSeparator"), AddSeparator));
            panel.Children.Add(addRow);

            var editRow = new WrapPanel();
            editRow.Margin = new Thickness(0, 0, 0, 8);
            editRow.Children.Add(MakeSmallButton(T("moveUp"), MoveUp));
            editRow.Children.Add(MakeSmallButton(T("moveDown"), MoveDown));
            editRow.Children.Add(MakeSmallButton(T("rename"), EditName));
            editRow.Children.Add(MakeSmallButton(T("editTarget"), EditTarget));
            editRow.Children.Add(MakeSmallButton(T("changeIcon"), ChangeIcon));
            editRow.Children.Add(MakeSmallButton(T("remove"), RemoveSelected));
            panel.Children.Add(editRow);
        }

        private Button MakeSmallButton(string text, RoutedEventHandler click)
        {
            var button = new Button();
            button.Content = text;
            button.Height = 30;
            button.MinWidth = 72;
            button.Margin = new Thickness(0, 0, 6, 6);
            button.Padding = new Thickness(10, 0, 10, 0);
            button.Click += click;
            StyleButton(button);
            return button;
        }

        private void AddAppOrFile(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.Filter = T("appFileFilter");
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                foreach (var path in dialog.FileNames)
                {
                    _mainWindow.AddPath(path, null);
                }
                RefreshItems();
            }
        }

        private void AddFolder(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            using (var dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = "\u9009\u62e9\u8981\u653e\u5165 Dock \u7684\u6587\u4ef6\u5939";
                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    _mainWindow.AddPath(dialog.SelectedPath, null);
                    RefreshItems();
                }
            }
        }

        private void AddUrl(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var urlDialog = new InputDialog(_config, T("addUrl"), "URL", "https://");
            urlDialog.Owner = this;
            if (urlDialog.ShowDialog() != true)
            {
                return;
            }

            var nameDialog = new InputDialog(_config, T("addUrl"), T("name"), urlDialog.Value);
            nameDialog.Owner = this;
            if (nameDialog.ShowDialog() == true)
            {
                _mainWindow.AddDockItem("url", nameDialog.Value, urlDialog.Value, null);
                RefreshItems();
            }
        }

        private void AddSeparator(object sender, RoutedEventArgs e)
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
            ApplyAndRefresh();
        }

        private void MoveUp(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var index = _itemsList.SelectedIndex;
            if (index <= 0)
            {
                return;
            }

            var item = _config.Items[index];
            _config.Items.RemoveAt(index);
            _config.Items.Insert(index - 1, item);
            ApplyAndRefresh(index - 1);
        }

        private void MoveDown(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var index = _itemsList.SelectedIndex;
            if (index < 0 || index >= _config.Items.Count - 1)
            {
                return;
            }

            var item = _config.Items[index];
            _config.Items.RemoveAt(index);
            _config.Items.Insert(index + 1, item);
            ApplyAndRefresh(index + 1);
        }

        private void EditName(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            var dialog = new InputDialog(_config, T("rename"), T("name"), item.Name);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                item.Name = dialog.Value;
                ApplyAndRefresh(_itemsList.SelectedIndex);
            }
        }

        private void EditTarget(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var item = GetSelectedItem();
            if (item == null || item.Type == "separator" || item.Type == "settings")
            {
                return;
            }

            var dialog = new InputDialog(_config, T("editTarget"), T("targetPathOrUrl"), item.Target);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                item.Target = dialog.Value;
                ApplyAndRefresh(_itemsList.SelectedIndex);
            }
        }

        private void ChangeIcon(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var item = GetSelectedItem();
            if (item == null || item.Type == "separator")
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.Filter = T("iconFilter");
            if (dialog.ShowDialog() == true)
            {
                item.Icon = dialog.FileName;
                ApplyAndRefresh(_itemsList.SelectedIndex);
            }
        }

        private void RemoveSelected(object sender, RoutedEventArgs e)
        {
            if (!RequireEditMode())
            {
                return;
            }

            var index = _itemsList.SelectedIndex;
            if (index < 0 || index >= _config.Items.Count)
            {
                return;
            }

            _config.Items.RemoveAt(index);
            ApplyAndRefresh(Math.Min(index, _config.Items.Count - 1));
        }

        private DockItem GetSelectedItem()
        {
            var index = _itemsList.SelectedIndex;
            if (index < 0 || index >= _config.Items.Count)
            {
                return null;
            }
            return _config.Items[index];
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

        private void ApplyAndRefresh()
        {
            ApplyAndRefresh(-1);
        }

        private void ApplyAndRefresh(int selectedIndex)
        {
            Apply();
            RefreshItems();
            if (selectedIndex >= 0 && selectedIndex < _itemsList.Items.Count)
            {
                _itemsList.SelectedIndex = selectedIndex;
            }
        }

        private void RefreshItems()
        {
            if (_itemsList == null)
            {
                return;
            }

            var selectedIndex = _itemsList.SelectedIndex;
            _itemsList.Items.Clear();
            foreach (var item in _config.Items)
            {
                _itemsList.Items.Add(item);
            }

            if (selectedIndex >= 0 && selectedIndex < _itemsList.Items.Count)
            {
                _itemsList.SelectedIndex = selectedIndex;
            }
        }

        private void SelectDisplay(ComboBox combo)
        {
            var selectedValue = string.IsNullOrEmpty(_config.Dock.Display) ? "primary" : _config.Dock.Display;
            foreach (var item in combo.Items)
            {
                var option = item as DisplayOption;
                if (option != null && option.Value == selectedValue)
                {
                    combo.SelectedItem = option;
                    return;
                }
            }

            combo.SelectedIndex = 0;
        }

        private void SelectTheme(ComboBox combo)
        {
            var selectedValue = string.IsNullOrEmpty(_config.Dock.Theme) ? "system" : _config.Dock.Theme;
            foreach (var item in combo.Items)
            {
                var option = item as ThemeOption;
                if (option != null && option.Value == selectedValue)
                {
                    combo.SelectedItem = option;
                    return;
                }
            }

            combo.SelectedIndex = 0;
        }

        private void SelectLanguage(ComboBox combo)
        {
            var selectedValue = string.IsNullOrEmpty(_config.Dock.Language) ? "zh-CN" : _config.Dock.Language;
            foreach (var item in combo.Items)
            {
                var option = item as LanguageOption;
                if (option != null && option.Value == selectedValue)
                {
                    combo.SelectedItem = option;
                    return;
                }
            }

            combo.SelectedIndex = 0;
        }

        private void ApplyWindowTheme()
        {
            _palette = ThemeService.GetPalette(_config.Dock.Theme);
            Background = ThemeService.Brush(_palette.WindowBackground);
            Foreground = ThemeService.Brush(_palette.Text);
            if (_root != null)
            {
                _root.Background = ThemeService.Brush(_palette.WindowBackground);
                StyleTree(_root);
            }
        }

        private void ClearIconCache(object sender, RoutedEventArgs e)
        {
            _mainWindow.ClearIconCache();
            MessageBox.Show(T("cacheCleared"), "TidyDock", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenConfigFolder(object sender, RoutedEventArgs e)
        {
            _mainWindow.OpenConfigFolder();
        }

        private void OpenLogFolder(object sender, RoutedEventArgs e)
        {
            _mainWindow.OpenLogFolder();
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowAbout();
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ResetConfig(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                T("resetConfirm"),
                "TidyDock",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _mainWindow.ResetToDefaults();
                RefreshItems();
            }
        }

        private void Apply()
        {
            _mainWindow.ApplySettings();
        }

        private string T(string key)
        {
            return LocalizationService.T(_config, key);
        }

        private UIElement MakeTitle(string text)
        {
            var block = new TextBlock();
            block.Text = text;
            block.FontSize = 15;
            block.FontWeight = FontWeights.SemiBold;
            block.Margin = new Thickness(0, 0, 0, 10);
            block.Foreground = ThemeService.Brush(_palette.Text);
            block.Tag = "title";
            return block;
        }

        private UIElement MakeField(string label, UIElement control)
        {
            var root = new StackPanel();
            root.Margin = new Thickness(0, 0, 0, 12);

            var text = new TextBlock();
            text.Text = label;
            text.FontSize = 12;
            text.Margin = new Thickness(0, 0, 0, 5);
            text.Foreground = ThemeService.Brush(_palette.MutedText);
            root.Children.Add(text);
            root.Children.Add(control);
            return root;
        }

        private Slider MakeSlider(double min, double max, double value)
        {
            var slider = new Slider();
            slider.Minimum = min;
            slider.Maximum = max;
            slider.Value = value;
            slider.TickFrequency = Math.Max(1, (max - min) / 10);
            slider.IsSnapToTickEnabled = false;
            return slider;
        }

        private Panel MakeSection(Panel parent, string title)
        {
            var border = new Border();
            border.Tag = "section";
            border.Margin = new Thickness(0, 0, 0, 12);
            border.Padding = new Thickness(14, 13, 14, 12);
            border.CornerRadius = new CornerRadius(8);
            border.BorderThickness = new Thickness(1);
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = _palette.Shadow,
                BlurRadius = 16,
                ShadowDepth = 4,
                Opacity = _palette.IsDark ? 0.28 : 0.1
            };

            var section = new StackPanel();
            section.Children.Add(MakeTitle(title));
            border.Child = section;
            parent.Children.Add(border);
            StyleSection(border);
            return section;
        }

        private void StyleTree(DependencyObject root)
        {
            var section = root as Border;
            if (section != null && string.Equals(section.Tag as string, "section", StringComparison.Ordinal))
            {
                StyleSection(section);
            }

            var button = root as Button;
            if (button != null)
            {
                StyleButton(button);
            }

            var combo = root as ComboBox;
            if (combo != null)
            {
                combo.Height = 30;
                combo.Foreground = ThemeService.Brush(_palette.Text);
                combo.Background = ThemeService.Brush(_palette.ControlBackground, _palette.IsDark ? 0.92 : 1);
                combo.BorderBrush = ThemeService.Brush(_palette.PanelBorder, _palette.IsDark ? 0.7 : 0.9);
            }

            var checkBox = root as CheckBox;
            if (checkBox != null)
            {
                checkBox.Foreground = ThemeService.Brush(_palette.Text);
            }

            var listBox = root as ListBox;
            if (listBox != null)
            {
                listBox.Foreground = ThemeService.Brush(_palette.Text);
                listBox.Background = ThemeService.Brush(_palette.ControlBackground, _palette.IsDark ? 0.48 : 0.78);
                listBox.BorderBrush = ThemeService.Brush(_palette.PanelBorder, _palette.IsDark ? 0.7 : 0.9);
                listBox.BorderThickness = new Thickness(1);
            }

            var textBox = root as TextBox;
            if (textBox != null)
            {
                textBox.Foreground = ThemeService.Brush(_palette.Text);
                textBox.Background = ThemeService.Brush(_palette.ControlBackground);
                textBox.BorderBrush = ThemeService.Brush(_palette.PanelBorder, _palette.IsDark ? 0.7 : 0.9);
            }

            var textBlock = root as TextBlock;
            if (textBlock != null)
            {
                textBlock.Foreground = string.Equals(textBlock.Tag as string, "title", StringComparison.Ordinal)
                    ? ThemeService.Brush(_palette.Text)
                    : ThemeService.Brush(_palette.MutedText);
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                StyleTree(VisualTreeHelper.GetChild(root, i));
            }
        }

        private void StyleSection(Border border)
        {
            border.Background = ThemeService.Brush(_palette.PanelBackground, _palette.IsDark ? 0.78 : 0.94);
            border.BorderBrush = ThemeService.Brush(_palette.PanelBorder, _palette.IsDark ? 0.44 : 0.78);
            var effect = border.Effect as System.Windows.Media.Effects.DropShadowEffect;
            if (effect != null)
            {
                effect.Color = _palette.Shadow;
                effect.Opacity = _palette.IsDark ? 0.28 : 0.1;
            }
        }

        private void StyleButton(Button button)
        {
            var primary = string.Equals(button.Tag as string, "primary", StringComparison.Ordinal);
            button.FontSize = 12;
            button.BorderThickness = new Thickness(1);
            button.Foreground = ThemeService.Brush(primary ? _palette.AccentText : _palette.Text);
            button.Background = ThemeService.Brush(primary ? _palette.Accent : _palette.ControlBackground, primary ? 1 : (_palette.IsDark ? 0.64 : 0.88));
            button.BorderBrush = ThemeService.Brush(primary ? _palette.Accent : _palette.PanelBorder, primary ? 1 : (_palette.IsDark ? 0.62 : 0.9));
            button.Template = CreateRoundedButtonTemplate(primary);
        }

        private ControlTemplate CreateRoundedButtonTemplate(bool primary)
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Chrome";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetBinding(ContentPresenter.MarginProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.AppendChild(presenter);

            var template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;

            var hoverBrush = ThemeService.Brush(primary ? _palette.Accent : _palette.TileHover, primary ? 0.88 : (_palette.IsDark ? 0.58 : 0.76));
            var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Chrome"));
            template.Triggers.Add(hover);

            var pressedBrush = ThemeService.Brush(primary ? _palette.Accent : _palette.TileHover, primary ? 0.72 : (_palette.IsDark ? 0.72 : 0.96));
            var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBrush, "Chrome"));
            template.Triggers.Add(pressed);

            return template;
        }

        private class DisplayOption
        {
            public DisplayOption(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public string Value { get; private set; }
            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }

        private class ThemeOption
        {
            public ThemeOption(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public string Value { get; private set; }
            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }

        private class LanguageOption
        {
            public LanguageOption(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public string Value { get; private set; }
            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}
