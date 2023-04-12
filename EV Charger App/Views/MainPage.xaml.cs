using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.Linq;
using EV_Charger_App.ViewModels;
using System.Threading.Tasks;
using EV_Charger_App.Views;
using EV_Charger_App.Services;
using GoogleApi.Entities.Common;
using System.Collections.Generic;
using GoogleApi.Entities.Maps.Common;
using Distance = Xamarin.Forms.GoogleMaps.Distance;
using Location = Xamarin.Essentials.Location;
using Debug = System.Diagnostics.Debug;
using GoogleApi.Entities.Places.Common;
using Android.Locations;
using Address = GoogleApi.Entities.Common.Address;
using static Android.Icu.Text.Transliterator;
using Position = Xamarin.Forms.GoogleMaps.Position;
using GoogleApi.Entities.Interfaces;
using GoogleApi.Entities.Maps.Directions.Response;

namespace EV_Charger_App
{
    public partial class MainPage : ContentPage
    {
        App app;

        Xamarin.Forms.GoogleMaps.Map map;
        Location previousLocation;
        DoEAPI doe;
        RoutingAPI routeAPI;
        GooglePlacesApi googlePlacesApi;
        List<Prediction> prediction;
        SearchBar lastChanged;
        String lastAddress;
        bool chargerRouting;
        private int chargePercentage;
        private int maxRange;
        private int rechargeMileage;
        public MainPage(App app)
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, true);
            LoadMap(39.5, -98.35);

#pragma warning disable CS0618 // Type or member is obsolete
            map.CameraChanged += Map_CameraChanged;
#pragma warning restore CS0618 // Type or member is obsolete

            map.InfoWindowLongClicked += Map_InfoWindowLongClicked;

            searchBar.TextChanged += (sender, e) => OnTextChanged(sender, e, searchResultsListView, searchBar);
            secondSearchBar.TextChanged += (sender, e) => OnTextChanged(sender, e, searchResultsListView, secondSearchBar);
            secondSearchBar.PropertyChanged += SecondSearchBar_PropertyChanged;
            searchResultsListView.ItemTapped += (sender, e) => ListItemTapped(sender, e, searchResultsListView, searchBar);

            this.app = app;
            doe = new DoEAPI(app.database.GetDOEAPIKey());
            routeAPI = new RoutingAPI();
            googlePlacesApi = new GooglePlacesApi(app.database.GetGoogleAPIKey());
            prediction = new List<Prediction>();
            chargePercentage = 100;
            maxRange = 1;
            rechargeMileage = 10;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Routing Button Clicked event handler
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void OnButtonClicked(object sender, EventArgs e)
        {
            secondSearchBar.IsVisible = true;
            searchBar.Placeholder = "Starting Point";

            // Make sure addresses are valid
            var search1 = await Task.Run(() => googlePlacesApi.GetLocationAsync(searchBar.Text).Result);
            var search2 = await Task.Run(() => googlePlacesApi.GetLocationAsync(secondSearchBar.Text).Result);

            // If the return address is valid then get a route
            if (search1 != null && search2 != null)
            {
                GetRoute(searchBar.Text, secondSearchBar.Text);
            }
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
                    RelativeLayout.SetYConstraint(searchResultsListView, Constraint.RelativeToView(secondSearchBar, (parent, sibling) => sibling.Bounds.Bottom));
                    // Position the lblInfo label below the secondSearchBar
                    RelativeLayout.SetYConstraint(lblInfo, Constraint.RelativeToView(secondSearchBar, (parent, sibling) => sibling.Bounds.Bottom + 70));
                }
                else
                {
                    // Move the searchResultsListView back to its original position
                    RelativeLayout.SetYConstraint(searchResultsListView, Constraint.RelativeToView(searchBar, (parent, sibling) => sibling.Bounds.Bottom));
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
                if (!string.IsNullOrEmpty(e.NewTextValue) && !string.IsNullOrWhiteSpace(e.NewTextValue) && e.NewTextValue != "" && e.NewTextValue != lastAddress)
                {
                    // Take coordinates from previousLocation
                    Coordinate latlng = new Coordinate(previousLocation.Latitude, previousLocation.Longitude);
                    // Send API call based on text and location
                    var response = await googlePlacesApi.AutoComplete(e.NewTextValue, latlng, GetVisibleRadius(map.CameraPosition.Zoom));
                    prediction = (List<Prediction>)response.Predictions;
                    List<string> result = new List<string>();

                    lastChanged = srchBar;

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
                Location selectedPlace = new Location();

                selectedPlace = await Task.Run(() => googlePlacesApi.GetLocationAsync(locationName).Result);

                if (selectedPlace == null)
                {
                    return;
                }

                // Move the map to the selected place
                Position position = new Position(selectedPlace.Latitude, selectedPlace.Longitude);

                Pin pin = new Pin()
                {
                    Label = locationName,
                    Position = position
                };

                map.Pins.Add(pin);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMiles(1)));

                // Set the value in the search bar to the item being tapped and set whichever list is being used to invisible
                lastChanged.Text = locationName;

                await Task.Delay(250);

                listView.IsVisible = false;
                lastAddress = locationName;

            } catch (Exception ex)
            {
                Debug.WriteLine("Error in Tapping Handler: " + $"{ex.Message}");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Overload functions for if the user long clicks on an info card
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void Map_InfoWindowLongClicked(object sender, InfoWindowLongClickedEventArgs e)
        {
            Debug.WriteLine(e.Pin.Label);
            await Navigation.PushAsync(new ChargerInfo(app, GetChargerInfo(e.Pin.Label)));

        }

        //Function to get charger information to pass to charger information page
        string[] GetChargerInfo(string chargerName)
        {
            FuelStation charger = doe.GetFuelStation(chargerName);

            string address = charger.street_address + " " + charger.city + ", " + charger.state;
            string updatedAt = charger.updated_at.ToString();
            string accessibility = charger.access_days_time;

            string rating = app.database.GetChargerRating(chargerName) + "";

            return new string[4] { chargerName, address, updatedAt, accessibility };
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
                
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(latitude,longitude), Distance.FromMiles(1000)));
                
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
            // Get the current DateTime object
            DateTime currentDate = DateTime.Now;

            if (previousLocation != null && pos != null)
            {
                // Call DoE API to get nearest chargers in a radius relative to the camera zoom level
                double alt = pos.Zoom;
                double radius = GetVisibleRadius(alt);
                doe.getNearestCharger(lat.ToString(), lng.ToString(), radius.ToString());

                // Load the nearby chargers on startup
                List<FuelStation> chargers = doe.LoadChargers();
                if (chargers != null)
                {
                    foreach (var charger in chargers)
                    {
                        // Get the difference in last updated for the charger and assign green, yellow, or red status based on this
                        DateTime chargerDate = charger.updated_at;
                        TimeSpan difference = currentDate - chargerDate;

                        string chargerIconName = "";

                        if(difference.TotalDays < 7)
                        {
                            chargerIconName = "Charger-Icon-Green.png";
                        }
                        else if(difference.TotalDays < 31)
                        {
                            chargerIconName = "Charger-Icon-Yellow.png";
                        }
                        else
                        {
                            chargerIconName = "Charger-Icon-Red.png";
                        }

                        var chargerPin = new Pin()
                        {
                            Tag = charger.id,
                            Type = PinType.Place,
                            Label = charger.station_name,
                            Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle(chargerIconName) : BitmapDescriptorFactory.FromView(new Image() { Source = "Charger-Icon-Green.png", WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
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

        //-----------------------------------------------------------------------------------------------------------------------------
        // Call Directions API to get a route between two different locations
        //-----------------------------------------------------------------------------------------------------------------------------
        public async void GetRoute(string originAdd, string destinationAdd)
        {
            Address originAddress = new Address(originAdd);
            Address destinationAddress = new Address(destinationAdd);

            LocationEx origin = new LocationEx(originAddress);
            LocationEx destination = new LocationEx(destinationAddress);

            // Call the routing api
            var result = await routeAPI.GetRouteAsync(origin, destination);

            if (result == null)
            {
                // Handle error
                return;
            }

            // Encoded points of polylines
            var encodedOverviewPolyline = result.Routes.First().OverviewPath.Points;

            // Decoded point of polyline
            var positions = DecodePolyline(encodedOverviewPolyline);

            // If we want to route with charging consideration
            if (chargerRouting == true)
            {
                // Call function to determine which chargers along the route we are going to use
                List<FuelStation> finalRouteChargers = await GetChargingStationsAlongRouteAsync(positions, originAdd, destinationAdd);
                List<Position> finalRoute = new List<Position>();                
                FuelStation prev = new FuelStation();

                // Ensure that we have a valid list of chargers
                if (finalRouteChargers != null)
                {
                    // We need to make API calls between the origin, chargers, and destination and combine the point data
                    foreach (var charger in finalRouteChargers)
                    {
                        string chargerStringAdd = charger.street_address + ", " + charger.city + ", " + charger.state + "," + charger.zip;
                        Address chargerAddress = new Address(chargerStringAdd);
                        LocationEx chargerLocationEx = new LocationEx(chargerAddress);
                        DirectionsResponse response = new DirectionsResponse();

                        // If we are getting the first set of points from origin to the first charger
                        if (charger == finalRouteChargers.First())
                        {
                            // Call the routing api between the origin and charger locationEx
                            response = await routeAPI.GetRouteAsync(origin, chargerLocationEx);
                        }
                        else if (charger == finalRouteChargers.Last())
                        {
                            // Call the routing api between the charger and the destination
                            response = await routeAPI.GetRouteAsync(chargerLocationEx, destination);
                        }
                        else
                        {
                            // Call the routing api between the previous charger and the current charger
                            string prevChargerStringAdd = prev.street_address + ", " + prev.city + ", " + prev.state + "," + prev.zip;
                            Address prevChargerAddress = new Address(chargerStringAdd);
                            LocationEx prevChargerLocationEx = new LocationEx(chargerAddress);
                            response = await routeAPI.GetRouteAsync(chargerLocationEx, prevChargerLocationEx);
                        }

                        // If we get a valid reponse add it to the list
                        if (response != null)
                        {
                            // Add the points to the finalRoute list
                            finalRoute.AddRange(DecodePolyline(response.Routes.First().OverviewPath.Points));
                        }

                        // Keep track of the previous charger
                        prev = charger;
                    }

                    // Create actual polyline
                    var polyline = new Xamarin.Forms.GoogleMaps.Polyline
                    {
                        StrokeColor = Color.Blue,
                        StrokeWidth = 5,
                    };

                    // Add each point to the polyline
                    foreach (var p in positions)
                    {
                        polyline.Positions.Add(p);
                    }

                    // Clear map and add line to map
                    map.Polylines.Clear();
                    map.Polylines.Add(polyline);

                    map.MoveToRegion(MapSpan.FromPositions(positions));
                }
            }
            else
            {
                // Create actual polyline
                var polyline = new Xamarin.Forms.GoogleMaps.Polyline
                {
                    StrokeColor = Color.Blue,
                    StrokeWidth = 5,
                };

                // Add each point to the polyline
                foreach (var p in positions)
                {
                    polyline.Positions.Add(p);
                }

                // Clear map and add line to map
                map.Polylines.Clear();
                map.Polylines.Add(polyline);

                map.MoveToRegion(MapSpan.FromPositions(positions));
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Determine which charging stations to route to given a start and end point
        //-----------------------------------------------------------------------------------------------------------------------------
        public async Task<List<FuelStation>> GetChargingStationsAlongRouteAsync(List<Position> positions, string originAdd, string destinationAdd)
        {
            // Geocode the addresses to obtain coordinates
            var originLocation = await Geocoding.GetLocationsAsync(originAdd);
            var destinationLocation = await Geocoding.GetLocationsAsync(destinationAdd);
            var originLocationLoc = originLocation.FirstOrDefault();
            var destinationLocationLoc = destinationLocation.FirstOrDefault();
            double distance = Location.CalculateDistance(originLocationLoc, destinationLocationLoc, DistanceUnits.Miles);

            // Only route with chargers if the distances is long enough
            if (maxRange * (chargePercentage * 1E-3) < distance)
            {
                List<Position> pos = new List<Position>();

                Position prev = new Position();
                foreach(var ps in positions)
                {
                    if(ps == positions.First())
                    {
                        prev = ps;
                        continue;
                    }
                    else
                    {
                        // If the distance between two points is less than 5 miles we don't need that point
                        if(Location.CalculateDistance(new Location(prev.Latitude, prev.Longitude), new Location(ps.Latitude, ps.Longitude), DistanceUnits.Miles) > 5)
                        {
                            // If we find a point more than five miles away add it to the list and move on
                            pos.Add(ps);
                            prev = ps;
                        }
                        else
                        {
                            prev = ps; continue;
                        }
                    }
                }
                
                // If we ended up with only one charging station make sure we at least have two points for the request
                if(pos.Count < 2)
                {
                    pos.Add(positions.Last());
                }

                // Using the position data get list of chargers along the route from DoE
                Root chargersAlongRoute = doe.getChargersAlongRoute(pos, "2");
                int numChargers = (int)distance / rechargeMileage;

                List<FuelStation> finalRouteChargers = new List<FuelStation>();
                if (chargersAlongRoute != null && chargersAlongRoute.fuel_stations != null)
                {
                    // Loop through each charger provided
                    foreach (var charger in chargersAlongRoute.fuel_stations)
                    {
                        // If we have enough chargers, break
                        if (finalRouteChargers.Count >= numChargers)
                        {
                            break;
                        }

                        // Find the first charger
                        if (finalRouteChargers.Count == 0)
                        {
                            // Find the distance between the origin and the first charger in the list
                            double dist = Location.CalculateDistance(originLocationLoc, new Location(charger.latitude, charger.longitude), DistanceUnits.Miles);
                            if (dist >= rechargeMileage)
                            {
                                finalRouteChargers.Add(charger);
                            }
                            // If the distance from the last charger added to the destination is greater than recharge distance, find the next charger
                        }
                        else if (Location.CalculateDistance(new Location(finalRouteChargers.Last().latitude, finalRouteChargers.Last().longitude), destinationLocationLoc, DistanceUnits.Miles) > rechargeMileage)
                        {

                            // Get distance between the last charger on the route and the next possible charger
                            double dist = Location.CalculateDistance(new Location(finalRouteChargers.Last().latitude, finalRouteChargers.Last().longitude), new Location(charger.latitude, charger.longitude), DistanceUnits.Miles);
                            if (dist >= rechargeMileage)
                            {
                                finalRouteChargers.Add(charger);
                            }
                        }                        
                    }
                    Debug.WriteLine("Num chargers on Route: " + finalRouteChargers.Count);
                    return finalRouteChargers;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Draw the polyline onto the map
        //-----------------------------------------------------------------------------------------------------------------------------
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
            await Navigation.PushAsync(new PagesList(app));
        }

    }
}
