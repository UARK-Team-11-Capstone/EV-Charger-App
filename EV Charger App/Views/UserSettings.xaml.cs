using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static Android.Media.Session.MediaSession;

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

            string token = app.session.getToken();

            List<object[]> userInformation = app.database.GetQueryRecords("SELECT * FROM Users WHERE sessionToken = '" + token + "'");

            string email = userInformation[0][0].ToString();
            string name = userInformation[0][2].ToString();
            string vehicle = userInformation[0][3].ToString();

            NameTypeCell.Text = name;
            EmailTypeCell.Text = email;
            VehicleTypeCell.Text = vehicle;
        }

        //This is the function called when the save button is clicked
        async private void SaveSettings(object sender, EventArgs e)
        {
            string fullName = NameTypeCell.Text;
            string vehicleType = VehicleTypeCell.Text;
            string token = app.session.getToken();

            if (!string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(vehicleType))
            {
                app.database.UpdateRecord("Users", new string[2] { "name", "vehicle" }, new string[2] { fullName, vehicleType }, "sessionToken", token);
            }

                app.session.setVehicleCharge((int)ChargeSlider.Value);
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