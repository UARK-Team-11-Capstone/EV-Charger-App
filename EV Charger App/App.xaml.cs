using EV_Charger_App.Services;
using EV_Charger_App.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using GoogleApi;
using Debug = System.Diagnostics.Debug;


namespace EV_Charger_App
{
    public partial class App : Application
    {
        public Database database;
        public Session session;

        public App()
        {
            InitializeComponent();
            Debug.WriteLine("App Initialized");

            database = new Database();
            /*
            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
            */

            MainPage = new NavigationPage(new LoginPage(this));

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

        public void CreateSession(string email)
        {
            session = new Session(email, database);
        }
    }
}
