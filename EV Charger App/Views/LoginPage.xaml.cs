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

        async private void LoginToCreate(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateAccountPage(app));
        }

        //This is the function called when the login button is clicked
        async private void SubmitLogin(object sender, EventArgs args)
        {
            String email = emailInput.Text;
            String password = passwordInput.Text;

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
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

        //Checks if the email and password inputted match an email and password combination in the database
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