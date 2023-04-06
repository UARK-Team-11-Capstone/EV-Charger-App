using System;
using System.Collections.Generic;


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
        public object intersection_directions { get; set; }
        public object plus4 { get; set; }
        public string state { get; set; }
        public string street_address { get; set; }
        public string zip { get; set; }
        public string country { get; set; }
        public List<string> ev_connector_types { get; set; }
        public object ev_dc_fast_num { get; set; }
        public object ev_level1_evse_num { get; set; }
        public int ev_level2_evse_num { get; set; }
        public string ev_network { get; set; }
        public string ev_network_web { get; set; }
        public object ev_other_evse { get; set; }
        public object ev_pricing { get; set; }
        public object ev_renewable_source { get; set; }
        public object access_days_time_fr { get; set; }
        public object intersection_directions_fr { get; set; }
        public object bd_blends_fr { get; set; }
        public string groups_with_access_code_fr { get; set; }
        public object ev_pricing_fr { get; set; }
        public EvNetworkIds ev_network_ids { get; set; }
        public double distance { get; set; }
        public double distance_km { get; set; }
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
