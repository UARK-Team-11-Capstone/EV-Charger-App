using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Places.QueryAutoComplete.Request;
using GoogleApi.Entities.Places.QueryAutoComplete.Response;

namespace EV_Charger_App.Services
{
   public class GooglePlacesApi
    {
        private readonly string apiKey = "AIzaSyAg3OyUuTc-u3Q28HD3D3PGErkVDv0fTkc";
        public GooglePlacesApi() 
        {

        }

        public async Task<PlacesQueryAutoCompleteResponse> QueryAutoComplete(string input, Coordinate latlng, double radius)
        {

            var autoCompleteRequest = new PlacesQueryAutoCompleteRequest()
            {
                Key = apiKey,
                Input = input,
                Location = latlng,
                Radius = radius
            };

            var autoCompleteResponse = await GooglePlaces.QueryAutoComplete.QueryAsync(autoCompleteRequest); 

            return autoCompleteResponse;

        }
    }
    
}
