using System.Drawing.Imaging;
using System.Reflection;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.UI;
using TidyDock.WinForms.Shell;

namespace TidyDock.UiTests;

internal static class Program
{
    private static int _assertions;

    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(args.Contains("--system-dpi") ? HighDpiMode.PerMonitorV2 : HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var output = Path.GetFullPath(args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? Path.Combine(AppContext.BaseDirectory, "screenshots"));
            Directory.CreateDirectory(output);
            if (args.Contains("--glass-preview"))
            {
                FrostPreview.Run(output);
                return 0;
            }
            TestControls();
            TestFrostedSurface();
            TestFilteredActions();
            TestDraftLifecycle();
            TestLayouts(output);
            Console.WriteLine($"PASS: {_assertions} assertions. Screenshots: {output}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void TestControls()
    {
        using var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-12000, -12000), ShowInTaskbar = false };
        using var button = new GlassButton { Text = "Test", Location = new Point(10, 10) };
        using var toggle = new ToggleSwitch { Location = new Point(10, 60) };
        using var slider = new GlassSlider { Location = new Point(10, 110), Minimum = 20, Maximum = 80, Value = 50 };
        host.Controls.AddRange([button, toggle, slider]);
        host.Show();
        Application.DoEvents();

        var clicks = 0;
        button.Click += (_, _) => clicks++;
        button.Focus();
        Key(button, "OnKeyDown", Keys.Space);
        Check(clicks == 0, "Button activates on key release, not key down");
        Key(button, "OnKeyUp", Keys.Space);
        Key(button, "OnKeyDown", Keys.Enter);
        Key(button, "OnKeyUp", Keys.Enter);
        Check(clicks == 2, "Space and Enter activate button exactly once");
        Key(button, "OnKeyDown", Keys.Space);
        slider.Focus();
        Key(button, "OnKeyUp", Keys.Space);
        Check(clicks == 2, "Losing focus cancels pending button activation");
        button.Enabled = false;
        Key(button, "OnKeyDown", Keys.Enter);
        Key(button, "OnKeyUp", Keys.Enter);
        Check(clicks == 2, "Disabled button cannot activate");
        button.AccessibilityObject.DoDefaultAction();
        Check(clicks == 2, "Disabled button ignores accessible action");
        button.Enabled = true;
        button.AccessibilityObject.DoDefaultAction();
        Check(clicks == 3, "Button supports accessible default action");

        toggle.Focus();
        Key(toggle, "OnKeyDown", Keys.Space);
        Key(toggle, "OnKeyUp", Keys.Space);
        Check(toggle.Checked, "Space toggles switch");
        Check(toggle.AccessibilityObject.State.HasFlag(AccessibleStates.Checked), "Switch exposes checked accessibility state");
        toggle.Enabled = false;
        toggle.AccessibilityObject.DoDefaultAction();
        Check(toggle.Checked, "Disabled switch ignores accessible action");
        toggle.Enabled = true;
        toggle.AccessibilityObject.DoDefaultAction();
        Check(!toggle.Checked, "Accessible default action toggles switch");

        slider.Focus();
        Key(slider, "OnKeyDown", Keys.Right);
        Check(slider.Value == 51, "Arrow changes slider by one");
        Key(slider, "OnKeyDown", Keys.PageDown);
        Check(slider.Value == 45, "PageDown changes slider by a larger step");
        Key(slider, "OnKeyDown", Keys.Home);
        Check(slider.Value == 20, "Home reaches minimum");
        Key(slider, "OnKeyDown", Keys.Left);
        Check(slider.Value == 20, "Slider clamps lower bound");
        Key(slider, "OnKeyDown", Keys.End);
        Check(slider.Value == 80 && slider.AccessibilityObject.Value == "80", "End reaches maximum and accessibility value stays in sync");
        slider.AccessibilityObject.Value = "35";
        Check(slider.Value == 35, "Slider supports accessible value updates");
        Invoke(slider, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 10, 16, 0));
        Check(slider.Capture && slider.Value == 20, "Slider captures mouse and maps track start to minimum");
        slider.Capture = false;
        Invoke(slider, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 200, 16, 0));
        Check(slider.Value == 20, "Capture loss ends drag");
        slider.Enabled = false;
        Key(slider, "OnKeyDown", Keys.Right);
        Check(slider.Value == 20, "Disabled slider ignores keyboard");
        host.Close();
    }

    private static void TestFrostedSurface()
    {
        Check(DwmBackdrop.PackAccentColor(Color.FromArgb(0x12, 0x34, 0x56, 0x78)) == 0x12785634, "Native tint uses ABGR packing");
        foreach (var theme in new[] { "light", "dark" })
        {
            var palette = Theme.Get(theme);
            Check(FrostedSurface.Tint(palette, 0, false).A == 0, "Zero opacity removes material tint");
            Check(FrostedSurface.Tint(palette, 1, true).A > FrostedSurface.Tint(palette, 1, false).A, "High contrast increases material opacity");
            using var image = new Bitmap(120, 80);
            using var graphics = Graphics.FromImage(image);
            FrostedSurface.Draw(graphics, new Rectangle(4, 4, 110, 70), 16, palette, false);
            Check(image.GetPixel(50, 40).A == 255, "Unsupported composition falls back to opaque fill");
            graphics.Clear(Color.Transparent);
            FrostedSurface.Draw(graphics, new Rectangle(4, 4, 110, 70), 16, palette, true);
            Check(image.GetPixel(50, 40).A is > 0 and < 255, "Active material keeps foreground translucent");
            Check(image.GetPixel(4, 4).A == 0, "Rounded foreground does not tint outside corners");
            graphics.Clear(Color.Transparent);
            FrostedSurface.Draw(graphics, new Rectangle(4, 4, 110, 70), 16, palette, true, 0);
            Check(image.GetPixel(50, 40).A == 0, "Zero opacity leaves icons-only space");
        }
    }

    private static void TestFilteredActions()
    {
        using var fixture = new SettingsFixture(12);
        var form = fixture.Form;
        var items = Field<ListView>(form, "_items");
        var search = Field<TextBox>(form, "_itemSearch");
        var originalOrder = fixture.Config.Items.Select(item => item.Id).ToArray();
        search.Text = "Entry 09";
        Check(items.Items.Count == 1, "Search filters rows");
        items.Items[0].Selected = true;
        Invoke(form, "MoveSelected", -1);
        Check(fixture.Config.Items[8].Name == "Entry 09", "Filtered move uses config index, not visible row index");
        Check(items.SelectedItems.Count == 1, "Move retains selected row");
        Invoke(form, "RemoveSelected", null, EventArgs.Empty);
        Check(fixture.Config.Items.Count == 11 && fixture.Config.Items.All(item => item.Name != "Entry 09"), "Filtered removal deletes the selected item only");
        Check(Field<GlassButton>(form, "_undoRemoveButton").Enabled, "Undo becomes available");
        Invoke(form, "UndoRemove");
        Check(search.TextLength == 0 && items.Items.Count == 12, "Undo reveals restored row");
        Check(fixture.Config.Items[8].Name == "Entry 09" && items.SelectedItems[0].Text.Length > 0, "Undo restores position and selection");
        Invoke(form, "MoveSelected", 1);
        Check(fixture.Config.Items.Select(item => item.Id).SequenceEqual(originalOrder), "Move back restores original ordering");
        search.Text = "nonexistent search text";
        Check(items.Items.Count == 0 && Field<Label>(form, "_emptyItemsLabel").Visible, "Search has a visible empty state");
        Check(!Field<GlassButton>(form, "_removeButton").Enabled, "No matches disables selection actions");
        search.Clear();
        items.Items[0].Selected = true;
        Invoke(form, "RemoveSelected", null, EventArgs.Empty);
        Invoke(form, "RemoveSelected", null, EventArgs.Empty);
        Invoke(form, "UndoRemove");
        Invoke(form, "UndoRemove");
        Check(fixture.Config.Items.Select(item => item.Id).SequenceEqual(originalOrder), "Multiple removals undo in order");
        Check(!Field<GlassButton>(form, "_undoRemoveButton").Enabled, "Undo disables when history is empty");
        search.Text = "Entry";
        search.Focus();
        Invoke(form, "ProcessCmdKey", new Message(), Keys.Escape);
        Check(search.TextLength == 0 && !form.IsDisposed, "Escape clears active search before closing settings");
        Invoke(form, "ProcessCmdKey", new Message(), Keys.Control | Keys.Tab);
        Check(Field<int>(form, "_activeSection") == 1, "Ctrl+Tab selects next category");
        Invoke(form, "ProcessCmdKey", new Message(), Keys.Control | Keys.Shift | Keys.Tab);
        Check(Field<int>(form, "_activeSection") == 0, "Ctrl+Shift+Tab selects previous category");
    }

    private static void TestDraftLifecycle()
    {
        using var fixture = new SettingsFixture(2);
        Check(Field<GlassButton>(fixture.Form, "_confirmButton").Enabled, "Unchanged settings can save and close");
        Check(!Field<GlassButton>(fixture.Form, "_previewButton").Enabled, "Unchanged draft cannot preview redundantly");
        fixture.Config.Dock.IconSize++;
        Invoke(fixture.Form, "MarkDraftChanged");
        Check(Field<GlassButton>(fixture.Form, "_previewButton").Enabled, "Draft change enables preview");
        Invoke(fixture.Form, "PreviewSettings", null, EventArgs.Empty);
        Check(fixture.PreviewCount == 1 && fixture.CommitCount == 0, "Preview does not commit");
        Check(!Field<GlassButton>(fixture.Form, "_previewButton").Enabled, "Preview disables until draft changes again");
        fixture.Config.Dock.IconSize--;
        Invoke(fixture.Form, "ConfirmSettings", null, EventArgs.Empty);
        Check(fixture.CommitCount == 1 && fixture.Committed?.Dock.IconSize == 52, "Saving original values after a preview restores the original configuration");

        using var rebuild = new SettingsFixture(4);
        Field<ListView>(rebuild.Form, "_items").Items[2].Selected = true;
        SelectSection(rebuild.Form, 1);
        var oldItems = Field<ListView>(rebuild.Form, "_items");
        var oldImages = oldItems.SmallImageList!;
        var theme = Descendants(rebuild.Form).OfType<ComboBox>().First(combo => combo.AccessibleName == "主题");
        theme.SelectedIndex = 2;
        Application.DoEvents();
        Check(oldItems.IsDisposed, "Theme rebuild disposes old controls");
        Check(!oldImages.HandleCreated, "Theme rebuild releases image list");
        Check(Field<int>(rebuild.Form, "_activeSection") == 1, "Theme rebuild retains section");
        Check(rebuild.Config.Dock.Theme == "dark", "Theme selection updates draft");
        Check(Field<ListView>(rebuild.Form, "_items").SelectedItems[0].Tag == rebuild.Config.Items[2], "Theme rebuild retains selected item");
        var magnification = Descendants(rebuild.Form).OfType<GlassSlider>().First(slider => slider.AccessibleName == "悬停放大");
        Key(magnification, "OnKeyDown", Keys.End);
        Check(rebuild.Config.Dock.Magnification == 1.8, "Magnification slider agrees with persisted limit");
    }

    private static void TestLayouts(string output)
    {
        foreach (var theme in new[] { "light", "dark" })
        {
            var palette = Theme.Get(theme);
            Check(Contrast(palette.Accent, palette.AccentText) >= 4.5, $"{theme}: accent text contrast meets 4.5:1");
            foreach (var width in new[] { 860, 960, 1280 })
            {
                using var fixture = new SettingsFixture(50, theme);
                var form = fixture.Form;
                form.Size = new Size(width, width == 860 ? 560 : 720);
                Application.DoEvents();
                for (var section = 0; section < 5; section++)
                {
                    SelectSection(form, section);
                    var root = Field<FlowLayoutPanel>(form, "_scrollRoot");
                    Check(!root.HorizontalScroll.Visible, $"{theme}/{width}/{section}: no horizontal scrolling");
                    foreach (var control in Descendants(root).Where(control => control.Visible && control is not ListView))
                    {
                        if (control.Parent is not null && control.Parent != root)
                        {
                            Check(control.Right <= control.Parent.ClientSize.Width + 1, $"{theme}/{width}/{section}: {control.GetType().Name} fits parent horizontally");
                        }
                    }
                    foreach (var row in Descendants(root).OfType<FlowLayoutPanel>())
                    {
                        var children = row.Controls.Cast<Control>().Where(child => child.Visible).ToArray();
                        for (var i = 0; i < children.Length; i++)
                        {
                            for (var j = i + 1; j < children.Length; j++)
                            {
                                Check(!children[i].Bounds.IntersectsWith(children[j].Bounds), $"{theme}/{width}/{section}: controls do not overlap");
                            }
                        }
                    }
                    if (section == 0)
                    {
                        var list = Field<ListView>(form, "_items");
                        Check(list.Height < 340 * (list.DeviceDpi / 96f) && list.Items.Count == 50 && list.Scrollable, "Large item list stays bounded and scrollable");
                        Check(list.Columns.Cast<ColumnHeader>().Sum(column => column.Width) <= list.ClientSize.Width, "Table columns fit list");
                        Check(!root.VerticalScroll.Visible, "Item actions fit viewport without scrolling the page");
                    }
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(Path.Combine(output, $"settings-{theme}-{width}-{section}.png"), ImageFormat.Png);
                }
            }
        }
        using var empty = new SettingsFixture(0);
        Check(Field<Label>(empty.Form, "_emptyItemsLabel").Visible, "Empty configuration exposes empty state");
    }

    private static void SelectSection(SettingsForm form, int index)
    {
        var navigation = Field<FlowLayoutPanel>(form, "_navigation");
        ((RadioButton)navigation.Controls[index]).Checked = true;
        Application.DoEvents();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static double Contrast(Color a, Color b)
    {
        static double Channel(byte c) => c / 255d <= 0.04045 ? c / 255d / 12.92 : Math.Pow((c / 255d + 0.055) / 1.055, 2.4);
        static double Luminance(Color c) => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        return (Math.Max(Luminance(a), Luminance(b)) + 0.05) / (Math.Min(Luminance(a), Luminance(b)) + 0.05);
    }

    private static void Key(Control control, string method, Keys key) => Invoke(control, method, new KeyEventArgs(key));

    private static object? Invoke(object target, string method, params object?[] args)
    {
        return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    }

    private static T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
        _assertions++;
    }

    internal sealed class SettingsFixture : IDisposable
    {
        private readonly IconService _icons;
        public AppConfig Config { get; }
        public SettingsForm Form { get; }
        public int PreviewCount { get; private set; }
        public int CommitCount { get; private set; }
        public AppConfig? Committed { get; private set; }

        public SettingsFixture(int count, string theme = "light")
        {
            Config = new AppConfig { Dock = new DockSettings { Theme = theme } };
            for (var i = 0; i < count; i++)
            {
                Config.Items.Add(new DockItem
                {
                    Name = $"Entry {i:00}",
                    Type = DockItemTypes.Folder,
                    Target = Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                });
            }
            var paths = new AppPaths();
            var log = new LogService(paths);
            _icons = new IconService(paths, log);
            // Never load, save, import, or delete the user's configuration in this harness.
            Form = new SettingsForm(Config, ConfigService.Clone(Config), _icons,
                new ShortcutService(paths), new StartupService(), new DataBackupService(paths, log),
                _ => PreviewCount++, config => { CommitCount++; Committed = config; }, _ => { })
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-12000, -12000),
                ShowInTaskbar = false
            };
            Form.Show();
            Application.DoEvents();
        }

        public void Dispose()
        {
            // Dispose bypasses interactive discard confirmation for isolated test drafts.
            Form.Dispose();
            _icons.Dispose();
        }
    }
}
