using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.Linq;
using EV_Charger_App.ViewModels;

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
                var placemarks = await Geocoding.GetPlacemarksAsync(latitude, longitude);

                var placemark = placemarks?.FirstOrDefault();


                map = new Xamarin.Forms.GoogleMaps.Map()
                {
                    Margin = new Thickness(2, 2, 2, 2),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MapType = MapType.Street,
                    IsEnabled = true
                };

            
                Position position = new Position();

                MapSpan mapSpan = new MapSpan(position, latitude, longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(latitude, longitude),
                Distance.FromMiles(1)));

                StackLayout stackLayout = new StackLayout()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    BackgroundColor = Color.Transparent,
                    Orientation = StackOrientation.Vertical
                };
                
                // Load the nearby chargers on startup
                var chargers = mainPageViewModel.LoadChargers();
                Console.WriteLine("Grab charger list");
                if (chargers != null)
                {
                    Console.WriteLine("Chargers not NULL");
                    foreach (var charger in chargers)
                    {
                        var chargerPin = new Pin()
                        {
                            Type = PinType.Place,
                            Label = "Charger",
                            Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Battery-Icon.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Battery-Icon.png", WidthRequest = 30, HeightRequest = 30 }),
                            Position = new Position(Convert.ToDouble(charger.Latitude), Convert.ToDouble(charger.Latitude)),

                        };
                        Console.WriteLine("Add Pin to Map");
                        map.Pins.Add(chargerPin);
                    }

                }

                stackLayout.Children.Add(map);


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

    }
}
