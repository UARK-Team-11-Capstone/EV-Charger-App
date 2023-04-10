using EV_Charger_App.Services;
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

        App app;

        public LoginPage(App app)
        {
            InitializeComponent();
            this.app = app; 
        }

        async private void LoginToCreate(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateAccountPage(app));
        }

        //This gets called when you click the menu bar on the ribbon
        // Will send the user to the page containing a list of pages
        // (map screen link, login screen link, settings link)
        async private void ListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PagesList(app));
        }

        //This is the function called when the login button is clicked
        async private void SubmitLogin(object sender, EventArgs args)
        {
            String email = emailInput.Text;
            String password = passwordInput.Text;

            //Check if credentials are valid
            if(CredentialsValid(email, password))
            {
                await Navigation.PushAsync(new MainPage(app));
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
            return true;
        }

    }
}