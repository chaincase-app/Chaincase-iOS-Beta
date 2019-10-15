using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace Chaincase.ViewModels
{
	public class SendViewModel : ViewModelBase
	{
		private string _password;
		private string _address;
		private string _label;
		private CoinListViewModel _coinListViewModel;


		public SendViewModel(IScreen hostScreen) : base(hostScreen)
		{
			CoinListViewModel = new CoinListViewModel(hostScreen);
		}

		public CoinListViewModel CoinListViewModel {
			get => _coinListViewModel;
			set => this.RaiseAndSetIfChanged(ref _coinListViewModel, value);
		}

	}
}
