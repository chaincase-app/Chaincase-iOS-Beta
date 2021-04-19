using System;
using Chaincase.Common;
using Chaincase.Common.Contracts;

namespace Chaincase.UI.Services
{
    public class UIStateService
    {
        private string _title;
        private bool _darkMode;
        private readonly Global _global;
        private readonly IThemeManager _themeManager;

        public UIStateService(Global global, IThemeManager themeManager)
        {
            _global = global;
            _global.Resumed += (s, e) => SetSystemTheme();
            _themeManager = themeManager;
            themeManager.SubscribeToThemeChanged(() => SetSystemTheme());
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                StateChanged?.Invoke();
            }
        }

        public void SetSystemTheme()
        {
            DarkMode = _themeManager.IsDarkTheme();
        }

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                _darkMode = value;
                StateChanged?.Invoke();
                ThemeChanged?.Invoke();
            }
        }

        public event Action StateChanged;
        public event Action ThemeChanged;
    }
}
