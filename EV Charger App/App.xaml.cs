using EV_Charger_App.Services;
using EV_Charger_App.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App
{
    public partial class App : Application
    {
        //Grant was here
        //Kate was here
        public App()
        {
            InitializeComponent();

            /*
            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
            */

            MainPage = new NavigationPage(new ChangePassword());


        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
