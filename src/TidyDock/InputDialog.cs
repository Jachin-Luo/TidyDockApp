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
            Title = title;
            Width = 360;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));

            var root = new Grid();
            root.Margin = new Thickness(14);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;

            var labelBlock = new TextBlock();
            labelBlock.Text = label;
            labelBlock.Margin = new Thickness(0, 0, 0, 8);
            labelBlock.Foreground = new SolidColorBrush(Color.FromRgb(39, 50, 68));
            root.Children.Add(labelBlock);

            _textBox = new TextBox();
            _textBox.Text = value ?? string.Empty;
            _textBox.Height = 28;
            _textBox.VerticalContentAlignment = VerticalAlignment.Center;
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
            cancel.Height = 28;
            cancel.Margin = new Thickness(0, 0, 8, 0);
            cancel.Click += delegate { DialogResult = false; };
            buttons.Children.Add(cancel);

            var ok = new Button();
            ok.Content = T("ok");
            ok.Width = 72;
            ok.Height = 28;
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
    }
}
