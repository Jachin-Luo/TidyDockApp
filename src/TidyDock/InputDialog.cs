using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TidyDock
{
    public class InputDialog : Window
    {
        private readonly TextBox _textBox;
        private readonly DockConfig _config;

        public InputDialog(string title, string label, string value)
            : this(null, title, label, value)
        {
        }

        public InputDialog(DockConfig config, string title, string label, string value)
        {
            _config = config;
            var palette = ThemeService.GetPalette(_config == null ? "system" : _config.Dock.Theme);
            Title = title;
            Width = 360;
            Height = 164;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = ThemeService.Brush(palette.WindowBackground);

            var shell = new Border();
            shell.Margin = new Thickness(12);
            shell.Padding = new Thickness(14);
            shell.CornerRadius = new CornerRadius(8);
            shell.BorderThickness = new Thickness(1);
            shell.Background = ThemeService.Brush(palette.PanelBackground, palette.IsDark ? 0.82 : 0.96);
            shell.BorderBrush = ThemeService.Brush(palette.PanelBorder, palette.IsDark ? 0.5 : 0.8);
            Content = shell;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.Child = root;

            var labelBlock = new TextBlock();
            labelBlock.Text = label;
            labelBlock.Margin = new Thickness(0, 0, 0, 8);
            labelBlock.Foreground = ThemeService.Brush(palette.MutedText);
            root.Children.Add(labelBlock);

            _textBox = new TextBox();
            _textBox.Text = value ?? string.Empty;
            _textBox.Height = 28;
            _textBox.VerticalContentAlignment = VerticalAlignment.Center;
            _textBox.Foreground = ThemeService.Brush(palette.Text);
            _textBox.Background = ThemeService.Brush(palette.ControlBackground, palette.IsDark ? 0.92 : 1);
            _textBox.BorderBrush = ThemeService.Brush(palette.PanelBorder, palette.IsDark ? 0.7 : 0.9);
            Grid.SetRow(_textBox, 1);
            root.Children.Add(_textBox);

            var buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            buttons.Margin = new Thickness(0, 14, 0, 0);
            Grid.SetRow(buttons, 2);
            root.Children.Add(buttons);

            var cancel = new Button();
            cancel.Content = T("cancel");
            cancel.Width = 72;
            cancel.Height = 30;
            cancel.Margin = new Thickness(0, 0, 8, 0);
            StyleButton(cancel, palette, false);
            cancel.Click += delegate { DialogResult = false; };
            buttons.Children.Add(cancel);

            var ok = new Button();
            ok.Content = T("ok");
            ok.Width = 72;
            ok.Height = 30;
            StyleButton(ok, palette, true);
            ok.Click += delegate { DialogResult = true; };
            buttons.Children.Add(ok);

            Loaded += delegate
            {
                _textBox.Focus();
                _textBox.SelectAll();
            };
        }

        public string Value
        {
            get { return _textBox.Text; }
        }

        private string T(string key)
        {
            return LocalizationService.T(_config, key);
        }

        private void StyleButton(Button button, ThemePalette palette, bool primary)
        {
            button.FontSize = 12;
            button.BorderThickness = new Thickness(1);
            button.Foreground = ThemeService.Brush(primary ? palette.AccentText : palette.Text);
            button.Background = ThemeService.Brush(primary ? palette.Accent : palette.ControlBackground, primary ? 1 : (palette.IsDark ? 0.64 : 0.88));
            button.BorderBrush = ThemeService.Brush(primary ? palette.Accent : palette.PanelBorder, primary ? 1 : (palette.IsDark ? 0.62 : 0.9));
            button.Template = CreateRoundedButtonTemplate(palette, primary);
        }

        private ControlTemplate CreateRoundedButtonTemplate(ThemePalette palette, bool primary)
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
            border.AppendChild(presenter);

            var template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;

            var hoverBrush = ThemeService.Brush(primary ? palette.Accent : palette.TileHover, primary ? 0.88 : (palette.IsDark ? 0.58 : 0.76));
            var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Chrome"));
            template.Triggers.Add(hover);

            return template;
        }
    }
}
