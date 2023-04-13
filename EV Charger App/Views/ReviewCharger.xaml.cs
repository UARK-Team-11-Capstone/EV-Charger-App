using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReviewCharger : ContentPage
    {

        App app;

        string chargerID;

        public ReviewCharger(App app, string id)
        {
            InitializeComponent();
            this.app = app;
            this.chargerID = id;
        }

        private async void OnSubmitButtonClicked(object sender, EventArgs e)
        {
            // Get the selected rating and comment
            int rating = (int)RatingSlider.Value;
            String comment = CommentEditor.Text;

            // Save the review to the database
            string email = GetUserFromToken();

            //I need some way of getting the charger ID
            string chargerID = "";

            app.database.InsertRecord("Reviews", new string[4] { chargerID, email, rating.ToString(), comment });

            //await App.Database.SaveReviewAsync(rating, comment);

            // Show a confirmation message
            await DisplayAlert("Thank you", "Your review has been submitted.", "OK");

            // Reset the form
            RatingSlider.Value = 1;
            CommentEditor.Text = "";

        }

        string GetUserFromToken()
        {
            string email = "";

            string token = app.session.getToken();

            string query = "SELECT * FROM Users WHERE sessionToken = '" + token + "'";

            List<Object[]> data = app.database.GetQueryRecords(query);

            email = data[0][0].ToString();

            return email;
        }

    }


}