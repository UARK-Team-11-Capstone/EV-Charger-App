using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

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
      
        public static async Task HTTPRequestAsync(string parameters, string callType)
        {
            HttpClient client = new HttpClient();
            string api_param = "api_key=" + api_key;
            
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chargers.txt");
            Debug.WriteLine("File path: " + fileName);
            
            try
            {
                string URL = requestURL + callType + api_param + parameters;
                HttpResponseMessage response = await client.GetAsync(URL);
                if (response.IsSuccessStatusCode)
                {
                    
                    // Open a file stream and a buffered stream for writing the response content
                    using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        // Read the response content as a stream asynchronously and write it to the file stream using the buffered stream
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            Debug.WriteLine("Writing to file");
                            await contentStream.CopyToAsync(fileStream);

                        }
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


            string file = File.ReadAllText(fileName);
            Debug.WriteLine("Content: " + file);
        }
        public void getAvailableChargersInZip(string zipCode)
        {
            // Parameter for this request
            string param = "&zip=" + zipCode + fuel_type + status_code + ev_connector_type + access;
            Debug.WriteLine("Calling DoE for chargers in ZIP");
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
