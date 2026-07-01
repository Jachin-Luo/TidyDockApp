using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using Forms = System.Windows.Forms;

namespace TidyDock
{
    public class TrayService : IDisposable
    {
        private readonly MainWindow _window;
        private readonly DockConfig _config;
        private readonly Forms.NotifyIcon _notifyIcon;
        private readonly Forms.ContextMenuStrip _menu;

        public TrayService(MainWindow window, DockConfig config, bool visible)
        {
            _window = window;
            _config = config;
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location) ?? SystemIcons.Application;
            _notifyIcon.Text = "TidyDock";
            _notifyIcon.Visible = visible;

            _menu = new Forms.ContextMenuStrip();
            _notifyIcon.ContextMenuStrip = _menu;
            _notifyIcon.DoubleClick += OnToggleDock;
            RefreshText();
        }

        public void RefreshText()
        {
            _menu.Items.Clear();
            _menu.Items.Add(LocalizationService.T(_config, "toggleDock"), null, OnToggleDock);
            _menu.Items.Add(LocalizationService.T(_config, "settings"), null, OnSettings);
            _menu.Items.Add(LocalizationService.T(_config, "about"), null, OnAbout);
            _menu.Items.Add(LocalizationService.T(_config, "exit"), null, OnExit);
        }

        public void SetVisible(bool visible)
        {
            _notifyIcon.Visible = visible;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
        }

        private void OnToggleDock(object sender, EventArgs e)
        {
            _window.Dispatcher.Invoke(delegate
            {
                _window.ToggleDockVisibility();
            });
        }

        private void OnSettings(object sender, EventArgs e)
        {
            _window.Dispatcher.Invoke(delegate
            {
                _window.ShowSettings();
            });
        }

        private void OnAbout(object sender, EventArgs e)
        {
            _window.Dispatcher.Invoke(delegate
            {
                _window.ShowAbout();
            });
        }

        private void OnExit(object sender, EventArgs e)
        {
            _window.Dispatcher.Invoke(delegate
            {
                Application.Current.Shutdown();
            });
        }
    }
}
