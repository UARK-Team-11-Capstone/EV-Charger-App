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
	public partial class PagesList : ContentPage
	{
		public PagesList ()
		{
			InitializeComponent ();
		}

        async private void MapScreen(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage());
        }

        async private void LoginScreen(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }

        async private void UserSettings(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserSettings());
        }

        async private void ReviewChargers(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ReviewCharger());
        }

        //This gets called when you click the menu bar on the ribbon
        // Will send the user to the page containing a list of pages
        // (map screen link, login screen link, settings link)
        async private void ListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PagesList());
        }
    }
}