using EV_Charger_App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            this.BindingContext = new LoginViewModel();
        }

        async private void LoginToCreate(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateAccountPage());
        }

        //This is the function called when the login button is clicked
        async private void SubmitLogin(object sender, EventArgs args)
        {
            //TODO: Add code to compare inputted credentials with existing credentials in the database

            String email = emailInput.Text;
            String password = passwordInput.Text;

            //Check if credentials are valid
            if(CredentialsValid(email, password))
            {
                await Navigation.PushAsync(new LoginPage());
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
            return false;
        }

    }
}