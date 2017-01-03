using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using Bing.Maps;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MG2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Directions : Page
    {
        private readonly string BingMapsKey =
            "IXB5oJIHrwqzHTYYx7UL~KS98L2RK55rRx5jaQ4moPw~AlRzTfaWi5RuqplzTO53yfHvRJYWHajVS9Gu1XWbEGGs7uq4dRL2xN6RqdOBIiKX";

        private readonly XNamespace BingMapsNamespace =
            "http://schemas.microsoft.com/search/local/ws/rest/v1";
        
        private Geolocator locator;

        private uint count;
        
        public Directions()
        {
            this.InitializeComponent();

            this.WayPoints = new List<string>();

            this.locator = new Geolocator();

            this.locator.DesiredAccuracy = PositionAccuracy.Default;

            this.locator.PositionChanged += this.GeolocatorPositionChanged;
             
        }

        public List<string> WayPoints
        {
            get;
            set;
        }

        async private void GeolocatorPositionChanged (Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Location location =
                         new Location(e.Position.Coordinate.Latitude,
                                      e.Position.Coordinate.Longitude);
                });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //base.OnNavigatedTo(e);
        }

        
        private void MapTapped(object sender, TappedRoutedEventArgs e)
        {
            var position = e.GetPosition(this.Map);

            Location location;
         
            if(this.Map.TryPixelToLocation(position, out location))
            {
                Pushpin pin = new Pushpin();

                MapLayer.SetPosition(pin, location);

                pin.Text = (++count).ToString();

                this.Map.Children.Add(pin);

                this.Map.SetView(location);

                this.GetDirections(location);
            }

        }

        async private void GetDirections(Location location)
        {
            this.WayPoints.Add(string.Format("{0}, {1}", location.Latitude, location.Longitude));

            if (this.WayPoints.Count < 2) return;

            HttpClient client = new HttpClient();

            StringBuilder builder = new
                StringBuilder("http://dev.virtualearth.net/REST/V1/Routes/Driving?o=xml&");


            for (int index = 0; index < this.WayPoints.Count; index++)
            {
                builder.Append(
                    string.Format("wp.{0}={1}&", index, this.WayPoints[index]));
            }

            //builder.Append("avoid=minimizeTolls&key=");

            builder.Append("&key=" + this.BingMapsKey);

            HttpResponseMessage response = await client.GetAsync(builder.ToString());

            response.EnsureSuccessStatusCode();
            Stream stream = await response.Content.ReadAsStreamAsync();

            XDocument document = XDocument.Load(stream);

            var query = from p
                        in document.Descendants(this.BingMapsNamespace + "ManeuverPoint")
                        select new
                        {
                            Latitude = p.Element(this.BingMapsNamespace + "Latitude").Value,
                            Longitude = p.Element(this.BingMapsNamespace + "Longitude").Value
                        };

            MapShapeLayer layer = new MapShapeLayer();

            MapPolyline polyline = new MapPolyline();

            foreach (var point in query)
            {
                double latitude, longitude;

                double.TryParse(point.Latitude, out latitude);
                double.TryParse(point.Longitude, out longitude);

                polyline.Locations.Add(new Location(latitude, longitude));
            }

            polyline.Color = Windows.UI.Colors.Red;

            polyline.Width = 5;

            layer.Shapes.Add(polyline);

            this.Map.ShapeLayers.Add(layer);

            var distance = (from d in document.Descendants
                                (this.BingMapsNamespace + "TravelDistance")
                            select d).First().Value;
            

            //this.DistanceTextBlock.Text = string.Format("{0} miles", distance.ToString());

            // Convert to km ==> 1 mile = 1.609344 km
            double distanceKm = Convert.ToDouble(distance) * 1.609344;
            
            this.DistanceTextBlock.Text = string.Format("{0} kms", distanceKm.ToString());

            //this.DistanceTextBlock.Text = string.Format("{0} miles or {1} kms", distance.ToString(), distanceKm.ToString());

        }

        private void Home_ButtonClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GroupedItemsPage));
        }

        

        private void ClearMap(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Directions));
        }

    }
}
