﻿using Android.OS;
using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Debug = System.Diagnostics.Debug;


namespace EV_Charger_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReviewCharger : ContentPage
    {

        App app;

        string chargerName;

		public ReviewCharger (App app, string name)
		{
			InitializeComponent ();
            this.app = app;
            this.chargerName = name;

            //Updates the Charger Name field
            ChargerNames.Text = chargerName;
        }
        
        private async void OnSubmitButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Get the selected rating and comment
                int rating = (int)RatingSlider.Value;
                String comment = CommentEditor.Text;

                // Save the review to the database
                string email = GetUserFromToken();

                if(email == "")
                {
                    return;
                }

                string currentDate = DateTime.Now.ToString("MM-dd-yyyy");

                app.database.InsertRecord("Reviews", new string[5] { chargerName, email, rating.ToString(), comment, currentDate });
            }
            catch(NullReferenceException exception)
            {
                Debug.WriteLine(exception.Message);
                await DisplayAlert("Error", "Your review could not be processed. Try again later.", "OK");

                await Navigation.PushAsync(new MainPage(app));
            }

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

            Debug.WriteLine("Client Side User Token: " + token);

            string query = "SELECT * FROM Users WHERE sessionToken = '" + token + "'";

            List<Object[]> data = app.database.GetQueryRecords(query);

            email = data[0][0].ToString();

            Debug.WriteLine("Returning Email: " + email);

            return email;
        }

    }


}