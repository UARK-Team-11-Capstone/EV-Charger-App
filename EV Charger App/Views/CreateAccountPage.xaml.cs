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
            await Navigation.PushAsync(new LoginPage(app));
        }
    }
}