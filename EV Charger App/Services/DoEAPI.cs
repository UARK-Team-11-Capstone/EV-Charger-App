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

        static string RequestURL = "https://developer.nrel.gov/api/alt-fuel-stations/v1.json?";
        public DoEAPI()
        {

        }
      
        public async Task<string> HTTPRequestAsync(String parameters)
        {
            
            HttpClient client = new HttpClient();
            string api_param = "api_key=" + api_key;

            string URL = RequestURL + api_param + parameters;
            HttpResponseMessage response = await client.GetAsync(URL);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            // Return HTTP GET request result to function
            return result;
        }


        // Functions for various charger information calls
        public void getChargerStatus()
        {

        }

        //
        public void getNearestCharger()
        {

        }

  
    }
}
