using EV_Charger_App.Services;
using EV_Charger_App.Views;
using Xamarin.Forms;
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
            MainPage = new NavigationPage(new LoginPage(this));

        }

        protected override void OnStart()
        {

        }

        protected override void OnSleep()
        {
            database.Disconnect();
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
