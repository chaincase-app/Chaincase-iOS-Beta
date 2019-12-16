using System;

using Xamarin.Forms;

namespace Chaincase.Views
{
    public class SpendWhoPage : ContentPage
    {
        public SpendWhoPage()
        {
            Content = new StackLayout
            {
                Children = {
                    new Label { Text = "Hello ContentPage" }
                }
            };
        }
    }
}

