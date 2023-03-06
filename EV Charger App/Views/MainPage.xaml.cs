using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.Linq;

namespace EV_Charger_App
{
    public partial class MainPage : ContentPage
    {
        Xamarin.Forms.GoogleMaps.Map map;
        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, true);
            LoadMap(19.0605421, 72.8618913);
            
        }

        public async void LoadMap(double latitude, double longitude)
        {
            try
            {

                var placemarks = await Geocoding.GetPlacemarksAsync(latitude, longitude);

                var placemark = placemarks?.FirstOrDefault();

                map = new Xamarin.Forms.GoogleMaps.Map()
                {
                    Margin = new Thickness(2, 2, 2, 2),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MapType = MapType.Street,
                    IsEnabled = true
                };

                var pin = new Pin()
                {
                    Position = new Xamarin.Forms.GoogleMaps.Position(latitude, longitude),
                    Type = PinType.Place,
                    Label = placemark.Locality + "",
                    Address = placemark.CountryName

                };

                map.Pins.Add(pin);
                Position position = new Position();

                MapSpan mapSpan = new MapSpan(position, latitude, longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(latitude, longitude),
                Distance.FromMiles(1)));

                StackLayout stackLayout = new StackLayout()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    BackgroundColor = Color.Transparent,
                    Orientation = StackOrientation.Vertical
                };

                stackLayout.Children.Add(map);

                ContentMap.Content = stackLayout;
                ContentMap.IsVisible = true;
                layoutContainer.IsVisible = true;
                lblInfo.Text = "";
                lblInfo.IsVisible = false;
            }
            catch (Exception ex)
            {
                lblInfo.Text = ex.Message.ToString();
                ContentMap.IsVisible = false;
                lblInfo.IsVisible = true;
                layoutContainer.IsVisible = false;
            }
        }

        private void BtnNormal_Clicked(object sender, EventArgs e)
        {

        }

        private void BtnMapType_Clicked(object sender, EventArgs e)
        {
            try
            {
                String mapType = ((Button)sender).Text.ToString().ToLower();
                switch (mapType)
                {
                    case "hybrid":
                        map.MapType = MapType.Hybrid;
                        break;
                    case "satellite":
                        map.MapType = MapType.Satellite;
                        break;
                    case "street":
                        map.MapType = MapType.Street;
                        break;
                }

            }
            catch (Exception ex)
            {

            }
        }
    }
}
