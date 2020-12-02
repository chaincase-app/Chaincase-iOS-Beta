using System;

namespace Chaincase.UI.Services
{
	public class UIStateService
	{
		private string _title;

		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				StateChanged?.Invoke();
			}
		}

		public event Action StateChanged;
	}
}
