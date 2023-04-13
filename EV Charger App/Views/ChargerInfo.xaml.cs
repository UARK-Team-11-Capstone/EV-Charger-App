﻿using Android.Media.Audiofx;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Debug = System.Diagnostics.Debug;
using System;
using System.Linq.Expressions;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChargerInfo : ContentPage
    {

        App app;

        string chargerName = "";

        string address = "";

        string updated = "";

        string accessibility = "";
        
        int rating;

        string starImage;
        
        StackLayout infoLayout;

        string[] bigStars = new string[6] { "zero_star_20" , "one_star_20" , "two_star_20" , "three_star_20" , "four_star_20" , "five_star_20" };
        string[] smallStars;

        public ChargerInfo(App app, string[] chargerInfo)
        {
            InitializeComponent();
            this.app = app;

            chargerName = chargerInfo[0];
            address = chargerInfo[1];
            updated = chargerInfo[2];
            accessibility = chargerInfo[3];

            rating = (int)Math.Round(app.database.GetChargerRating(chargerName));

            Title = "Charger Information";

            var nameLabel = new Label
            {
                Text = chargerName,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(5),
            };

            var avgStars = new Label
            {
                Text = app.database.GetChargerRating(chargerName) + "",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(5),
            };




            var totalStars = new Image { Source = starImage };

            var addressText = new Label
            {
                Text = " Address: ",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(5, 5, 0, 0),
                HorizontalTextAlignment = TextAlignment.Center,
            };

            var addressLabel = new Label
            {
                Text = address,
                Margin = new Thickness(5,0,0,5),
                HorizontalTextAlignment = TextAlignment.Center,
            };

            var updatedText = new Label
            {
                Text = " Last Updated: ",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(5, 5, 0, 0),
                VerticalOptions= LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,

            };

            var updatedLabel = new Label
            {
                Text = updated,
                Margin = new Thickness(5, 0, 0, 5),
                HorizontalTextAlignment = TextAlignment.Center,
            };

            var accessibilityText = new Label
            {
                Text = " Accessibility: ",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(5, 5, 0, 0),
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,

            };

            var accessibilityLabel = new Label
            {
                Text = accessibility,
                Margin = new Thickness(5, 0, 0, 5),
                HorizontalTextAlignment = TextAlignment.Center,

            };

            var reviewsText = new Label
            {
                Text = " Reviews:",
                Margin = new Thickness(5),
                FontAttributes = FontAttributes.Bold,
                //HorizontalOptions= LayoutOptions.Center,

            };

            //GM specific hex color used for the text 
            string hexColor = "#3D3935";
            Color GMBlack = Color.FromHex(hexColor);

            //Changing the color to the text to GM black  
            nameLabel.TextColor = GMBlack;
            addressLabel.TextColor = GMBlack;
            updatedLabel.TextColor = GMBlack;
            accessibilityLabel.TextColor = GMBlack;

            infoLayout = new StackLayout
            {
                Children =
                {
                    nameLabel,
                    avgStars,
                    totalStars,
                    addressText,
                    addressLabel,
                    updatedText,
                    updatedLabel,
                    accessibilityText,
                    accessibilityLabel,
                    /*new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal, // Set the orientation to Horizontal
                        Children =
                        {
                            addressText,
                            addressLabel,
                        }
                    },
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal, // Set the orientation to Horizontal
                        Children =
                        {
                            updatedText,
                            updatedLabel,
                        }
                    },
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal, // Set the orientation to Horizontal
                        Children =
                        {
                            accessibilityText,
                            accessibilityLabel,
                        }
                    },*/
                    reviewsText,
                    /*
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal, // Set the orientation to Horizontal
                        Children =
                        {
                            reviewStars,
                            emailLabel,
                        }
                    },
                    commentsLabel,
                    */
                }
            };

            var scrollView = new ScrollView
            {
                Content = infoLayout,
            };

            //GM specific hex color used for the button
            string bluehexColor = "#0072CE";
            Color GMBlue = Color.FromHex(bluehexColor);

            var buttonLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Padding = new Thickness(10),
                Children =
                {
                    new Button
                    {
                        Text = "Review Charger",
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        BackgroundColor = GMBlue,
                        CornerRadius=10,
                        Command = new Command(async () =>
                        {
                            await Navigation.PushAsync(new ReviewCharger(app, chargerName));
                        })

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

            PopulateReviews();
        }

        string setStarImage(string[] starOptions, int rating)
        {
            switch (rating)
            {
                case 5:
                    starImage = starOptions[5];
                    break;
                case 4:
                    starImage = starOptions[4];
                    break;
                case 3:
                    starImage = starOptions[3];
                    break;
                case 2:
                    starImage = starOptions[2];
                    break;
                case 1:
                    starImage = starOptions[1];
                    break;
                default:
                    starImage = starOptions[0];
                    break;
            }

            return starImage;
        }

        void PopulateReviews()
        {
            Debug.WriteLine("Populating Reviews");
            List<object[]> reviews = app.database.GetQueryRecords("SELECT * FROM Reviews WHERE chargerName = '" + chargerName + "'");

            foreach (object[] review in reviews)
            {
                string email = review[1].ToString();
                string rating = review[2].ToString();
                string comment = review[3].ToString();
                string date = review[4].ToString();

                Debug.WriteLine(email + " " + rating + comment);


                //Review Labels
                var emailLabel = new Label
                {
                    Text = email,
                    Margin = new Thickness(10, 0, 5, 0),
                };

                var reviewStars = new Image
                {
                    Source = "five_star.png",
                    Margin = new Thickness(5)
                };

                var dateLabel = new Label
                {
                    Text = date,
                    Margin = new Thickness(10, 0, 5, 0)
                };

                var commentsLabel = new Label
                {
                    Text = comment,
                    Margin = new Thickness(10, 0, 0, 0),
                };

                var reviewHeader = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal, // Set the orientation to Horizontal
                    Children =
                        {
                            reviewStars,
                            emailLabel,
                            dateLabel
                        }
                };
                
                infoLayout.Children.Add(reviewHeader);
                infoLayout.Children.Add(commentsLabel);
                
            }
        }
    }
}