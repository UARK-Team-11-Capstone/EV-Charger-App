using MySqlConnector;
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

        string chargerName = "";

        public ReviewCharger(App app, string name)
        {
            InitializeComponent();
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
                int rating = (int)Math.Round(RatingSlider.Value);
                String comment = CommentEditor.Text;

                bool a = Accessible.IsChecked;


                // Save the review to the database
                string email = GetUserFromToken();

                if (email == "")
                {
                    return;
                }

                string currentDate = DateTime.Now.ToString("MM-dd-yyyy");

                if (UserReviewed())
                {
                    //Delete old review and insert new one since it is easier
                    string query = "DELETE FROM Reviews WHERE chargerName='" + chargerName + "' AND userEmail='" + email + "';";
                    app.database.ExecuteRawNonQuery(query);
                    app.database.InsertRecord("Reviews", new string[6] { chargerName, email, rating.ToString(), comment, currentDate, a.ToString() });
                }
                else
                {
                    app.database.InsertRecord("Reviews", new string[6] { chargerName, email, rating.ToString(), comment, currentDate, a.ToString() });
                }
            }
            catch (NullReferenceException exception)
            {
                Debug.WriteLine(exception.Message);
                await DisplayAlert("Error", "Your review could not be processed. Try again later.", "OK");

                await Navigation.PushAsync(new MainPage(app));
            }

            //await App.Database.SaveReviewAsync(rating, comment);

            // Show a confirmation message
            await DisplayAlert("Thank you", "Your review has been submitted.", "OK");

            await Navigation.PushAsync(new MainPage(app));

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

        bool UserReviewed()
        {
            string email = GetUserFromToken();

            string query = "SELECT * FROM Reviews WHERE userEmail = @email and chargerName = @chargerName";

            MySqlParameter emailParam = new MySqlParameter("@email", email);
            MySqlParameter chargerParam = new MySqlParameter("@chargerName", chargerName);

            return app.database.RecordExists(query, emailParam, chargerParam);
        }

    }


}