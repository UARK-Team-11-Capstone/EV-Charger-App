using Android.OS;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms.GoogleMaps;

namespace EV_Charger_App.ViewModels
{
    public class Cluster
    {
        public int Id { get; set; }
        public Position position { get; set; }
        public List<FuelStation> fuel_stations { get; set; }
        

        public Cluster(double latitude, double longitude)
        {
            position = new Position(latitude, longitude);
            fuel_stations = new List<FuelStation>();
        }

        public void AddFuelStation(FuelStation fuelStation)
        {
            fuel_stations.Add(fuelStation);
            UpdatePosition(fuelStation.latitude, fuelStation.longitude);
        }

        private void UpdatePosition(double latitude, double longitude)
        {
            if (fuel_stations.Count == 1)
            {
                // If it's the first fuel station in the cluster, set the position to its latitude and longitude
                position = new Position(latitude, longitude);
            }
            else
            {
                try
                {
                    // Update the position by taking the average of the current position and the new latitude and longitude
                    double totalLatitude = position.Latitude * (fuel_stations.Count - 1);
                    double totalLongitude = position.Longitude * (fuel_stations.Count - 1);
                    totalLatitude += latitude;
                    totalLongitude += longitude;
                    double updatedLatitude = totalLatitude / fuel_stations.Count;
                    double updatedLongitude = totalLongitude / fuel_stations.Count;
                    position = new Position(updatedLatitude, updatedLongitude);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error setting cluster position: " + ex.Message);
                }
            }
        }
    }
}
