using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace EV_Charger_App.Services
{
    public class DoEAPI
    {
        
        static string api_key = "hFdk47D7j0ulexFWZ3a3IGqQIBJSA46W5srlkAaF";
        static string fuel_type = "&fuel_type=ELEC";
        static string status_code = "&status_code=E";
        static string ev_connector_type = "&ev_connector_type=J1772";
        static string access = "&access=public";

        static string requestURL = "https://developer.nrel.gov/api/alt-fuel-stations/";

        static string callDefault = "v1.json?";
        static string callNearest = "v1/nearest.json";
        static string callNearestRoute = "v1/nearby-route.json?";
        static string callId = ":id.json?";
       
        public DoEAPI()
        {

        }
      
        public async Task<string> HTTPRequestAsync(string parameters, string callType)
        {
            
            HttpClient client = new HttpClient();
            string api_param = "api_key=" + api_key;

            string URL = requestURL + callType + api_param + parameters;
            HttpResponseMessage response = await client.GetAsync(URL);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            // Return HTTP GET request result to function
            return result;
        }

        public string getAvailableChargersInZip(string zipCode)
        {
            // Parameter for this request
            string param = "&zip=" + zipCode + fuel_type + status_code + ev_connector_type + access;
            

            // Collect reponse
            string response = HTTPRequestAsync(param, callDefault).Result;
            return response;
        }

        public string getChargersAlongRoute(string lineString, string distance)
        {
            // Parameter for this request
            string param = "&LINESTRING=" + lineString + "&distance=" + distance + fuel_type + status_code + ev_connector_type + access;
            
            // Collect response
            string response = HTTPRequestAsync(param, callNearestRoute).Result;
            return response;
        }

        public string getNearestCharger(string latitude, string longitude, string radius)
        {
            string param = "&latitude=" + latitude + "&longitude=" + longitude + "&radius=" + radius + fuel_type + status_code + ev_connector_type + access;
            
            // Collect response
            string response = HTTPRequestAsync(param, callNearest).Result;
            return response;
        }
 
        public string getStationByID(string id)
        {
            string param = "&id=" + id;

            // Collect response
            string response = HTTPRequestAsync(param, callId).Result;
            return response;
        }
    }
}
