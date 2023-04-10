using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChargerInfo : ContentPage
    {

        App app;

        public ChargerInfo(App app)
        {
            InitializeComponent();
            this.app = app;

            Title = "Charger Information";

            /*var mapView = new MapView();
            mapView.MapType = MapType.Standard;*/

            var nameLabel = new Label
            {
                Text = "Name of Charger",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(10),
            };

            var addressLabel = new Label
            {
                Text = "Address of Charger",
                Margin = new Thickness(10),
            };

            var updatedLabel = new Label
            {
                Text = "Last Updated",
                Margin = new Thickness(10),
            };

            var accessibilityLabel = new Label
            {
                Text = "Accessibility information",
                Margin = new Thickness(10),
            };

            var websiteLabel = new Label
            {
                Text = "https://www.gm.com",
                Margin = new Thickness(10),
                TextColor = Color.Blue,
            };
            websiteLabel.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => Device.OpenUri(new Uri("https://www.gm.com"))),
            });

            var infoLayout = new StackLayout
            {
                Children =
                {
                    nameLabel,
                    addressLabel,
                    updatedLabel,
                    accessibilityLabel,
                    websiteLabel,
                }
            };

            var scrollView = new ScrollView
            {
                Content = infoLayout,
            };

            var buttonLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Padding = new Thickness(10),
                Children =
                {
                    new Button
                    {
                        Text = "Directions",
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    },
                    new Button
                    {
                        Text = "Save",
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    },
                }
            };

            var mainLayout = new StackLayout
            {
                Children =
                {
                    scrollView,
                    buttonLayout,
                }
            };

            Content = mainLayout;
        }
    }
}