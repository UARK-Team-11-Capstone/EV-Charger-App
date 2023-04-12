using EV_Charger_App.ViewModels;
using GoogleApi.Entities.Common;
using Newtonsoft.Json;
using Org.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms.GoogleMaps;
using System.Linq;
using Android.App;
using GoogleApi.Entities.Maps.Common;
using Android.Provider;

namespace EV_Charger_App.Services
{
    public class DoEAPI
    {
        private static readonly object _lock = new object();
        HttpClient client = new HttpClient();

        static string api_key;
        static string fuel_type = "&fuel_type=ELEC";
        static string status_code = "&status_code=E";
        static string ev_connector_type = "&ev_connector_type=J1772";
        static string access = "&access=public";

        static string requestURL = "https://developer.nrel.gov/api/alt-fuel-stations/";

        static string callDefault = "v1.json?";
        static string callNearest = "v1/nearest.json?";
        static string callNearestRoute = "v1/nearby-route.json?";
        static string callId = ":id.json?";

        Root chargersAlongRoute;
        Root previousResults;
        Root CHARGER_LIST;
        bool writeToFile;
        public DoEAPI()
        {
            writeToFile = false;
            CHARGER_LIST = new Root();
            chargersAlongRoute = new Root();
            previousResults = new Root();
            api_key = key;
        }
      
        public async void HTTPRequestAsync(string parameters, string callType)
        {
            
            string api_param = "api_key=" + api_key;            

            try
            {
                string URL = requestURL + callType + api_param + parameters;
                Debug.WriteLine(URL);

                HttpResponseMessage response = await client.GetAsync(URL);
                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();
                    ProcessResponse(jsonContent, callDefault);
                }               
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("HTTP Exception: " + ex.Message);
            }          
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
            }
        }

        public async void PostHTTPRequestAsync(string callType, string lineString, string distance)
        {

            string api_param = "api_key=" + api_key;
            // Construct the URL for the API endpoint
            string URL = requestURL + callType + api_param;

            // Create a dictionary to hold the request parameters
            Dictionary<string, string> requestData = new Dictionary<string, string>
            {
                { "route", lineString },
                { "distance", distance},
                { "fuel_type", "ELEC" },
                { "access", "public" },
                { "status_code", "E" },
                { "ev_connector_type", "J1772" }
            };

            // Create a FormUrlEncodedContent object to encode the request data
            var content = new FormUrlEncodedContent(requestData);
            string cont = await content.ReadAsStringAsync();
            Debug.WriteLine("Content for Post Request: " + cont);
            Debug.WriteLine("URL: " + URL + cont);

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
                        Debug.WriteLine("Nearby Route Response:" + responseContent);
                        ProcessResponse(responseContent, callNearestRoute);
                    }
                    else
                    {
                        Debug.WriteLine($"Error in Post Request: {response.StatusCode}");
                    }

                }
            }catch(HttpRequestException ex)
            {
                Debug.WriteLine("Error in POST request: " + ex.Message);
            }
        }

        public void ProcessResponse(string response, string callType)
        {
            // Add toggle for whether or not we want to store in a file
            if (writeToFile == false)
            {
                // Add new chargers to the master object
                var result = JsonConvert.DeserializeObject<Root>(response);
                CHARGER_LIST.fuel_stations = result.fuel_stations.Except(result.fuel_stations).ToList();
            }
            else
            {
                // Copy data from HTTP request to a string
                string fileName = Path.Combine(Application.Context.FilesDir.AbsolutePath, "Chargers.json");
                previousResults = JsonConvert.DeserializeObject<Root>(response);

                Root doeResponse = previousResults;

                // Store chargers separately if from route call
                if (callType == callNearestRoute)
                {
                    chargersAlongRoute = previousResults;
                }

                // If this is the devices first time using the app we need to create the Chargers.json file first
                if (!File.Exists(fileName))
                {
                    File.Create(fileName);
                }

                // Use lock on file and then write to the file
                lock (_lock)
                {
                    // Read current status, append new stations, and then append file
                    string json = File.ReadAllText(fileName);
                    Root curr = JsonConvert.DeserializeObject<Root>(json);

                    // Check to make sure the file wasn't empty
                    if (curr != null)
                    {
                        curr.fuel_stations.AddRange(doeResponse.fuel_stations);

                        string updatedRoot = JsonConvert.SerializeObject(curr);
                        // Serialize object and append file
                        File.WriteAllText(fileName, updatedRoot);
                    }
                    else
                    {
                        File.WriteAllText(fileName, response);
                    }
                }
            }
        }
        public List<FuelStation> LoadChargers()
        {
            if (writeToFile == false)
            {
                return CHARGER_LIST.fuel_stations;
            }
            else
            {
                try
                {
                    // Grab directory path to the JSON file
                    string fileName = Path.Combine(Application.Context.FilesDir.AbsolutePath, "Chargers.json");

                    lock (_lock)
                    {
                        string jsonString = File.ReadAllText(fileName);
                        // We only want to add the stations that are new
                        if (previousResults != null && !string.IsNullOrEmpty(jsonString))
                        {
                            JsonSerializerSettings settings = new JsonSerializerSettings { CheckAdditionalContent = true };
                            List<FuelStation> list = JsonConvert.DeserializeObject<Root>(jsonString).fuel_stations.Except(previousResults.fuel_stations).ToList();
                            return list;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine($"Error reading JSON file: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception loading chargers: " + ex.Message);
                    return null;
                }
            }

        }
        public void getAvailableChargersInZip(string zipCode)
        {
            // Parameter for this request
            string param = "&zip=" + zipCode + fuel_type + status_code + ev_connector_type + access;
            HTTPRequestAsync(param, callDefault);
        }

        public Root getChargersAlongRoute(List<Position> lineString, string distance)
        {
            // Given a list of locations create a request for the DOE
            int count = 0;
            string LineString = "LINESTRING(";
            
            foreach(Position line in lineString)
            {   
                if (count == 0)
                {
                    LineString += line.Latitude + " " +line.Longitude;
                }
                else
                {
                    LineString += "," + line.Latitude + " " + line.Longitude;
                }
                count++;
            }

            LineString += ")";

            Debug.WriteLine("LineString:" + LineString);
            Debug.WriteLine("Calling DoE for chargers along route...");
            
            // Collect response
            PostHTTPRequestAsync(callNearestRoute, LineString, "2.0");

            return chargersAlongRoute;
        }

        public void getNearestCharger(string latitude, string longitude, string radius)
        {
            string param = "&latitude=" + latitude + "&longitude=" + longitude + "&radius=" + radius + fuel_type + status_code + ev_connector_type + access;
            // Collect response
            HTTPRequestAsync(param, callNearest);
            
        }
 
        public void getStationByID(string id)
        {
            string param = "&id=" + id;
            // Collect response
            HTTPRequestAsync(param, callId);
        }
    }
}
