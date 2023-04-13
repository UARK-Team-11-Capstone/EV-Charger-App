using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UserSettings : ContentPage
    {

        App app;

        public UserSettings(App app)
        {
            InitializeComponent();
            this.app = app;
        }

        //This is the function called when the save button is clicked
        async private void SaveSettings(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage(app));
        }

        async private void ChangePasswordTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ChangePassword(app));
        }

        async private void LogoutTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage(app));
        }



    }
}