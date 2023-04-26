using System;
using System.Collections.Generic;
using Xamarin.Forms.GoogleMaps;

namespace EV_Charger_App.ViewModels
{
    public class BD
    {
        public int total { get; set; }
    }

    public class CNG
    {
        public int total { get; set; }
    }

    public class E85
    {
        public int total { get; set; }
    }

    public class ELEC
    {
        public int total { get; set; }
        public Stations stations { get; set; }
    }

    public class EvNetworkIds
    {
        public List<string> station { get; set; }
        public List<string> posts { get; set; }
    }

    public class Fuels
    {
        public BD BD { get; set; }
        public E85 E85 { get; set; }
        public ELEC ELEC { get; set; }
        public HY HY { get; set; }
        public LNG LNG { get; set; }
        public CNG CNG { get; set; }
        public LPG LPG { get; set; }
    }

    public class FuelStation
    {
        public enum ColorStatus
        {
            Green, Yellow, Red
        }
        public string access_code { get; set; }
        public string access_days_time { get; set; }
        public object access_detail_code { get; set; }
        public object cards_accepted { get; set; }
        public string date_last_confirmed { get; set; }
        public object expected_date { get; set; }
        public string fuel_type_code { get; set; }
        public string groups_with_access_code { get; set; }
        public int id { get; set; }
        public string open_date { get; set; }
        public object owner_type_code { get; set; }
        public string status_code { get; set; }
        public object restricted_access { get; set; }
        public string station_name { get; set; }
        public string station_phone { get; set; }
        public DateTime updated_at { get; set; }
        public object facility_type { get; set; }
        public string geocode_status { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string street_address { get; set; }
        public string zip { get; set; }
        public string country { get; set; }
        public List<string> ev_connector_types { get; set; }
        public string ev_network { get; set; }
        public object ev_pricing { get; set; }
        public object access_days_time_fr { get; set; }
        public object ev_pricing_fr { get; set; }
        public double distanceFromUser { get; set; }
        public object BindingContext { get; set; }
        public ColorStatus colorStatus { get; set; }
        public Position position { get; set; }
        public Xamarin.Essentials.Location location { get; set; }

        /// <summary>
        /// ToString for FuelStation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $" {station_name}, {id},  {updated_at}";
        }

        /// <summary>
        /// Determines if two FuelStation objects are equivalent based on name, address, and access code
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            FuelStation other = (FuelStation)obj;
            // compare all properties
            return station_name == other.station_name
                   && street_address == other.street_address
                   && access_code == other.access_code;
        }

        /// <summary>
        /// Returns hashcode of FuelStation object
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = hashCode * 23 + station_name.GetHashCode();
            hashCode = hashCode * 23 + street_address.GetHashCode();
            hashCode = hashCode * 23 + access_code.GetHashCode();
            return hashCode;
        }

    }
    public class FuelStationEqualityComparer : IEqualityComparer<FuelStation>
    {
        public bool Equals(FuelStation x, FuelStation y)
        {
            // Check for nullity and reference equality
            if (x == null || y == null || ReferenceEquals(x, y))
            {
                return x == y;
            }

            // Compare station name, latitude, and longitude
            return x.station_name == y.station_name &&
                   x.latitude == y.latitude &&
                   x.longitude == y.longitude &&
                   x.street_address == y.street_address;
        }

        public int GetHashCode(FuelStation obj)
        {
            // Compute hash code based on station name, latitude, and longitude
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.station_name.GetHashCode();
                hash = hash * 23 + obj.latitude.GetHashCode();
                hash = hash * 23 + obj.longitude.GetHashCode();
                hash = hash * 23 + obj.street_address.GetHashCode();
                return hash;
            }
        }
    }



    public class HY
    {
        public int total { get; set; }
    }

    public class LNG
    {
        public int total { get; set; }
    }

    public class LPG
    {
        public int total { get; set; }
    }

    public class Precision
    {
        public int value { get; set; }
        public string name { get; set; }
        public List<string> types { get; set; }
    }

    public class Root
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string location_country { get; set; }
        public Precision precision { get; set; }
        public string station_locator_url { get; set; }
        public int total_results { get; set; }
        public StationCounts station_counts { get; set; }
        public int offset { get; set; }
        public List<FuelStation> fuel_stations { get; set; }
    }

    public class StationCounts
    {
        public int total { get; set; }
        public Fuels fuels { get; set; }
    }

    public class Stations
    {
        public int total { get; set; }
    }

}
