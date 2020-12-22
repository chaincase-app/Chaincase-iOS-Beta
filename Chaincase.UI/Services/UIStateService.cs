using System;

namespace Chaincase.UI.Services
{
	public class UIStateService
	{
		private string _title;
		private bool _darkMode;

		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				StateChanged?.Invoke();
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
