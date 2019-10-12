using System;
using ReactiveUI;

namespace Wasabi.ViewModels
{
	public class SendViewModel : ViewModelBase
	{
		private string _password;
		private string _address;
		private string _label;

		public SendViewModel(IScreen hostScreen) : base(hostScreen)
		{
		}
	}
}
