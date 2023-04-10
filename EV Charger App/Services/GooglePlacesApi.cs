using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Maps.Directions.Request;
using GoogleApi.Entities.Maps.Directions.Response;
using GoogleApi.Entities.Places.AutoComplete.Request;
using GoogleApi.Entities.Places.AutoComplete.Response;
using Xamarin.Essentials;
using Location = Xamarin.Essentials.Location;

namespace EV_Charger_App.Services
{
   public class GooglePlacesApi
    {
        private readonly string apiKey = "AIzaSyAg3OyUuTc-u3Q28HD3D3PGErkVDv0fTkc";
        public GooglePlacesApi() 
        {

        }

        public async Task<PlacesAutoCompleteResponse> AutoComplete(string input, Coordinate latlng, double radius)
        {

            var autoCompleteRequest = new PlacesAutoCompleteRequest()
            {
                Key = apiKey,
                Input = input,
                Location = latlng,
                Radius = radius
            };

            var autoCompleteResponse = await GooglePlaces.AutoComplete.QueryAsync(autoCompleteRequest); 

            return autoCompleteResponse;

        }

        public async Task<DirectionsResponse> Directions(string input, Coordinate latlng, double radius)
        {
            var directions = new DirectionsRequest()
            {
                Key= apiKey
            };

            var directionsResponse = await GoogleMaps.Directions.QueryAsync(directions);
            
            return directionsResponse;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Provided with an address get the coordinate location
        //-----------------------------------------------------------------------------------------------------------------------------
        public async Task<Location> GetLocationAsync(string address)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    Debug.WriteLine("Getting coordinates....");
                    var locations = await Geocoding.GetLocationsAsync(address);
                    Debug.WriteLine("Coordinate found: " + locations.First().Latitude + locations.First().Longitude);
                    return locations.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}");
            }
            return null;
        }

    }
}
