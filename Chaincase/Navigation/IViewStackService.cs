using System;
using System.Collections.Immutable;
using System.Reactive;

namespace Chaincase.Navigation
{
    public interface IViewStackService
    {
        IView View
        {
            get;
        }

        IObservable<IImmutableList<IViewModel>> PageStack
        {
            get;
        }

        IObservable<IImmutableList<IViewModel>> ModalStack
        {
            get;
        }

        IObservable<Unit> PushPage(
        IViewModel page,
        string contract = null,
        bool resetStack = false,
        bool animate = true);

        IObservable<Unit> PopPage(
            bool animate = true);

        IObservable<Unit> PushModal(
            IViewModel modal,
            string contract = null);

        IObservable<Unit> PopModal();
    }

    public interface IView
    {
        IObservable<IViewModel> PagePopped
        {
            get;
        }

        IObservable<Unit> PushPage(
            IViewModel pageViewModel,
            string contract,
            bool resetStack,
            bool animate);

        IObservable<Unit> PopPage(
            bool animate);

        IObservable<Unit> PushModal(
            IViewModel modalViewModel,
            string contract);

        IObservable<Unit> PopModal();
    }
}
