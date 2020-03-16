using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.Navigation
{
    public sealed class ViewStackService : IViewStackService
    {
        private readonly BehaviorSubject<IImmutableList<IViewModel>> modalStack;
        private readonly BehaviorSubject<IImmutableList<IViewModel>> pageStack;
        private readonly IView view;

        public ViewStackService(IView view)
        {
            Guard.NotNull(nameof(view), view);

            this.modalStack = new BehaviorSubject<IImmutableList<IViewModel>>(ImmutableList<IViewModel>.Empty);
            this.pageStack = new BehaviorSubject<IImmutableList<IViewModel>>(ImmutableList<IViewModel>.Empty);
            this.view = view;

            this
                .view
                .PagePopped
                .Do(
                    poppedPage =>
                    {
                        var currentPageStack = this.pageStack.Value;

                        if (currentPageStack.Count > 0 && poppedPage == currentPageStack[currentPageStack.Count - 1])
                        {
                            var removedPage = PopStackAndTick(this.pageStack);
                            Logger.LogDebug($"Removed page '{removedPage.Id}' from stack.");
                        }
                    })
                .Subscribe();
        }

        public IView View => this.view;

        public IObservable<IImmutableList<IViewModel>> ModalStack => this.modalStack;

        public IObservable<IImmutableList<IViewModel>> PageStack => this.pageStack;

        public IObservable<Unit> PushPage(IViewModel page, string contract = null, bool resetStack = false, bool animate = true)
        {
            Guard.NotNull(nameof(view), view);

            return this
                .view
                .PushPage(page, contract, resetStack, animate)
                .Do(
                    _ =>
                    {
                        AddToStackAndTick(this.pageStack, page, resetStack);
                        Logger.LogDebug($"Added page '{page.Id}' (contract '{contract}') to stack.");
                    });
        }

        public IObservable<Unit> PopPage(bool animate = true) =>
            this
                .view
                .PopPage(animate);

        public IObservable<Unit> PushModal(IViewModel modal, string contract = null)
        {
            Guard.NotNull(nameof(modal), modal);

            return this
                .view
                .PushModal(modal, contract)
                .Do(
                    _ =>
                    {
                        AddToStackAndTick(this.modalStack, modal, false);
                        Logger.LogDebug($"Added modal '{modal.Id}' (contract '{contract}') to stack.");
                    });
        }

        public IObservable<Unit> PopModal() =>
            this
                .view
                .PopModal()
                .Do(
                    _ =>
                    {
                        var removedModal = PopStackAndTick(this.modalStack);
                        Logger.LogDebug($"Removed modal '{removedModal.Id}' from stack.");
                    });

        private static void AddToStackAndTick<T>(BehaviorSubject<IImmutableList<T>> stackSubject, T item, bool reset)
        {
            var stack = stackSubject.Value;

            if (reset)
            {
                stack = new[] { item }.ToImmutableList();
            }
            else
            {
                stack = stack.Add(item);
            }

            stackSubject.OnNext(stack);
        }

        private static T PopStackAndTick<T>(BehaviorSubject<IImmutableList<T>> stackSubject)
        {
            var stack = stackSubject.Value;

            if (stack.Count == 0)
            {
                throw new InvalidOperationException("Stack is empty.");
            }

            var removedItem = stack[stack.Count - 1];
            stack = stack.RemoveAt(stack.Count - 1);
            stackSubject.OnNext(stack);
            return removedItem;
        }
    }
}

