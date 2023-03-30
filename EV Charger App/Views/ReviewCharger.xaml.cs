using Android;
using Android.Content.Res;
using Android.Graphics;
using Android.Widget;
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
	public partial class ReviewCharger : ContentPage
	{
		public ReviewCharger ()
		{
			InitializeComponent ();
		}

        async private void SecondStar(object sender, EventArgs args)
        {
            
            await Navigation.PushAsync(new MainPage());

        }

        async private void ThirdStar(object sender, EventArgs args)
        {

            await Navigation.PushAsync(new MainPage());

        }

        async private void FourthStar(object sender, EventArgs args)
        {

            await Navigation.PushAsync(new MainPage());

        }

        async private void FifthStar(object sender, EventArgs args)
        {

            await Navigation.PushAsync(new MainPage());

        }
    }

    
}