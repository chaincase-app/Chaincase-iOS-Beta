using System.Windows.Input;
using Xamarin.Forms;
using Chaincase.Navigation;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System;
using System.Reactive.Linq;

namespace Chaincase.ViewModels
{
	public class MnemonicViewModel : BaseViewModel
	{
		private string _mnemonicString;
		public string MnemonicString
		{
			get => _mnemonicString;
			set => this.RaiseAndSetIfChanged(ref _mnemonicString, value);
		}

		public ReactiveCommand<Unit, Unit> AcceptCommand;

		public MnemonicViewModel(IViewStackService viewStackService, string mnemonicString) : base(viewStackService)
		{
			MnemonicString = mnemonicString;
			AcceptCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				viewStackService.PushPage(new VerifyMnemonicViewModel(viewStackService, MnemonicString)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}
	}
}
