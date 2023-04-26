using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.GoogleMaps;

namespace EV_Charger_App.ViewModels
{
    public class RouteParser
    {
        [JsonProperty("routes")]
        public List<Route> Routes { get; set; }
    }

    public class Route
    {
        [JsonProperty("bounds")]
        public Bounds Bounds { get; set; }

        [JsonProperty("legs")]
        public List<Leg> Legs { get; set; }

        [JsonProperty("overview_polyline")]
        public Polyline OverviewPolyline { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; }

        public List<Position> GetRoutePoints()
        {
            var routePoints = new List<Position>();
            foreach (var leg in Legs)
            {
                foreach (var step in leg.Steps)
                {
                    var polyline = step.Polyline.DecodePath();
                    routePoints.AddRange(polyline.Select(location => new Position(location.Lat, location.Lng)));
                }
            }
            return routePoints;
        }
    }

    public class Bounds
    {
        [JsonProperty("northeast")]
        public Location Northeast { get; set; }

        [JsonProperty("southwest")]
        public Location Southwest { get; set; }
    }

    public class Leg
    {
        [JsonProperty("distance")]
        public Distance Distance { get; set; }

        [JsonProperty("duration")]
        public Duration Duration { get; set; }

        [JsonProperty("end_address")]
        public string EndAddress { get; set; }

        [JsonProperty("end_location")]
        public Location EndLocation { get; set; }

        [JsonProperty("start_address")]
        public string StartAddress { get; set; }

        [JsonProperty("start_location")]
        public Location StartLocation { get; set; }

        [JsonProperty("steps")]
        public List<Step> Steps { get; set; }
    }

    public class Distance
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }
    }

    public class Duration
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }
    }

    public class Step
    {
        [JsonProperty("distance")]
        public Distance Distance { get; set; }

        [JsonProperty("duration")]
        public Duration Duration { get; set; }

        [JsonProperty("end_location")]
        public Location EndLocation { get; set; }

        [JsonProperty("html_instructions")]
        public string HtmlInstructions { get; set; }

        [JsonProperty("polyline")]
        public Polyline Polyline { get; set; }

        [JsonProperty("start_location")]
        public Location StartLocation { get; set; }

        [JsonProperty("travel_mode")]
        public string TravelMode { get; set; }
    }

    public class Location
    {
        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lng")]
        public double Lng { get; set; }
    }

    public class Polyline
    {
        [JsonProperty("points")]
        public string Points { get; set; }

        public List<Location> DecodePath()
        {
            var polylineChars = Points.ToCharArray();
            var index = 0;
            var currentLat = 0;
            var currentLng = 0;
            var next5Bits = 0;
            var shift = 0;
            var result = new List<Location>();

            while (index < polylineChars.Length)
            {
                int sum = 0;
                do
                {
                    next5Bits = polylineChars[index++] - 63;
                    sum |= (next5Bits & 31) << shift;
                    shift += 5;
                } while (next5Bits >= 32);

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                sum = 0;
                shift = 0;

                do
                {
                    next5Bits = polylineChars[index++] - 63;
                    sum |= (next5Bits & 31) << shift;
                    shift += 5;
                } while (next5Bits >= 32);

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                result.Add(new Location { Lat = Convert.ToDouble(currentLat) / 1E5, Lng = Convert.ToDouble(currentLng) / 1E5 });
            }

            return result;
        }

    }
}