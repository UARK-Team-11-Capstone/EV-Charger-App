using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace EV_Charger_App.ViewModels
{
    public partial class MainPageViewModel : Component
    {
        public MainPageViewModel()
        {
        }

        public class ChargerLocations
        {
            public string Latitude { get; set; }
            public string Longitude { get; set; }

        }

        internal List<ChargerLocations> LoadChargers()
        {
            List<ChargerLocations> chargerLocations = new List<ChargerLocations>()
            {
                new ChargerLocations{Latitude = "36.09165811091142", Longitude = "-94.20148265104417"}

            };
            return chargerLocations;
        }
    }
}
