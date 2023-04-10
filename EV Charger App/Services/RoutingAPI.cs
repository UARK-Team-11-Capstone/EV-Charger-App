using System.Net.Http;
using System;
using Xamarin.Essentials;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using EV_Charger_App.ViewModels;
using GoogleApi;
using GoogleApi.Entities.Maps.Directions.Request;
using GoogleApi.Entities.Maps.Directions.Response;
using GoogleApi.Entities.Search.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Interfaces;
using Xamarin.Forms.GoogleMaps;

namespace EV_Charger_App.Services
{
    public class RoutingAPI
    {
        private readonly string apiKey = "AIzaSyAg3OyUuTc-u3Q28HD3D3PGErkVDv0fTkc";
        
        
        public RoutingAPI()
        {

        }

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
    }
}