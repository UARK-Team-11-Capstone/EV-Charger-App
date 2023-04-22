using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Maps.Directions.Request;
using GoogleApi.Entities.Maps.Directions.Response;
using GoogleApi.Entities.Places.AutoComplete.Request;
using GoogleApi.Entities.Places.AutoComplete.Response;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Location = Xamarin.Essentials.Location;

namespace EV_Charger_App.Services
{
    public class GooglePlacesApi
    {
        private static string apiKey;
        public GooglePlacesApi(string key)
        {
            apiKey = key;
        }

        /// <summary>
        /// Send request to Google API for autocomplete results given a coordinate and radius 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="latlng"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Send request to Google Api for route resuls given a start and ending location
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public async Task<DirectionsResponse> GetRouteAsync(LocationEx origin, LocationEx destination)
        {
            var request = new DirectionsRequest
            {
                Origin = origin,
                Destination = destination,
                Key = apiKey
            };

            var response = await GoogleApi.GoogleMaps.Directions.QueryAsync(request);

            if (response.Status != GoogleApi.Entities.Common.Enums.Status.Ok)
            {
                // Handle error
                return null;
            }

            return response;

        }

        /// <summary>
        /// Given a string return a coordinate
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Location> GetLocationAsync(string address)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    var locations = await Geocoding.GetLocationsAsync(address);
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
