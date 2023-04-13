using MySqlConnector;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateAccountPage : ContentPage
    {

        App app;
        public CreateAccountPage(App app)
        {
            InitializeComponent();
            this.app = app;
        }

        async private void CreateToLogin(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage(app));
        }

        //This function is called when the create account button is pressed
        async private void SubmitCreate(object sender, EventArgs e)
        {
            //TODO: Add code to add inputted user credentials to the database

            string email = emailInputCreate.Text;
            string password = passwordInputCreate.Text;

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                //Check if email already exists in database
                if (UserExists(email))
                {
                    return;
                }

                RegisterUser(email, password);
            }

            await DisplayAlert("", "Your account has been created.", "OK");

            await Navigation.PushAsync(new LoginPage(app));
        }

        void RegisterUser(string email, string password)
        {
            //Hash password to store them in database

            string hashedPassword = app.database.HashPassword(password);

            //Insert new user into database
            app.database.InsertRecordSpecific("Users", new string[2] { "email", "password" }, new string[2] { email, hashedPassword });
        }

        bool UserExists(string email)
        {
            string query = "SELECT * FROM Users WHERE email = @email";

            MySqlParameter emailParam = new MySqlParameter("@email", email);

            return app.database.RecordExists(query, emailParam);
        }
    }
}