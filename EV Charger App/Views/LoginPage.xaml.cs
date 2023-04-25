using MySqlConnector;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {

        App app;

        public LoginPage(App app)
        {
            InitializeComponent();
            NavigationPage.SetHasBackButton(this, false);
            this.app = app;
        }

        /// <summary>
        /// Push the user from login page to create account page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void LoginToCreate(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateAccountPage(app));
        }

        /// <summary>
        /// This is the function called when the login button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async private void SubmitLogin(object sender, EventArgs args)
        {
            String email = emailInput.Text;
            String password = passwordInput.Text;

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && app.database.IsValidEmail(email))
            {
                //Check if credentials are valid
                if (CredentialsValid(email, password))
                {
                    //Create a session with a session token for the logged in user
                    app.CreateSession(email);
                    await Navigation.PushAsync(new MainPage(app));
                }
                else
                {
                    //Display error message
                    LoginErrorText.Opacity = 1.0;
                }
            }
            else
            {
                //Display error message
                LoginErrorText.Opacity = 1.0;
            }

        }

        /// <summary>
        /// Checks if the email and password inputted match an email and password combination in the database
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        bool CredentialsValid(String email, String password)
        {
            string hashedPassword = app.database.HashPassword(password);

            string query = "SELECT * FROM Users WHERE email = @email AND password = @password";

            MySqlParameter emailParam = new MySqlParameter("@email", email);
            MySqlParameter passwordParam = new MySqlParameter("@password", hashedPassword);

            return app.database.RecordExists(query, emailParam, passwordParam);
        }

    }
}