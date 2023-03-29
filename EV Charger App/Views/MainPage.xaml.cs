using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.Linq;
using EV_Charger_App.ViewModels;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using EV_Charger_App.Views;
using System.Diagnostics;

namespace EV_Charger_App
{
    public partial class MainPage : ContentPage
    {
        MainPageViewModel mainPageViewModel;
        Xamarin.Forms.GoogleMaps.Map map;
        public MainPage()
        {
            InitializeComponent();

            BindingContext = mainPageViewModel = new MainPageViewModel();

            NavigationPage.SetHasNavigationBar(this, true);
            LoadMap(36.09171012916079, -94.20143973570228);

        }

        public async void LoadMap(double latitude, double longitude)
        {
            try
            {
                // Create map
                map = new Xamarin.Forms.GoogleMaps.Map()
                {
                    Margin = new Thickness(2, 2, 2, 2),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MapType = MapType.Street,
                    IsEnabled = true
                };

                // Call the track location function
                TrackLocation();

                // Adjust XAML settings for the layout of the stack
                StackLayout stackLayout = new StackLayout()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    BackgroundColor = Color.Transparent,
                    Orientation = StackOrientation.Vertical
                };

                // Load the nearby chargers on startup
                var chargers = mainPageViewModel.LoadChargers();
                if (chargers != null)
                {
                    foreach (var charger in chargers)
                    {
                        var chargerPin = new Pin()
                        {
                            Type = PinType.Place,
                            Label = "Charger",
                            Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Battery-Icon.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Battery-Icon.png", WidthRequest = 10, HeightRequest = 10 }),
                            Position = new Position(Convert.ToDouble(charger.Latitude), Convert.ToDouble(charger.Longitude)),

                        };
                        map.Pins.Add(chargerPin);
                    }
                }

                // Add map to screen stack
                stackLayout.Children.Add(map);

                Services.DoEAPI test = new Services.DoEAPI();
                Debug.WriteLine("HTTP Call");
                test.getAvailableChargersInZip("72704");

                ContentMap.Content = stackLayout;
                ContentMap.IsVisible = true;
                layoutContainer.IsVisible = true;
                lblInfo.Text = "";
                lblInfo.IsVisible = false;

            }
            catch (Exception ex)
            {
                lblInfo.Text = ex.Message.ToString();
                ContentMap.IsVisible = false;
                lblInfo.IsVisible = true;
                layoutContainer.IsVisible = false;
            }
        }

        async void TrackLocation()
        {
            while(true)
            {
                // Retrieve the current location of the user
                var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
                // Move to current location of user with radius of one mile
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(loc.Latitude, loc.Longitude),
                Distance.FromMiles(1)));

                var locationPin = new Pin()
                {
                    Type = PinType.Place,
                    Label = "",
                    //Icon = BitmapDescriptorFactory.FromView(new Image() { Source = "Location-Dot.png", Scale = .25}),
                    Position = new Position(Convert.ToDouble(loc.Latitude), Convert.ToDouble(loc.Longitude)),
                };
                map.Pins.Add(locationPin);
                await Task.Delay(1000);

            }
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
