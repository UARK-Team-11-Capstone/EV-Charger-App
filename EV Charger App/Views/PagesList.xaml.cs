using EV_Charger_App.Services;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PagesList : ContentPage
    {
        App app;
        DoEAPI doe;
        MainPage main;
        public PagesList(App app, DoEAPI doe, MainPage main)
        {
            InitializeComponent();
            this.app = app;
            this.doe = doe;
            this.main = main;
        }

        async private void MapScreen(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage(app));
        }

        async private void LoginScreen(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage(app));
        }

        async private void ChargerList(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ChargerListPage(app, doe, main));
        }

    }
}