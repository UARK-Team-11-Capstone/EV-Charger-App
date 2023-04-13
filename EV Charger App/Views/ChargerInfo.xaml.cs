using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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

        public ChargerInfo(App app, string[] chargerInfo)
        {
            InitializeComponent();
            this.app = app;

            chargerName = chargerInfo[0];
            address = chargerInfo[1];
            updated = chargerInfo[2];
            accessibility = chargerInfo[3];

            Title = "Charger Information";

            var nameLabel = new Label
            {
                Text = chargerName,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(5),
            };

            var totalStars = new Image 
            { 
                Source = "newfive_star.png",
            };

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

            //Review Labels
            var emailLabel = new Label
            {
                Text = "email@uark.edu",
                Margin = new Thickness(10, 0, 5, 0),
            };

            var reviewStars = new Image
            {
                Source = "newfive_star.png",
            };

            var commentsLabel = new Label
            {
                Text = "LOTS OF RANDOM WORDS",
                Margin = new Thickness(10,0,0,0),
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
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal, // Set the orientation to Horizontal
                        Children =
                        {
                            emailLabel,
                            reviewStars,
                        }
                    },
                    commentsLabel,                    
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
        }
    }
}