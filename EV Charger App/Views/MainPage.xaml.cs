using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.Linq;
using EV_Charger_App.ViewModels;
using System.Threading.Tasks;
using EV_Charger_App.Views;
using EV_Charger_App.Services;
using Android.Util;
using Android.Views;
using System.Collections.Generic;
using Android.Graphics;
using Android.Webkit;
using GoogleApi.Entities.Places.AutoComplete.Request;
using System.Diagnostics;
using GoogleApi.Entities.Common.Enums;

namespace EV_Charger_App
{
    public partial class MainPage : ContentPage
    {
        MainPageViewModel mainPageViewModel;
        Xamarin.Forms.GoogleMaps.Map map;
        Location previousLocation;
        DoEAPI doe = new DoEAPI();
        public MainPage()
        {
            InitializeComponent();

            BindingContext = mainPageViewModel = new MainPageViewModel();

            NavigationPage.SetHasNavigationBar(this, true);
            LoadMap(36.09171012916079, -94.20143973570228);

            #pragma warning disable CS0618 // Type or member is obsolete
            map.CameraChanged += Map_CameraChanged;
            #pragma warning restore CS0618 // Type or member is obsolete

            map.InfoWindowLongClicked += Map_InfoWindowLongClicked;
            
        }

        // Overload functions for if the user double clicks on an info card
        private void Map_InfoWindowLongClicked(object sender, InfoWindowLongClickedEventArgs e)
        {
            // Add code here to open up a different view of charger information
            // @kate and @grant

        }

        // Responds on a camera moved action
        private void Map_CameraChanged(object sender, CameraChangedEventArgs e)
        {
            CameraPosition pos = e.Position;
            DynamicChargerLoadingAsync(pos);
        }

        // Intialize the Google Map
        public void LoadMap(double latitude, double longitude)
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
                    BackgroundColor = System.Drawing.Color.Transparent,
                    Orientation = StackOrientation.Vertical
                };

                // Add map to screen stack
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

        // Update the location of the users pin every 5 seconds
        async void TrackLocation()
        {
            // Intialization
            previousLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(previousLocation.Latitude, previousLocation.Longitude), Distance.FromMiles(1)));

            var locationPin = new Pin()
            {
                Type = PinType.Place,
                Label = "Current Location",
                //Icon = BitmapDescriptorFactory.FromView(new Image() { Source = "Location-Dot.png", Scale = .25}),
                Position = new Position(Convert.ToDouble(previousLocation.Latitude), Convert.ToDouble(previousLocation.Longitude)),
            };
            map.Pins.Add(locationPin);

            // Call DoE API to get nearest chargers in a radius relative to the camera zoom level
            double alt = map.CameraPosition.Zoom;
            double radius = GetVisibleRadius(alt);
            doe.getNearestCharger(previousLocation.Latitude.ToString(), previousLocation.Longitude.ToString(), radius.ToString());

            while (true)
            {
                // Retrieve the current location of the user
                var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
                // Only move if location has changed
                if (loc != previousLocation)
                {
                    // Move to current location of user with radius of one mile
                    // map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(loc.Latitude, loc.Longitude), Distance.FromMiles(1)));
                    // Find the current location pin and adjust the location
                    Pin currLoc = map.Pins.First(Pin => Pin.Label == "Current Location");
                    currLoc.Position = new Position(Convert.ToDouble(loc.Latitude), Convert.ToDouble(loc.Longitude));

                }
                // Set previousLocation to the current location
                previousLocation = loc;

                // Wait two seconds
                await Task.Delay(2000);

            }
        }

        // Load chargers based on the camera position asynchronously 
        public void DynamicChargerLoadingAsync(CameraPosition pos)
        {
            double lat = pos.Target.Latitude;
            double lng = pos.Target.Longitude;

            if (previousLocation != null && pos != null)
            {
                // Call DoE API to get nearest chargers in a radius relative to the camera zoom level
                double alt = pos.Zoom;
                double radius = GetVisibleRadius(alt);
                doe.getNearestCharger(lat.ToString(), lng.ToString(), radius.ToString());
                //doe.getAvailableChargersInZip("72704");
                // Load the nearby chargers on startup
                Root chargers = doe.LoadChargers();
                if (chargers != null)
                {
                    foreach (var charger in chargers.fuel_stations)
                    {
                        var chargerPin = new Pin()
                        {
                            Type = PinType.Place,
                            Label = charger.station_name,
                            Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Battery-Icon.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Battery-Icon.png", WidthRequest = 10, HeightRequest = 10 }),
                            Position = new Position(Convert.ToDouble(charger.latitude), Convert.ToDouble(charger.longitude)),

                        };
                        map.Pins.Add(chargerPin);
                    }
                }

            }
        }

        // Find the relative radius of the camera view
        public static double GetVisibleRadius(double zoomLevel)
        {
            // Based on pixel five
            int screenWidth = 1080;
            int screenHeight = 2340; 
            double mapAspectRatio = screenHeight/screenWidth;

            // Calculate the dimensions of the visible area in pixels
            double visibleWidth = screenWidth;
            double visibleHeight = screenWidth / mapAspectRatio;

            // Calculate the visible area in meters using the Mercator projection
            double metersPerPixel = 156543.03392 * Math.Cos(0) / Math.Pow(2, zoomLevel);
            double visibleWidthMeters = visibleWidth * metersPerPixel;
            double visibleHeightMeters = visibleHeight * metersPerPixel;

            // Calculate the visible radius in miles
            double visibleAreaMeters = Math.PI * visibleWidthMeters * visibleHeightMeters;
            double visibleRadiusMiles = Math.Sqrt(visibleAreaMeters) / 1609.344;

            return visibleRadiusMiles;
        }

        private async Task<List<AutocompletePrediction>> GetPlaces(string query)
        {
            var places = new List<AutocompletePrediction>();

            try
            {
                var result = await new PlaceAutocompleteRequest
                {
                    Input = query,
                    Language = "en",
                    Types = AutocompleteType.Address,
                    Components = new ComponentFilter[] { new ComponentFilter(ComponentFilterType.Country, "US") }
                }.GetResponseAsync();

                if (result != null && result.Status == Status.Ok)
                {
                    places = result.AutoCompletePlaces.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting places: {ex.Message}");
            }

            return places;
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var query = e.NewTextValue;

            if (!string.IsNullOrEmpty(query))
            {
                var places = await GetPlaces(query);

                if (places != null && places.Count > 0)
                {
                    predictionList.ItemsSource = places;
                    predictionList.IsVisible = true;
                }
                else
                {
                    predictionList.ItemsSource = null;
                    predictionList.IsVisible = false;
                }
            }
            else
            {
                predictionList.ItemsSource = null;
                predictionList.IsVisible = false;
            }
        }

        private void OnPredictionSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var prediction = e.SelectedItem as AutocompletePrediction;

            if (prediction != null)
            {
                searchBar.Text = prediction.Description;
                predictionList.ItemsSource = null;
                predictionList.IsVisible = false;
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
