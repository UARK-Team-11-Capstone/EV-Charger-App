using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.GoogleMaps.Extensions;
using System.Linq;
using EV_Charger_App.ViewModels;
using System.Threading.Tasks;
using EV_Charger_App.Views;
using EV_Charger_App.Services;
using GoogleApi.Entities.Common;
using System.Collections.Generic;
using Android.OS;
using System.Diagnostics;
using GoogleApi;
using GoogleApi.Entities.Maps.Common;
using Android.App.AppSearch;
using Distance = Xamarin.Forms.GoogleMaps.Distance;
using Location = Xamarin.Essentials.Location;
using Debug = System.Diagnostics.Debug;
using GoogleApi.Entities.Places.Common;
using GoogleApi.Entities.Maps.Directions.Response;
using GoogleApi.Entities.Interfaces;

namespace EV_Charger_App
{
    public partial class MainPage : ContentPage
    {

        Xamarin.Forms.GoogleMaps.Map map;
        Location previousLocation;
        DoEAPI doe = new DoEAPI();
        RoutingAPI routeapi = new RoutingAPI();
        GooglePlacesApi googlePlacesApi = new GooglePlacesApi();
        List<Prediction> prediction = new List<Prediction>();
        public MainPage()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, true);
            LoadMap(36.09171012916079, -94.20143973570228);

#pragma warning disable CS0618 // Type or member is obsolete
            map.CameraChanged += Map_CameraChanged;
#pragma warning restore CS0618 // Type or member is obsolete

            map.InfoWindowLongClicked += Map_InfoWindowLongClicked;

            searchBar.TextChanged += (sender, e) => OnTextChanged(sender, e, searchResultsListView, searchBar);
            secondSearchBar.TextChanged += (sender, e) => OnTextChanged(sender, e, searchResultsListView, secondSearchBar);
            secondSearchBar.PropertyChanged += SecondSearchBar_PropertyChanged;
            searchResultsListView.ItemTapped += (sender, e) => ListItemTapped(sender, e, searchResultsListView, searchBar);

        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Trigger if the second search bar becomes visible
        //-----------------------------------------------------------------------------------------------------------------------------
        private void SecondSearchBar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsVisible")
            {
                if (secondSearchBar.IsVisible)
                {
                    // Move the searchResultsListView down by the height of the secondSearchBar
                    RelativeLayout.SetYConstraint(searchResultsListView, Constraint.RelativeToView(secondSearchBar, (parent, sibling) => sibling.Height + 10));
                    // Position the lblInfo label below the secondSearchBar
                    RelativeLayout.SetYConstraint(lblInfo, Constraint.RelativeToView(secondSearchBar, (parent, sibling) => sibling.Height + 70));
                }
                else
                {
                    // Move the searchResultsListView back to its original position
                    RelativeLayout.SetYConstraint(searchResultsListView, Constraint.Constant(50));
                    // Position the lblInfo label below the searchResultsListView
                    RelativeLayout.SetYConstraint(lblInfo, Constraint.RelativeToView(searchResultsListView, (parent, sibling) => sibling.Height + 60));
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // If the text changes in the search bar send a query for an autocomplete
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void OnTextChanged(object sender, TextChangedEventArgs e, ListView list, SearchBar srchBar)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.NewTextValue))
                {
                    // Take coordinates from previousLocation
                    Coordinate latlng = new Coordinate(previousLocation.Latitude, previousLocation.Longitude);
                    // Send API call based on text and location
                    var response = await googlePlacesApi.AutoComplete(e.NewTextValue, latlng, GetVisibleRadius(map.CameraPosition.Zoom));
                    prediction = (List<Prediction>)response.Predictions;
                    List<string> result = new List<string>();

                    // If response is not null then display the possible results in the list view
                    if (response != null)
                    {
                        foreach (var pred in response.Predictions)
                        {
                            result.Add(pred.Description);
                        }
                        list.ItemsSource = result;
                        list.IsVisible = true;
                    }
                    else
                    {
                        list.ItemsSource = null;
                        list.IsVisible = false;
                    }
                }
                else
                {
                    list.ItemsSource = null;
                    list.IsVisible = false;
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("Error calling autocomplete: " + ex.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // If user taps on item in prediction list move to that location
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void ListItemTapped(object sender, ItemTappedEventArgs e, ListView listView, SearchBar srchBar)
        {
            try
            {
                string locationName = e.Item.ToString();
                Debug.WriteLine("Item tapped" + locationName);
                Location selectedPlace = new Location();

                selectedPlace = await Task.Run(() => googlePlacesApi.GetLocationAsync(locationName).Result);

                Debug.WriteLine("Location retrieved: " + selectedPlace.ToString());
                if (selectedPlace == null)
                {
                    Debug.WriteLine("SelectePlace is null");
                    Debug.WriteLine(e.Item.ToString());
                    return;
                }

                // Move the map to the selected place
                Position position = new Position(selectedPlace.Latitude, selectedPlace.Longitude);
                Debug.WriteLine("Position is: " + position.Latitude + ", " + position.Longitude);

                Pin pin = new Pin()
                {
                    Label = locationName,
                    Position = position
                };

                map.Pins.Add(pin);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMiles(1)));

                // Set the value in the search bar to the item being tapped and set whichever list is being used to invisible
                srchBar.Text = locationName;
                listView.IsVisible = false;

                // If we have inputted our first destination make the second search bar visible
                if (srchBar == searchBar)
                {
                    secondSearchBar.IsVisible = true;
                }

            } catch (Exception ex)
            {
                Debug.WriteLine("Error in Tapping Handler: " + $"{ex.Message}");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Overload functions for if the user double clicks on an info card
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void Map_InfoWindowLongClicked(object sender, InfoWindowLongClickedEventArgs e)
        {
            await Navigation.PushAsync(new ReviewCharger());

        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Responds on a camera moved action
        //-----------------------------------------------------------------------------------------------------------------------------
        private void Map_CameraChanged(object sender, CameraChangedEventArgs e)
        {
            CameraPosition pos = e.Position;
            DynamicChargerLoadingAsync(pos);
        }



        //-----------------------------------------------------------------------------------------------------------------------------
        // Intialize the Google Map
        //-----------------------------------------------------------------------------------------------------------------------------
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

        //-----------------------------------------------------------------------------------------------------------------------------
        // Update the location of the users pin every 5 seconds
        //-----------------------------------------------------------------------------------------------------------------------------
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

        //-----------------------------------------------------------------------------------------------------------------------------
        // Load chargers based on the camera position asynchronously 
        //-----------------------------------------------------------------------------------------------------------------------------
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
                            Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Charger-Icon.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Charger-Icon.png", WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                            Position = new Position(Convert.ToDouble(charger.latitude), Convert.ToDouble(charger.longitude)),

                        };
                        map.Pins.Add(chargerPin);
                    }
                }

            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Find the relative radius of the camera view
        //-----------------------------------------------------------------------------------------------------------------------------
        public static double GetVisibleRadius(double zoomLevel)
        {
            // Based on pixel five
            int screenWidth = 1080;
            int screenHeight = 2340;
            double mapAspectRatio = screenHeight / screenWidth;

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

        public async void MakeRoute()
        {
            Address originAddress = new Address("");
            Address destinationAddress = new Address("");

            LocationEx origin = new LocationEx(originAddress);
            LocationEx destination = new LocationEx(destinationAddress);
            
            var result = await routeapi.GetRouteAsync(origin, destination);

            if (result == null)
            {
                // Handle error
                return;
            }

            var encodedOverviewPolyline = result.Routes.First().OverviewPath.Points;

            var positions = DecodePolyline(encodedOverviewPolyline);

            var polyline = new Xamarin.Forms.GoogleMaps.Polyline
            {
                StrokeColor = Color.Blue,
                StrokeWidth = 5,
            };

            foreach (var p in positions)
            {
                polyline.Positions.Add(p);
            }

            map.Polylines.Clear();
            map.Polylines.Add(polyline);

            map.MoveToRegion(MapSpan.FromPositions(positions));
        }

        public static List<Position> DecodePolyline(string encodedPoints)
        {
            var poly = new List<Position>();
            int index = 0, len = encodedPoints.Length;
            int lat = 0, lng = 0;

            while (index < len)
            {
                int b, shift = 0, result = 0;
                do
                {
                    b = encodedPoints[index++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);

                int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += dlat;

                shift = 0;
                result = 0;
                do
                {
                    b = encodedPoints[index++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);

                int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += dlng;

                poly.Add(new Position(lat / 1E5, lng / 1E5));
            }

            return poly;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        //This gets called when you click the menu bar on the ribbon
        // Will send the user to the page containing a list of pages
        // (map screen link, login screen link, settings link)
        //-----------------------------------------------------------------------------------------------------------------------------
        async private void ListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PagesList());
        }

    }
}
