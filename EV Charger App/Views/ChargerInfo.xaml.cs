﻿using System;
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

            //GM specific hex color used for the text 
            string hexColor = "#3D3935";
            Color GMBlack = Color.FromHex(hexColor);

            //Changing the color to the text to GM black  
            nameLabel.TextColor = GMBlack;
            addressLabel.TextColor = GMBlack;
            updatedLabel.TextColor = GMBlack;
            accessibilityLabel.TextColor = GMBlack;

            var infoLayout = new StackLayout
            {
                Children =
                {
                    nameLabel,
                    addressLabel,
                    updatedLabel,
                    accessibilityLabel,
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