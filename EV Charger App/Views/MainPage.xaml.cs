﻿using EV_Charger_App.Services;
using EV_Charger_App.ViewModels;
using EV_Charger_App.Views;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Maps.Directions.Response;
using GoogleApi.Entities.Places.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.NetworkInformation;
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
        Location previousLocation;
        DoEAPI doe;
        RoutingAPI routeAPI;
        GooglePlacesApi googlePlacesApi;
        List<Prediction> prediction;
        SearchBar lastChanged;
        FunctionThrottler throttle;
        List<Cluster> clusterList;
        string lastAddress;
        bool chargerRouting;
        private int chargePercentage;
        private int maxRange;
        private int rechargeMileage;
        bool overrideLoading;
        private double clusteringThreshold;

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
            throttle = new FunctionThrottler(new TimeSpan(0, 0, 3));     
            overrideLoading = false;
            chargePercentage = 100;
            maxRange = 1;
            rechargeMileage = 5;
            chargerRouting = false;
            clusteringThreshold = 2;
        }

        private void ChargerRoutingClicked(object sender, EventArgs e)
        {
            chargerRouting = !chargerRouting;
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
                    var response = await googlePlacesApi.AutoComplete(e.NewTextValue, latlng,  GetVisibleRadius(map.CameraPosition.Zoom));
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

            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error in Tapping Handler: " + $"{ex.Message}");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Overload functions for if the user double clicks on an info card
        //-----------------------------------------------------------------------------------------------------------------------------
        private async void Map_InfoWindowLongClicked(object sender, InfoWindowLongClickedEventArgs e)
        {
            await Navigation.PushAsync(new ReviewCharger(app, ""));

        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Responds on a camera moved action
        //-----------------------------------------------------------------------------------------------------------------------------
        private void Map_CameraChanged(object sender, CameraChangedEventArgs e)
        {
            CameraPosition pos = e.Position;
            // Check to see if the function is allowed to run
            if (!throttle.CanExecute() || overrideLoading == true)
            {
                DynamicChargerLoadingAsync(pos);
            }
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

                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(latitude, longitude), Distance.FromMiles(1000)));

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
        // Update the location of the users pin every 5 seconds
        //-----------------------------------------------------------------------------------------------------------------------------
        public async void TrackLocation()
        {
            try
            {
                // Intialization
                previousLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(previousLocation.Latitude, previousLocation.Longitude), Xamarin.Forms.GoogleMaps.Distance.FromMiles(1)));
                Debug.WriteLine("Current location found...");

                var locationPin = new Pin()
                {
                    Type = PinType.Place,
                    Label = "Current Location",
                    //Icon = BitmapDescriptorFactory.FromView(new Image() { Source = "Location-Dot.png", Scale = .25}),
                    Position = new Position(Convert.ToDouble(previousLocation.Latitude), Convert.ToDouble(previousLocation.Longitude)),
                };
                Debug.WriteLine("Adding location pin to the map...");
                map.Pins.Add(locationPin);

                while (true)
                {
                    //Debug.WriteLine("Checking current location...");
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
            catch (Exception ex)
            {
                Debug.WriteLine("Error tracking location: " + ex.Message);
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        // Load chargers based on the camera position asynchronously 
        //-----------------------------------------------------------------------------------------------------------------------------
        public void DynamicChargerLoadingAsync(CameraPosition pos)
        {
            Debug.WriteLine("Dynamically loading chargers...");
            double lat = pos.Target.Latitude;
            double lng = pos.Target.Longitude;

            try
            {
                if (previousLocation != null && pos != null)
                {
                    // Call DoE API to get nearest chargers in a radius relative to the camera zoom level
                    double alt = pos.Zoom;
                    double radius = GetVisibleRadius(alt);
                    doe.getNearestCharger(lat.ToString(), lng.ToString(), radius.ToString());
                    // Load the nearby chargers on startup
                    List<FuelStation> chargers = doe.LoadChargers();

                    // Null protection
                    if (chargers == null)
                    {
                        return;
                    }

                    // If the radius of our view is more than 150 miles, cluster our chargers
                    if (radius > 150)
                    {
                        Debug.WriteLine("Radius larger than 150, proceeding to cluster...");
                        // Clear the map of old pins to optimize for clusters
                        map.Pins.Clear();

                        // Call function to update global clusterList
                        ClusterFuelStations(chargers);

                        Debug.WriteLine("Cluster list size: " + clusterList.Count);
                        if (clusterList != null)
                        {
                            // Iterate through clusters and create pins
                            foreach (var cluster in clusterList)
                            {
                                if (cluster.fuel_stations != null)
                                {
                                    // If this is just a cluster of one, meaning one charger, then make a normal pin
                                    if (cluster.fuel_stations.Count == 1)
                                    {
                                        map.Pins.Add(CreatePin(cluster.fuel_stations.FirstOrDefault()));
                                    }
                                    else // Otherwise treat this like a cluster and create an overall pin
                                    {
                                        var clusterPin = new Pin()
                                        {
                                            Tag = cluster.Id,
                                            Type = PinType.SearchResult,
                                            Label = "",
                                            Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Charger-Icon.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Charger-Icon.png", WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                                            Position = new Position(cluster.position.Latitude, cluster.position.Longitude),
                                        };
                                        map.Pins.Add(clusterPin);
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            Debug.WriteLine("Done adding clusters to map...");
                        }
                        else
                        {
                            Debug.WriteLine("ClusterList was null");
                        }
                    }
                    else // Normal situation where the visible radius is less than 150 miles, make pins for all chargers in the visible radius
                    {
                        if (chargers != null)
                        {
                            if (clusterList == null)
                            {
                                foreach (var charger in chargers)
                                {
                                    map.Pins.Add(CreatePin(charger));
                                }
                            }
                            else
                            {
                                List<FuelStation> added = new List<FuelStation>();
                                // Clear clusters if they are present in the current view
                                for (int i = 0; i < map.Pins.Count - 1; i++)
                                {                                    
                                    // Calculate the distance between the cluster and the center of the visible area
                                    double distance = Location.CalculateDistance(new Location(lat, lng), new Location(map.Pins[i].Position.Latitude, map.Pins[i].Position.Longitude), DistanceUnits.Miles);
                                    if (map.Pins[i].Type == PinType.SearchResult && distance <= radius)
                                    {
                                        // Use lat and long as an identifier to find the cluster from the cluster list
                                        Cluster cluster = clusterList.Find(x => x.Id == (int)map.Pins[i].Tag);

                                        // Remove cluster icon and then add back the chargers from the cluster to the map
                                        map.Pins.Remove(map.Pins[i]);
                                        foreach (var charger in cluster.fuel_stations)
                                        {
                                            
                                            map.Pins.Add(CreatePin(charger));
                                            added.Add(charger);
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }                                    
                                }

                                // Ignore duplicates
                                added.AddRange(added.Except(chargers));
                                foreach(var charger in added)
                                {
                                    map.Pins.Add(CreatePin(charger));
                                }
                                

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading chargers: {ex}");
            }
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------
        // Given a FuelStation create a map pin
        //-----------------------------------------------------------------------------------------------------------------------------
        public Pin CreatePin(FuelStation fs)
        {
            try
            {
                // Get the current DateTime object
                DateTime currentDate = DateTime.Now;
                // Get the difference in last updated for the charger and assign green, yellow, or red status based on this
                DateTime chargerDate = fs.updated_at;
                TimeSpan difference = currentDate - chargerDate;

                string chargerIconName = "";

                if (difference.TotalDays < 7)
                {
                    chargerIconName = "Charger-Icon-Green.png";
                }
                else if (difference.TotalDays < 31)
                {
                    chargerIconName = "Charger-Icon-Yellow.png";
                }
                else
                {
                    chargerIconName = "Charger-Icon-Red.png";
                }

                var chargerPin = new Pin()
                {
                    Tag = fs.id,
                    Type = PinType.Place,
                    Label = fs.station_name,
                    Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle(chargerIconName) : BitmapDescriptorFactory.FromView(new Image() { Source = chargerIconName, WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                    Position = new Position(Convert.ToDouble(fs.latitude), Convert.ToDouble(fs.longitude)),
                };

                return chargerPin;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error creating pin: " + ex.Message);
            }

            return null;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Given a list of FuelStations cluster those that are within a quarter-mile radius and return
        //-----------------------------------------------------------------------------------------------------------------------------
        public void ClusterFuelStations(List<FuelStation> fuelStations)
        {
            try
            {
                Debug.WriteLine("Clustering chargers...");
                
                if (fuelStations != null)
                {
                    foreach (var fuelStation in fuelStations)
                    {
                        bool isClustered = false;

                        if (clusterList != null)
                        {
                            // Iterate through existing clusters
                            foreach (var cluster in clusterList)
                            {
                                double distance = Location.CalculateDistance(fuelStation.latitude, fuelStation.longitude, cluster.position.Latitude, cluster.position.Longitude, DistanceUnits.Miles);

                                if (distance <= clusteringThreshold)
                                {
                                    // Add fuel station to existing cluster
                                    cluster.AddFuelStation(fuelStation);
                                    isClustered = true;
                                    break; // No need to check other clusters
                                }
                            }

                            if (!isClustered)
                            {
                                // Create a new cluster for the fuel station
                                Cluster newCluster = new Cluster(fuelStation.latitude, fuelStation.longitude);
                                newCluster.AddFuelStation(fuelStation);
                                newCluster.Id = clusterList.Count;
                                clusterList.Add(newCluster);                                
                            }
                        }
                        else
                        {
                            // Create a new cluster for the fuel station
                            Cluster newCluster = new Cluster(fuelStation.latitude, fuelStation.longitude);
                            newCluster.AddFuelStation(fuelStation);
                            newCluster.Id = 0;
                            clusterList = new List<Cluster>{newCluster};
                        }                        
                    }

                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error clustering: " + ex.Message);
            }

        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Find the relative radius of the camera view
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

                Debug.WriteLine("Chargers found along route, creating route...");
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
                        if (charger == finalRouteChargers.FirstOrDefault())
                        {
                            // Call the routing api between the origin and charger locationEx
                            response = await routeAPI.GetRouteAsync(origin, chargerLocationEx);
                        }
                        else if (charger == finalRouteChargers.LastOrDefault())
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
                            Debug.WriteLine("Adding points to final route line ");
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
                    foreach (var p in finalRoute)
                    {
                        polyline.Positions.Add(p);
                    }

                    // Clear map and add line to map
                    map.Polylines.Clear();
                    map.Polylines.Add(polyline);

                    map.MoveToRegion(MapSpan.FromPositions(finalRoute));
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

                Debug.WriteLine("Calling to get chargers along route with position points: (" + pos.First().Latitude + ", " + pos.First().Longitude + ") " + "(" + pos.Last().Latitude + ", " + pos.Last().Longitude + ")");
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
                            Debug.WriteLine("Reached number of chargers needed");
                            break;
                        }

                        // Find the first charger
                        if (finalRouteChargers.Count == 0)
                        {

                            // Find the distance between the origin and the first charger in the list
                            double dist = Location.CalculateDistance(originLocationLoc, new Location(charger.latitude, charger.longitude), DistanceUnits.Miles);
                            if (dist >= rechargeMileage)
                            {
                                Debug.WriteLine("Added first charger to final route charging");
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
                                Debug.WriteLine("Added middle chargerg to final route charging");
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
