using EV_Charger_App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms.GoogleMaps;
using Debug = System.Diagnostics.Debug;
using Location = Xamarin.Essentials.Location;

namespace EV_Charger_App.Services
{
    internal class MapPinHandler
    {
        MainPage main;
        DoEAPI doe;
        double clusteringThreshold;
        int clusterDistance;

        FuelStationEqualityComparer fuelStationEqualityComparer;
        PinEqualityComparer pinEqualityComparer;
        List<Cluster> CLUSTER_LIST;
        List<Cluster> NEW_CLUSTERS;
        Dictionary<FuelStation, (Pin, Cluster)> CHARGER_PIN_CLUSTER_DICTIONARY;
        FunctionThrottler throttler;

        public MapPinHandler(DoEAPI doe, MainPage main)
        {
            this.doe = doe;
            this.main = main;

            clusteringThreshold = 2;
            clusterDistance = 50;
            CHARGER_PIN_CLUSTER_DICTIONARY = new Dictionary<FuelStation, (Pin, Cluster)>();
            CLUSTER_LIST = new List<Cluster>();
            NEW_CLUSTERS = new List<Cluster>();
            pinEqualityComparer = new PinEqualityComparer();
            fuelStationEqualityComparer = new FuelStationEqualityComparer();
            throttler = new FunctionThrottler(new TimeSpan(0, 0, 0, 1));
        }

        public async Task LoadChargersAsync(Location loc, double radius)
        {
            try
            {
                if (radius > 500) { return; }
                // Call the DoE to get chargers at the current location and given radius                
                await doe.getNearestCharger(loc.Latitude, loc.Longitude, radius);

                if (doe.NEW_CHARGERS.fuel_stations == null) { return; }

                // Add the new chargers to our overall dictionary
                List<FuelStation> newChargers = doe.NEW_CHARGERS.fuel_stations;
                var newChargerEntries = newChargers.ToDictionary(c => c, c => ((Pin)null, (Cluster)null), fuelStationEqualityComparer);
                CHARGER_PIN_CLUSTER_DICTIONARY = CHARGER_PIN_CLUSTER_DICTIONARY.Concat(newChargerEntries.Where(x => !CHARGER_PIN_CLUSTER_DICTIONARY.Keys.Contains(x.Key))).ToDictionary(d => d.Key, d => d.Value);

                // Cluster
                if (radius > clusterDistance)
                {
                    //Debug.WriteLine("Determining which chargers to cluster...");
                    // Grab all the chargers that are not clustered
                    var chargersToCluster = CHARGER_PIN_CLUSTER_DICTIONARY.Where(x => x.Value.Item2 == null).Select(x => x.Key).ToList();
                    var chargersToRemove = CHARGER_PIN_CLUSTER_DICTIONARY.Where(x => x.Value.Item1 != null && x.Value.Item2 == null).Select(x => x.Key).ToList();
                    await ClusterChargers(chargersToCluster);

                    // Debug.WriteLine("Attempting to remove charger pins from map");
                    if (chargersToRemove != null)
                    {
                        // Attempt to remove charger pins based on BindingContext
                        foreach (var charger in chargersToRemove)
                        {
                            main.RemovePin(charger);
                        }
                    }

                    //Debug.WriteLine("Attempting to add cluster pins to map | " + CLUSTER_LIST.Count + " clusters");
                    foreach (var cluster in CLUSTER_LIST)
                    {
                        // If there is no pin associated with the cluster
                        if (cluster.BindingContext == null)
                        {
                            // Create the cluster pin and grab its reference
                            var clusterPin = main.CreatePin("", cluster.position, DateTime.MinValue, cluster.id.ToString(), PinType.SearchResult, pinEqualityComparer);
                            cluster.BindingContext = clusterPin;

                            // Update charger dictionary entry and BindingContext for each charger
                            foreach (var charger in cluster.fuel_stations)
                            {
                                var (pin, oldCluster) = CHARGER_PIN_CLUSTER_DICTIONARY[charger];
                                charger.BindingContext = clusterPin;
                                UpdateDictionary(charger, clusterPin, cluster);
                            }
                        }
                        else
                        {
                            // If there is already a pin associated with the cluste just upadte the charger dictionary entries
                            foreach (var charger in cluster.fuel_stations)
                            {
                                var (pin, oldCluster) = CHARGER_PIN_CLUSTER_DICTIONARY[charger];
                                charger.BindingContext = (Pin)cluster.BindingContext;
                                UpdateDictionary(charger, (Pin)cluster.BindingContext, cluster);
                            }
                        }
                    }
                }
                else // Decluster and add new chargers
                {
                    foreach (var cluster in CLUSTER_LIST.ToList())
                    {
                        double distance = Location.CalculateDistance(loc, cluster.location, DistanceUnits.Miles);
                        if (distance < radius)
                        {
                            // Remove cluster pin from the map
                            bool result = main.RemovePin(cluster);

                            // Add each charger back to the map and update its record and BindingContext with new pin and null cluster
                            foreach (var charger in cluster.fuel_stations)
                            {
                                var chargerPin = main.CreatePin(charger, pinEqualityComparer);
                                charger.BindingContext = chargerPin;

                                if (chargerPin != null)
                                {
                                    UpdateDictionary(charger, chargerPin, null);
                                }
                            }

                            // Only remove the cluster if we successfully removed it
                            if (result == true)
                            {
                                // Also remove object from CLUSTER_LIST
                                CLUSTER_LIST.Remove(cluster);
                            }
                        }
                    }

                    // Add new chargers to the map and update dictionary
                    foreach (var dictionaryEntry in CHARGER_PIN_CLUSTER_DICTIONARY.Where(x => x.Value.Item1 == null && x.Value.Item2 == null).ToList())
                    {
                        var chargerPin = main.CreatePin(dictionaryEntry.Key, pinEqualityComparer);
                        dictionaryEntry.Key.BindingContext = chargerPin;

                        if (chargerPin != null)
                        {
                            UpdateDictionary(dictionaryEntry.Key, chargerPin, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading chargers: " + ex.Message + " | " + ex.StackTrace);
            }
        }

        public async Task ClusterChargers(List<FuelStation> chargersToCluster)
        {
            try
            {
                foreach (var charger in chargersToCluster)
                {
                    bool isClustered = false;
                    foreach (var cluster in CLUSTER_LIST)
                    {
                        double distance = Location.CalculateDistance(charger.location, cluster.location, DistanceUnits.Miles);
                        if (distance < clusteringThreshold)
                        {
                            // Add fuel station to existing cluster
                            cluster.AddFuelStation(charger);
                            isClustered = true;
                            break; // No need to check other clusters
                        }
                    }
                    // If not close enough to a premade cluster make a new cluster
                    if (!isClustered)
                    {
                        // Create a new cluster, add the charger, and add the cluster to the list
                        Cluster newCluster = new Cluster(charger.latitude, charger.longitude);
                        newCluster.AddFuelStation(charger);
                        CLUSTER_LIST.Add(newCluster);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error clustering chargers " + ex.Message + " | " + ex.StackTrace);
            }
        }

        public void UpdateDictionary(FuelStation charger, Pin pin, Cluster cluster)
        {
            try
            {
                CHARGER_PIN_CLUSTER_DICTIONARY[charger] = (pin, cluster);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error updating dictionary: " + ex.Message);
            }
        }
    }

    public class PinEqualityComparer : IEqualityComparer<Pin>
    {
        public PinEqualityComparer()
        {

        }
        public bool Equals(Pin x, Pin y)
        {

            if (x is null || y is null)
            {
                return false;
            }

            Debug.WriteLine($"Pin Comparison: {x.Label} == {y.Label} | {x.Position.Latitude} == {y.Position.Latitude} | {x.Position.Longitude} == {y.Position.Longitude} | {x.Tag} == {y.Tag}");

            return x.Label == y.Label
                && x.Position.Latitude == y.Position.Latitude
                && x.Position.Longitude == y.Position.Longitude
                && x.Tag == y.Tag;
        }

        public int GetHashCode(Pin obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + obj.Label.GetHashCode();
                hash = hash * 31 + obj.Position.Latitude.GetHashCode();
                hash = hash * 31 + obj.Position.Longitude.GetHashCode();
                hash = hash * 31 + (obj.Tag?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
