using System;
using Chaincase.Common;
using Chaincase.Common.Contracts;

namespace Chaincase.UI.Services
{
	public enum Theme
	{
		System,
		Light,
		Dark
	}

	public class UIStateService
	{



		private string _title;
		private Theme _theme;
		private bool _darkMode;
		protected IThemeManager _themeManager;

		public UIStateService(IThemeManager themeManager, Global global)
		{
			global.Resumed += (s, e) => SetSystemTheme();

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

		public Theme Theme
		{
			get => _theme;
			set
			{
				_theme = value;
				SetSystemTheme();
			}
		}

		public void SetSystemTheme()
		{
			switch(Theme)
			{
				case Theme.Light:
					DarkMode = false;
					break;
				case Theme.Dark:
					DarkMode = true;
					break;
				case Theme.System:
				default:
					DarkMode = _themeManager.IsDarkTheme();
					break;
			}
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
