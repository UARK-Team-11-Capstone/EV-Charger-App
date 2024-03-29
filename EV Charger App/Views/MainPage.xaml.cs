﻿using EV_Charger_App.Services;
using EV_Charger_App.ViewModels;
using EV_Charger_App.Views;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Maps.Directions.Response;
using GoogleApi.Entities.Places.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Address = GoogleApi.Entities.Common.Address;
using Debug = System.Diagnostics.Debug;
using Distance = Xamarin.Forms.GoogleMaps.Distance;
using Location = Xamarin.Essentials.Location;
using Math = System.Math;
using Position = Xamarin.Forms.GoogleMaps.Position;

namespace EV_Charger_App
{
    public partial class MainPage : ContentPage
    {
        App app;

        Xamarin.Forms.GoogleMaps.Map map;

        DoEAPI doe;
        GooglePlacesApi googlePlacesApi;
        List<Prediction> prediction;
        SearchBar lastChanged;
        FunctionThrottler throttle;

        string lastAddress;
        bool chargerRouting;
        private Location previousLocation;
        bool overrideLoading;
        MapPinHandler mapPinHandler;
        private int maxRange;
        private double chargePercentage;
        private int rechargeMileage;
        private bool routeVisible;

        public MainPage(App app)
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, true);
            NavigationPage.SetHasBackButton(this, false);
            _ = LoadMapAsync(39.5, -98.35);

#pragma warning disable CS0618 // Type or member is obsolete
            map.CameraChanged += Map_CameraChangedAsync;
#pragma warning restore CS0618 // Type or member is obsolete

            map.InfoWindowLongClicked += MapInfoWindowLongClicked;
            map.InfoWindowClicked += MapInfoWindowClicked;

            searchBar.TextChanged += (sender, e) => OnTextChanged(sender, e, searchResultsListView, searchBar);
            secondSearchBar.TextChanged += (sender, e) => OnTextChanged(sender, e, searchResultsListView, secondSearchBar);
            secondSearchBar.PropertyChanged += SecondSearchBarPropertyChanged;
            searchResultsListView.ItemTapped += (sender, e) => ListItemTapped(sender, e, searchResultsListView, searchBar);

            this.app = app;
            doe = new DoEAPI(app, app.database.GetDOEAPIKey());
            mapPinHandler = new MapPinHandler(doe, this);
            googlePlacesApi = new GooglePlacesApi(app.database.GetGoogleAPIKey());
            prediction = new List<Prediction>();
            throttle = new FunctionThrottler(new TimeSpan(0, 0, 2));
            overrideLoading = false;
            routeVisible = false;

            rechargeMileage = 50;
            maxRange = 200;

            chargePercentage = app.session.getVehicleCharge();

            if (chargePercentage == 0)
            {
                BatteryIcon.Source = "Battery_Icon_0";
            }
            else if (chargePercentage > 0 && chargePercentage <= 25)
            {
                BatteryIcon.Source = "Battery_Icon_25";
            }
            else if (chargePercentage > 25 && chargePercentage <= 50)
            {
                BatteryIcon.Source = "Battery_Icon_50";
            }
            else if (chargePercentage > 50 && chargePercentage <= 75)
            {
                BatteryIcon.Source = "Battery_Icon_75";
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handler for menu bar button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        async private void ListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserSettings(app, this, doe));
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handler for the charger routing toggle button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        private void ChargerRoutingClicked(object sender, EventArgs e)
        {
            chargerRouting = !chargerRouting;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handler for when the routing button is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void OnButtonClicked(object sender, EventArgs e)
        {
            if (routeVisible == false)
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
                    searchResultsListView.IsVisible = false;
                }
            }
            else
            {
                map.Polylines.Clear();
                foreach (var pin in map.Pins.Where(x => x.Type == PinType.SavedPin).ToList())
                {
                    RemovePin(pin, null);
                }
                secondSearchBar.Text = string.Empty;
                searchBar.Text = string.Empty;
                routeVisible = false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handler that triggers if the second search bar becomes visible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        private void SecondSearchBarPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        /// <summary>
        /// Handler for if the text changes in a search bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="list"></param>
        /// <param name="srchBar"></param>
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
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error calling autocomplete: " + ex.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handler for if the user selects an autocomplete entry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="listView"></param>
        /// <param name="srchBar"></param>
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

                CreatePin(locationName, position, DateTime.MinValue, "", PinType.SavedPin, null);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMiles(1)));

                // Set the value in the search bar to the item being tapped and set whichever list is being used to invisible
                lastChanged.Text = locationName;

                await Task.Delay(250);

                listView.IsVisible = false;
                lastAddress = locationName;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in Tapping Handler: " + $"{ex.Message}");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Overload functions for if the user long clicks on an info card
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void MapInfoWindowLongClicked(object sender, InfoWindowLongClickedEventArgs e)
        {
            if (e.Pin.Type == PinType.Place)
            {
                await Navigation.PushAsync(new ChargerInfo(app, doe.GetChargerInfo(e.Pin.Label)));
            }
            else
            {
                return;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Overload functions for if the user clicks on an info card
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void MapInfoWindowClicked(object sender, InfoWindowClickedEventArgs e)
        {
            if (e.Pin.Type == PinType.Place)
            {
                await Navigation.PushAsync(new ChargerInfo(app, doe.GetChargerInfo(e.Pin.Label)));
            }
            else
            {
                return;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handler for camera position changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void Map_CameraChangedAsync(object sender, CameraChangedEventArgs e)
        {
            CameraPosition pos = e.Position;
            // Check to see if the function is allowed to run
            if (!throttle.CanExecute() || overrideLoading == true)
            {
                Location loc = new Location(pos.Target.Latitude, pos.Target.Longitude);
                double radius = GetVisibleRadius(pos.Zoom);
                await mapPinHandler.LoadChargersAsync(loc, radius);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes the map for the app given a cooridnate
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------------------------------------------------------
        public async Task LoadMapAsync(double latitude, double longitude)
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

                // Move map over USA
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(latitude, longitude), Distance.FromMiles(1000)));

                // Intialization of user location
                previousLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(previousLocation.Latitude, previousLocation.Longitude), Xamarin.Forms.GoogleMaps.Distance.FromMiles(1)));

                CreatePin("Current Location", new Position(Convert.ToDouble(previousLocation.Latitude), Convert.ToDouble(previousLocation.Longitude)), DateTime.MinValue, "", PinType.Generic, null);

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
            catch (System.Exception ex)
            {
                lblInfo.Text = ex.Message.ToString();
                ContentMap.IsVisible = false;
                lblInfo.IsVisible = true;
                layoutContainer.IsVisible = false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Continuously checks to see if the users location has changed
        /// </summary>
        //-----------------------------------------------------------------------------------------------------------------------------
        public async void TrackLocation()
        {
            try
            {
                while (true)
                {
                    // Retrieve the current location of the user
                    var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
                    // Only move if location has changed
                    if (loc != previousLocation)
                    {
                        // Find the current location pin and adjust the location
                        var currLoc = map.Pins.FirstOrDefault(Pin => Pin.Label == "Current Location");
                        if (currLoc.Label == "Current Location")
                        {
                            currLoc.Position = new Position(Convert.ToDouble(loc.Latitude), Convert.ToDouble(loc.Longitude));
                        }
                        else
                        {
                            CreatePin("Current Location", new Position(Convert.ToDouble(previousLocation.Latitude), Convert.ToDouble(previousLocation.Longitude)), DateTime.MinValue, "", PinType.Generic, null);
                        }
                    }
                    // Set previousLocation to the current location
                    previousLocation = loc;

                    // Wait two seconds
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error tracking location: " + ex.Message);
                TrackLocation();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get distance from the current location of the user on the map
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------------------------------------------------------
        public double GetDistanceFromUser(Location loc)
        {
            if (previousLocation != null && loc != null)
            {
                double distance = Location.CalculateDistance(previousLocation, loc, DistanceUnits.Miles);
                return distance;
            }
            else
            {
                return double.NegativeInfinity;
            }

        }
        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Remove pin based on cluster binding context
        /// </summary>
        /// <param name="pin"></param>
         //-----------------------------------------------------------------------------------------------------------------------------
        public bool RemovePin(Cluster cluster)
        {
            try
            {
                if (cluster != null)
                {
                    bool result = map.Pins.Remove((Pin)cluster.BindingContext);
                    if (result == false)
                    {
                        var pin = (Pin)cluster.BindingContext;
                        result = map.Pins.Remove(map.Pins.First(x => x.Tag == x.Tag));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to remove pin: " + ex.Message + ex.StackTrace);
            }
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Remove pin based on charger binding context
        /// </summary>
        /// <param name="pin"></param>
         //-----------------------------------------------------------------------------------------------------------------------------
        public bool RemovePin(FuelStation charger)
        {
            try
            {
                if (charger != null)
                {
                    bool result = map.Pins.Remove((Pin)charger.BindingContext);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing pin: " + ex.Message + ex.StackTrace);
            }
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Remove given pin from map
        /// </summary>
        /// <param name="pin"></param>
         //-----------------------------------------------------------------------------------------------------------------------------
        public bool RemovePin(Pin pin, PinEqualityComparer pinEqualityComparer)
        {
            try
            {
                if (pinEqualityComparer != null)
                {
                    var pinToRemove = map.Pins.FirstOrDefault(p => pinEqualityComparer.Equals(p, pin));
                    if (pinToRemove != null)
                    {
                        bool result = map.Pins.Remove(pinToRemove);
                        return result;
                    }
                }
                else
                {
                    var pinToRemove = map.Pins.FirstOrDefault(p => p == pin);
                    if (pinToRemove != null)
                    {
                        bool result = map.Pins.Remove(pinToRemove);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing pin: " + ex.Message + ex.StackTrace);
            }
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Remove list of pins from the map 
        /// </summary>
        /// <param name="pins"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        public void RemovePin(List<Pin> pins, PinEqualityComparer pinEqualityComparer)
        {
            if (pins != null)
            {
                foreach (var pin in pins)
                {
                    RemovePin(pin, pinEqualityComparer);
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// If pin exists on the map return reference to it, otherwise return null
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------------------------------------------------------
        public Pin ContainsPin(Pin pin)
        {
            if (map.Pins.Contains(pin))
            {
                return map.Pins.FirstOrDefault(p => Equals(p, pin));
            }
            else
            {
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Given pin parameters create a pin on the map (pin object is already added to map but is returned for record keeping)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pos"></param>
        /// <param name="time"></param>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------------------------------------------------------
        public Pin CreatePin(string name, Position pos, DateTime time, string id, PinType type, PinEqualityComparer pinEqualityComparer)
        {
            try
            {
                string iconName = "";
                if (type == PinType.Place)
                {
                    // Get the current DateTime object
                    DateTime currentDate = DateTime.Now;
                    // Get the difference in last updated for the charger and assign green, yellow, or red status based on this
                    DateTime chargerDate = time;
                    TimeSpan difference = currentDate - chargerDate;

                    if (difference.TotalDays < 7)
                    {
                        iconName = "Charger-Icon-Green.png";
                    }
                    else if (difference.TotalDays < 31)
                    {
                        iconName = "Charger-Icon-Yellow.png";
                    }
                    else
                    {
                        iconName = "Charger-Icon-Red.png";
                    }
                }
                else if (type == PinType.SearchResult)
                {
                    iconName = "Charger-Icon.png";
                }
                else if (type == PinType.SavedPin)
                {
                    var placePin = new Pin()
                    {
                        Tag = id,
                        Type = type,
                        Label = name,
                        Position = pos
                    };

                    // Check to see if the pin already exists, if so return that pin, otherwise add new pin
                    var result1 = ContainsPin(placePin);
                    if (result1 == null)
                    {
                        map.Pins.Add(placePin); return placePin;
                    }
                    else
                    {
                        return result1;
                    }
                }
                else
                {
                    iconName = "Location-Dot.png";
                }


                var pin = new Pin()
                {
                    Tag = id,
                    Type = type,
                    Label = name,
                    Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle(iconName) : BitmapDescriptorFactory.FromView(new Image() { Source = iconName, WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                    Position = pos
                };

                // Check to see if the pin already exists, if so return that pin, otherwise add new pin
                var result = ContainsPin(pin);
                if (result == null)
                {
                    map.Pins.Add(pin); return pin;
                }
                else
                {
                    return result;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error creating pin: " + ex.Message);
            }

            return new Pin();
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create pin and add to map based on a FuelStation object, return pin for record keeping
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------------------------------------------------------
        public Pin CreatePin(FuelStation fs, PinEqualityComparer pinEqualityComparer)
        {
            try
            {
                string chargerIconName = "";
                if (fs.colorStatus == FuelStation.ColorStatus.Green)
                {
                    chargerIconName = "Charger-Icon-Green.png";
                }
                else if (fs.colorStatus == FuelStation.ColorStatus.Yellow)
                {
                    chargerIconName = "Charger-Icon-Yellow.png";
                }
                else
                {
                    chargerIconName = "Charger-Icon-Red.png";
                }

                var pin = new Pin()
                {
                    Tag = fs.id,
                    Type = PinType.Place,
                    Label = fs.station_name,
                    Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle(chargerIconName) : BitmapDescriptorFactory.FromView(new Image() { Source = chargerIconName, WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                    Position = new Position(Convert.ToDouble(fs.latitude), Convert.ToDouble(fs.longitude)),
                };

                // Check to see if the pin already exists, if so return that pin, otherwise add new pin
                var result = ContainsPin(pin);
                if (result == null)
                {
                    map.Pins.Add(pin); return pin;
                }
                else
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error creating pin: " + ex.Message);
            }
            return null;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Returns the radius of the users view based on the zoom level of the camera
        /// </summary>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------------------------------------------------------
        public double GetVisibleRadius(double zoomLevel)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting visible radius: " + ex.Message);
            }
            return 0;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Computes and displays route between a given origin and destination address
        /// </summary>
        /// <param name="originAdd"></param>
        /// <param name="destinationAdd"></param>
        //-----------------------------------------------------------------------------------------------------------------------------
        public async void GetRoute(string originAdd, string destinationAdd)
        {
            Address originAddress = new Address(originAdd);
            Address destinationAddress = new Address(destinationAdd);

            LocationEx origin = new LocationEx(originAddress);
            LocationEx destination = new LocationEx(destinationAddress);

            // Call the routing api
            var result = await googlePlacesApi.GetRouteAsync(origin, destination);

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
                        CreatePin(charger, null);
                        string chargerStringAdd = charger.street_address + ", " + charger.city + ", " + charger.state + "," + charger.zip;
                        Address chargerAddress = new Address(chargerStringAdd);
                        LocationEx chargerLocationEx = new LocationEx(chargerAddress);
                        DirectionsResponse response = new DirectionsResponse();

                        // If we are getting the first set of points from origin to the first charger
                        if (charger == finalRouteChargers.FirstOrDefault())
                        {
                            // Call the routing api between the origin and charger locationEx
                            response = await googlePlacesApi.GetRouteAsync(origin, chargerLocationEx);
                        }
                        else
                        {
                            // Call the routing api between the previous charger and the current charger
                            string prevChargerStringAdd = prev.street_address + ", " + prev.city + ", " + prev.state + "," + prev.zip;
                            Address prevChargerAddress = new Address(prevChargerStringAdd);
                            LocationEx prevChargerLocationEx = new LocationEx(prevChargerAddress);
                            response = await googlePlacesApi.GetRouteAsync(prevChargerLocationEx, chargerLocationEx);
                        }

                        // If we get a valid reponse add it to the list
                        if (response != null)
                        {
                            // Add the points to the finalRoute list
                            finalRoute.AddRange(DecodePolyline(response.Routes.First().OverviewPath.Points));
                        }

                        if (charger == finalRouteChargers.LastOrDefault())
                        {
                            // Call the routing api between the charger and the destination
                            response = await googlePlacesApi.GetRouteAsync(chargerLocationEx, destination);

                            if (response != null)
                            {
                                // Add the points to the finalRoute list
                                finalRoute.AddRange(DecodePolyline(response.Routes.First().OverviewPath.Points));
                            }
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
                    foreach (var p in finalRoute)
                    {
                        polyline.Positions.Add(p);
                    }

                    // Clear map and add line to map
                    map.Polylines.Clear();
                    map.Polylines.Add(polyline);

                    map.MoveToRegion(MapSpan.FromPositions(finalRoute));

                    routeVisible = true;
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
                routeVisible = true;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Returns a list of charging stations and even intervals along a given route 
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="originAdd"></param>
        /// <param name="destinationAdd"></param>
        /// <returns></returns>
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
                foreach (var ps in positions)
                {
                    if (ps == positions.First())
                    {
                        prev = ps;
                        continue;
                    }
                    else
                    {
                        double dist = Location.CalculateDistance(new Location(prev.Latitude, prev.Longitude), new Location(ps.Latitude, ps.Longitude), DistanceUnits.Miles);
                        // If the distance between two points is less than 5 miles we don't need that point
                        if (dist > 2)
                        {
                            // If we find a point more than five miles away add it to the list and move on
                            pos.Add(ps);
                            prev = ps;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                // If we ended up with only one charging station make sure we at least have two points for the request
                if (pos.Count < 2)
                {
                    pos.Add(positions.Last());
                }

                // Using the position data get list of chargers along the route from DoE
                Root chargersAlongRoute = await doe.getChargersAlongRouteAsync(pos, "2");
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
        /// <summary>
        /// Returns a List<Positions> given a string of endcoded points
        /// </summary>
        /// <param name="encodedPoints"></param>
        /// <returns></returns>
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
    }
}
