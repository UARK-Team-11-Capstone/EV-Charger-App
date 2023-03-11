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
            Title = "Settings";
            Content = new StackLayout
            {
                Margin = new Thickness(20),
                Children =
            {
                new Label { Text = "Your profile" },
                new Label { Text = "Name" },
                new Label { Text = "Email" },
                new Label { Text = "Password" },
                new Label { Text = "Address" },
                new Label { Text = "Point Meter" },
                new Label { Text = "Vehicle Type" }
            }
            };
        }
    }
}