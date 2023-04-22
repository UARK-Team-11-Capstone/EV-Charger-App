using Android.App;
using EV_Charger_App.ViewModels;
using GoogleApi.Entities.Maps.Elevation.Response;
using GoogleApi.Entities.Search.Common;
using Newtonsoft.Json;
using Org.Apache.Http.Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms.GoogleMaps;
using static Android.Telephony.CarrierConfigManager;
using Location = Xamarin.Essentials.Location;

namespace EV_Charger_App.Services
{
    public class DoEAPI
    {
        private static readonly object _lock = new object();
        HttpClient client = new HttpClient();

        private static string api_key;
        static string fuel_type = "&fuel_type=ELEC";
        static string status_code = "&status_code=E";
        static string ev_connector_type = "&ev_connector_type=J1772";
        static string access = "&access=public";
        static string limit = "&limit=100";
        string api_param = "api_key=";

        static string requestURL = "https://developer.nrel.gov/api/alt-fuel-stations/";

        static string callDefault = "v1.json?";
        static string callNearest = "v1/nearest.json?";
        static string callNearestRoute = "v1/nearby-route.json?";
        static string callId = ":id.json?";

        Root chargersAlongRoute;
        Root previousResults;
        public Root CHARGER_LIST;
        public Root NEW_CHARGERS;
        private int requestThreshold;

        App app;

        public DoEAPI(App app, string key)
        {            
            NEW_CHARGERS = new Root();
            CHARGER_LIST = new Root();
            chargersAlongRoute = new Root();
            previousResults = new Root();
            api_key = key;
            api_param += key;
            this.app = app;

            requestThreshold = 200;
        }


        //Function to get charger information to pass to charger information page
        public string[] GetChargerInfo(string chargerName)
        {
            FuelStation charger = GetFuelStation(chargerName);

            string address = charger.street_address + " " + charger.city + ", " + charger.state;
            string updatedAt = charger.updated_at.ToString();
            string available = charger.access_days_time;

            string rating = app.database.GetChargerRating(chargerName) + "";
            string accessible = app.database.GetAccessibilityInfo(chargerName);

            Debug.WriteLine("[GetChargerInfo] Accessible? : " + accessible);

            return new string[6] { chargerName, address, updatedAt, available, rating, accessible };
        }

        public async Task HTTPRequestAsync(string parameters, string callType)
        {
            
            try
            {
                string URL = requestURL + callType + api_param + parameters;

                HttpResponseMessage response = await client.GetAsync(URL);
                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();
                    ProcessResponse(jsonContent, callDefault);
                }
                else
                {
                    Debug.WriteLine("Error from DoE: " + response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("HTTP Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("In HTTPRequest Exception: " + ex.Message);
            }
        }

        public async Task PostHTTPRequestAsync(string callType, string lineStringPOST, string getParam, string distance)
        {

            string api_param = "api_key=" + api_key;
            // Construct the URL for the API endpoint
            string URL = requestURL + callType + api_param;

            // Create a dictionary to hold the request parameters
            Dictionary<string, string> requestData = new Dictionary<string, string>
            {
                { "route", lineStringPOST },
                { "distance", distance},
                { "fuel_type", "ELEC" },
                { "access", "public" },
                { "status_code", "E" },
                { "ev_connector_type", "J1772" }
            };

            // Create a FormUrlEncodedContent object to encode the request data
            var content = new FormUrlEncodedContent(requestData);
            string cont = await content.ReadAsStringAsync();            

            try
            {
                // Create an HttpClient object
                using (var httpClient = new HttpClient())
                {
                    // Send a POST request to the API endpoint with the request data
                    var response = await httpClient.PostAsync(URL, content);

                    // Check if the response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string responseContent = await response.Content.ReadAsStringAsync();                        
                        ProcessResponse(responseContent, callNearestRoute);
                    }
                    else
                    {
                        await HTTPRequestAsync(getParam, callNearestRoute);
                    }

                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("Error in POST request: " + ex.Message);
            }
        }

        public void ProcessResponse(string response, string callType)
        {
            try
            {               
                // Grab result                    
                var result = JsonConvert.DeserializeObject<Root>(response);
                if (result != null)
                {
                    // If getting chargers for a route add to chargersAlongRoute list
                    if (callType == callNearestRoute)
                    {
                        if (chargersAlongRoute.fuel_stations == null)
                        {
                            chargersAlongRoute.fuel_stations = new List<FuelStation>(result.fuel_stations);
                        }
                        else
                        {
                            chargersAlongRoute.fuel_stations.Clear();
                            chargersAlongRoute.fuel_stations.AddRange(result.fuel_stations);
                            SetStatus(chargersAlongRoute.fuel_stations);
                        }
                    }
                    // If the app is initializing both will be null and both should be intialized with same values
                    if (NEW_CHARGERS.fuel_stations == null && CHARGER_LIST.fuel_stations == null)
                    {
                        SetStatus(result.fuel_stations);
                        CHARGER_LIST.fuel_stations = new List<FuelStation>(result.fuel_stations);
                        NEW_CHARGERS.fuel_stations = new List<FuelStation>(result.fuel_stations);
                    }
                    else
                    {
                        // Clear NEW_CHARGRES before adding response
                        NEW_CHARGERS.fuel_stations.Clear();
                        // Reset NEW_CHARGERS for the new response and only add new chargers to the CHARGER_LIST
                        var list = result.fuel_stations.Except(CHARGER_LIST.fuel_stations);

                        NEW_CHARGERS.fuel_stations.AddRange(list);

                        // Set the Green, Yellow, Red status of the new stations
                        SetStatus(NEW_CHARGERS.fuel_stations);
                        CHARGER_LIST.fuel_stations.AddRange(NEW_CHARGERS.fuel_stations);

                    }
                }
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error processing HTTP response: " + ex.Message);
            }
        }

        public void SetStatus(List<FuelStation> list)
        {
            if(list != null)
            {
                foreach(var charger in list)
                {
                    // Get the current DateTime object
                    DateTime currentDate = DateTime.Now;
                    // Get the difference in last updated for the charger and assign green, yellow, or red status based on this
                    DateTime chargerDate = charger.updated_at;
                    TimeSpan difference = currentDate - chargerDate;

                    if (difference.TotalDays < 7)
                    {
                        charger.colorStatus = FuelStation.ColorStatus.Green;                       
                    }
                    else if (difference.TotalDays < 31)
                    {
                        charger.colorStatus = FuelStation.ColorStatus.Yellow;                        
                    }
                    else
                    {
                        charger.colorStatus = FuelStation.ColorStatus.Red;
                    }

                    charger.position = new Position(charger.latitude, charger.longitude);
                    charger.location = new Location(charger.latitude, charger.longitude);
                }
            }
        }       
        public void getAvailableChargersInZip(string zipCode)
        {
            // Parameter for this request
            string param = "&zip=" + zipCode + fuel_type + status_code + ev_connector_type + access + limit;
            _ = HTTPRequestAsync(param, callDefault);
        }

        public Root getChargersAlongRoute(List<Position> lineStringPOS, string distance)
        {
            // Given a list of locations create a request for the DOE
            int count = 0;
            string lineStringPOST = "LINESTRING(";
            string lineStringGET = "LINESTRING(";

            foreach (Position line in lineStringPOS)
            {
                if (count == 0)
                {
                    lineStringPOST += line.Longitude + " " + line.Latitude;
                    lineStringGET += line.Longitude + "+" + line.Latitude;
                }
                else
                {
                    lineStringPOST += "," + line.Longitude + " " + line.Latitude;
                    lineStringGET += "," + line.Longitude + "+" + line.Latitude;
                }
                count++;
            }

            lineStringPOST += ")";
            lineStringGET += ")";

            string param = "&distance=2" + "&route=" + lineStringGET + fuel_type + status_code + ev_connector_type + access + limit;
            
            // Collect response
            _ = PostHTTPRequestAsync(callNearestRoute, lineStringPOST, param, "2.0");
            return chargersAlongRoute;
        }

        public async Task getNearestCharger(double latitude, double longitude, double radius)
        {   
            if(radius < requestThreshold)
            {
                string param = "&latitude=" + latitude + "&longitude=" + longitude + "&radius=" + radius + fuel_type + status_code + ev_connector_type + access + limit;
                // Collect response
                await HTTPRequestAsync(param, callNearest);
            }                              
        }

        public void getStationByID(string id)
        {
            string param = "&id=" + id;
            // Collect response
            _ = HTTPRequestAsync(param, callId);
        }

        public FuelStation GetFuelStation(string stationName)
        {
            foreach(FuelStation station in CHARGER_LIST.fuel_stations) 
            { 
                if(station.station_name == stationName)
                {
                    return station;
                }
            }
            Debug.WriteLine("Could not find station");
            return new FuelStation();
        }
    }
}
