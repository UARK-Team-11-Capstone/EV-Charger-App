using EV_Charger_App.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EV_Charger_App.Services
{
    public class DoEAPI
    {
        private static readonly object _lock = new object();
        HttpClient client = new HttpClient();

        static string api_key = "hFdk47D7j0ulexFWZ3a3IGqQIBJSA46W5srlkAaF";
        static string fuel_type = "&fuel_type=ELEC";
        static string status_code = "&status_code=E";
        static string ev_connector_type = "&ev_connector_type=J1772";
        static string access = "&access=public";

        static string requestURL = "https://developer.nrel.gov/api/alt-fuel-stations/";

        static string callDefault = "v1.json?";
        static string callNearest = "v1/nearest.json?";
        static string callNearestRoute = "v1/nearby-route.json?";
        static string callId = ":id.json?";
        public DoEAPI()
        {

        }
      
        public async Task HTTPRequestAsync(string parameters, string callType)
        {
            
            string api_param = "api_key=" + api_key;
            
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chargers.json");
            //Debug.WriteLine("File path: " + fileName);
            
            try
            {
                string URL = requestURL + callType + api_param + parameters;
                Debug.WriteLine("URL COMMAND: " + URL);
                HttpResponseMessage response = await client.GetAsync(URL);
                if (response.IsSuccessStatusCode)
                {

                    // Copy data from HTTP request to a string
                    string jsonContent = await response.Content.ReadAsStringAsync();
                    // Serialize JSON data
                    //string serializedJson = JsonConvert.SerializeObject(jsonContent);
                    //Debug.WriteLine(serializedJson);

                    // Use lock on file and then write to the file
                    lock (_lock)
                    {
                        File.WriteAllText(fileName, jsonContent);
                    }
                }
                else
                {
                    Debug.WriteLine($"Failed to download JSON file. HTTP response status code: {response.StatusCode}");
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

            //string file = File.ReadAllText(fileName);
            //Debug.WriteLine("Content: " + file);
        }

        public Root LoadChargers()
        {
            try
            {
                // Grab directory path to the JSON file
                string fileName = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Chargers.json");
                
                lock (_lock)
                {
                    string jsonString = File.ReadAllText(fileName);
                    return JsonConvert.DeserializeObject<Root>(jsonString);
                }

            }
            catch (JsonSerializationException ex)
            {
                Console.WriteLine($"Error reading JSON file: {ex.Message}"); 
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }

        }
        public void getAvailableChargersInZip(string zipCode)
        {
            // Parameter for this request
            string param = "&zip=" + zipCode + fuel_type + status_code + ev_connector_type + access;
            //Debug.WriteLine("Calling DoE for chargers in ZIP");
            _ = HTTPRequestAsync(param, callDefault);
        }

        public void getChargersAlongRoute(string lineString, string distance)
        {
            // Parameter for this request
            string param = "&LINESTRING=" + lineString + "&distance=" + distance + fuel_type + status_code + ev_connector_type + access;

            // Collect response
            _ = HTTPRequestAsync(param, callNearestRoute);
        }

        public void getNearestCharger(string latitude, string longitude, string radius)
        {
            string param = "&latitude=" + latitude + "&longitude=" + longitude + "&radius=" + radius + fuel_type + status_code + ev_connector_type + access;
            Debug.WriteLine("Calling DoE for chargers in LAT/LONG radius");
            // Collect response
            _ = HTTPRequestAsync(param, callNearest);
            
        }
 
        public void getStationByID(string id)
        {
            string param = "&id=" + id;

            // Collect response
            _ = HTTPRequestAsync(param, callId);
        }
    }
}
