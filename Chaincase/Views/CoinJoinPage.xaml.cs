﻿using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;
using AppKit;
using CoreGraphics;
using System;

namespace Chaincase.Views
{
	public partial class CoinJoinPage : ReactiveContentPage<CoinJoinViewModel>
	{

        public CoinJoinPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Balance,
                    v => v.Balance.Text)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.CoinList.ViewModel)
                    .DisposeWith(d);
            });
        }

        async void Confirm(object sender, EventArgs e)
        {
            string password = await DisplayPromptAsync("Confirm CoinJoin", "Enter your password.", "Confirm");
            ViewModel.CoinJoinCommand.Execute(password);
        }
    }
}
