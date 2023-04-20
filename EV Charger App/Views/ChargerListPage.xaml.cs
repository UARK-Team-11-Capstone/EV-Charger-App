using Android.OS;
using EV_Charger_App.Services;
using EV_Charger_App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using static EV_Charger_App.ViewModels.FuelStation;
using ColorStatus = EV_Charger_App.ViewModels.FuelStation.ColorStatus;
using Debug = System.Diagnostics.Debug;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChargerListPage : ContentPage
    {
        List<FuelStation> listOfChargers;
        DoEAPI doe;
        App app;
        MainPage main;
        public ChargerListPage(App app, DoEAPI doe, MainPage main)
        {
            InitializeComponent();
            this.doe = doe;
            this.app = app;
            this.main = main;
            listOfChargers = new List<FuelStation>(doe.CHARGER_LIST.fuel_stations);

            // Get distance of each charger from the user
            foreach (var fuelStation in listOfChargers)
            {
                fuelStation.distanceFromUser = main.GetDistanceFromUser(new Xamarin.Essentials.Location(fuelStation.latitude, fuelStation.longitude));
            }

            // Sort the fuel stations by distance from the user
            listOfChargers.Sort((x, y) => x.distanceFromUser.CompareTo(y.distanceFromUser));

            // Set the binding context for the frontend
            fuelStationsListView.ItemsSource = listOfChargers;           
            
        }

        private async void OnFuelStationSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
            {
                return;
            }

            var selectedFuelStation = (FuelStation)e.SelectedItem;

            await Navigation.PushAsync(new ChargerInfo(app, doe.GetChargerInfo(selectedFuelStation.station_name)));
            
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------
    // Convert the color status of the charger into the colorstatus for the view cell
    //-----------------------------------------------------------------------------------------------------------------------------
    public class ColorStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ColorStatus status)
            {
                switch (status)
                {
                    case ColorStatus.Green:                        
                        return new Color(108,194,74);
                    case ColorStatus.Yellow:
                        return new Color(240,179,35);
                    case ColorStatus.Red:
                        return new Color(227,82,5);
                }
            }
            return Color.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}