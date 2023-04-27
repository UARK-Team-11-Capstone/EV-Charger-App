using MySqlConnector;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChangePassword : ContentPage
    {
        App app;

        public ChangePassword(App app)
        {
            InitializeComponent();
            this.app = app;
        }

        //This is the function called when the Update Password button is clicked
        async private void SavePassword(object sender, EventArgs e)
        {
            //Get user from token
            string currentPass = currentPassword.Text;
            string token = app.session.getToken();

            //Check if current password is correct
            if (VerifyCurrentPassword(currentPass, token))
            {
                ErrorText.Opacity = 0.0;

                //Check is new passwords match
                if (newPassword1.Text == newPassword2.Text)
                {
                    //Update database with new password
                    string hashedPassword = app.database.HashPassword(newPassword1.Text);

                    app.database.UpdateRecord("Users", new string[1] { "password" }, new string[1] { hashedPassword }, "sessionToken", token);

                    //Removing error message if one exist
                    ErrorText.Opacity = 0.0;

                    await DisplayAlert("", "Your password has been updated.", "OK");

                    await Navigation.PushAsync(new LoginPage(app));

                }
                //new passwords don't match
                else
                {
                    //Display error message
                    ErrorText.Opacity = 1.0;
                    ErrorText.Text = "Oops! Passwords don't match.";
                }
            }
            else
            {
                //Display error message
                ErrorText.Opacity = 1.0;
                ErrorText.Text = "Password incorrect";

            }
        }

        public bool VerifyCurrentPassword(string currentPassword, string token)
        {
            string hashedPassword = app.database.HashPassword(currentPassword);

            string query = "SELECT * FROM Users WHERE password = @password AND sessionToken = @token";

            MySqlParameter tokenParam = new MySqlParameter("@token", token);
            MySqlParameter passwordParam = new MySqlParameter("@password", hashedPassword);


            return app.database.RecordExists(query, passwordParam, tokenParam);
        }

    }
}