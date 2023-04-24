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

        /// <summary>
        /// Push the user back to the login page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void CreateToLogin(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage(app));
        }

        /// <summary>
        /// This function is called when the create account button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void SubmitCreate(object sender, EventArgs e)
        {
            string email = emailInputCreate.Text;
            string password = passwordInputCreate.Text;

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                //Check if email already exists in database
                if (UserExists(email) || passwordInputCreate.Text != confirmPasswordInputCreate.Text || !app.database.IsValidEmail(email))
                {
                    await DisplayAlert("", "There was an error creating your account.", "OK");
                    return;
                }

                RegisterUser(email, password);
                await DisplayAlert("", "Your account has been created.", "OK");
                await Navigation.PushAsync(new LoginPage(app));
            }
        }

        /// <summary>
        /// Create entry in the database with the given email and password
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        void RegisterUser(string email, string password)
        {
            //Hash password to store them in database

            string hashedPassword = app.database.HashPassword(password);

            //Insert new user into database
            app.database.InsertRecordSpecific("Users", new string[2] { "email", "password" }, new string[2] { email, hashedPassword });
        }

        /// <summary>
        /// Check if the given user exists based on email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        bool UserExists(string email)
        {
            string query = "SELECT * FROM Users WHERE email = @email";

            MySqlParameter emailParam = new MySqlParameter("@email", email);

            return app.database.RecordExists(query, emailParam);
        }
    }
}