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
            await Navigation.PushAsync(new UserSettings(app));
        }
    }
}