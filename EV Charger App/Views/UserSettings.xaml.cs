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
    public partial class UserSettings : ContentPage
    {
        public UserSettings()
        {
            InitializeComponent();
        }

        //This is the function called when the save button is clicked
        async private void SaveSettings(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage());
        }
    }
}