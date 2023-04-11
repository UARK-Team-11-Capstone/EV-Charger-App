using System;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using EV_Charger_App.Views;
using EV_Charger_App.Services;
using Location = Xamarin.Essentials.Location;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Common;
using System.Collections.Generic;
using Distance = Xamarin.Forms.GoogleMaps.Distance;

namespace EV_Charger_App.ViewModels
{
    internal class MapFunctionality
    {
        Xamarin.Forms.GoogleMaps.Map map;
        Xamarin.Essentials.Location previousLocation;
        DoEAPI doe;
        RoutingAPI routeAPI;
        public MapFunctionality(Xamarin.Forms.GoogleMaps.Map map, Xamarin.Essentials.Location previousLocation, DoEAPI doe, RoutingAPI routeAPI) {
            this.map = map;
            this.previousLocation = previousLocation;
            this.doe = doe;
            this.routeAPI = routeAPI;
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
                Root chargers = doe.LoadChargers();
                if (chargers != null)
                {
                    foreach (var charger in chargers.fuel_stations)
                    {

                        // Get the difference in last updated for the charger and assign green, yellow, or red status based on this
                        DateTime chargerDate = charger.updated_at;
                        TimeSpan difference = currentDate - chargerDate;

                        if (difference.TotalDays < 7)
                        {
                            var chargerPin = new Pin()
                            {
                                Type = PinType.Place,
                                Label = charger.station_name,
                                Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Charger-Icon-Green.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Charger-Icon-Green.png", WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                                Position = new Position(Convert.ToDouble(charger.latitude), Convert.ToDouble(charger.longitude)),
                            };
                            map.Pins.Add(chargerPin);
                        }
                        else if (difference.TotalDays < 31)
                        {
                            var chargerPin = new Pin()
                            {
                                Type = PinType.Place,
                                Label = charger.station_name,
                                Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Charger-Icon-Yellow.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Charger-Icon-Yellow.png", WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                                Position = new Position(Convert.ToDouble(charger.latitude), Convert.ToDouble(charger.longitude)),
                            };
                            map.Pins.Add(chargerPin);
                        }
                        else
                        {
                            var chargerPin = new Pin()
                            {
                                Type = PinType.Place,
                                Label = charger.station_name,
                                Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("Charger-Icon-Red.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "Charger-Icon-Red.png", WidthRequest = 10, HeightRequest = 10, Aspect = Aspect.AspectFit }),
                                Position = new Position(Convert.ToDouble(charger.latitude), Convert.ToDouble(charger.longitude)),
                            };
                            map.Pins.Add(chargerPin);
                        }
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Find the relative radius of the camera view
        //-----------------------------------------------------------------------------------------------------------------------------
        public double GetVisibleRadius(double zoomLevel)
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
        // Update the location of the users pin every 5 seconds
        //-----------------------------------------------------------------------------------------------------------------------------
        public async void TrackLocation()
        {
            // Intialization
            previousLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(previousLocation.Latitude, previousLocation.Longitude), Xamarin.Forms.GoogleMaps.Distance.FromMiles(1)));

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
        // Call Directions API to get a route between two different locations
        //-----------------------------------------------------------------------------------------------------------------------------
        public async void GetRoute(string originAdd, string destinationAdd)
        {
            Address originAddress = new Address(originAdd);
            Address destinationAddress = new Address(destinationAdd);

            LocationEx origin = new LocationEx(originAddress);
            LocationEx destination = new LocationEx(destinationAddress);

            var result = await routeAPI.GetRouteAsync(origin, destination);

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
    }
}
