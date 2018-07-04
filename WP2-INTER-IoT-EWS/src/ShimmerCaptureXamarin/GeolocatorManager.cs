using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using Plugin.Geolocator.Abstractions;
using Plugin.Geolocator;

namespace ShimmerCaptureXamarin
{
    public class GeolocatorManager
    {
        public IGeolocator Geolocator => CrossGeolocator.Current;

        public GeolocatorManager()
        { }

        public async Task ExecuteStartTrackingAsync()
        {

            try
            {
                if (Geolocator.IsListening)
                {
                    await Geolocator.StopListeningAsync();
                }

                if (Geolocator.IsGeolocationAvailable)
                {
                    //Geolocator.AllowsBackgroundUpdates = true;
                    Geolocator.DesiredAccuracy = 25;

                    Geolocator.PositionChanged += Geolocator_PositionChanged;
                    //every 3 second, 5 meters
                    await Geolocator.StartListeningAsync(3000, 5);
                    CurrentPosition = await Geolocator.GetPositionAsync();
                }

            }
            catch (Exception ex)
            {
                throw ex;
                //Console.WriteLine(ex);
            }
        }

        async void Geolocator_PositionChanged(object sender, PositionEventArgs e)
        {
            // Only update the route if we are meant to be recording coordinates
            CurrentPosition = e.Position;
        }

        Position position;

        public Position CurrentPosition
        {
            get
            {
                if (position == null)
                    this.ExecuteStartTrackingAsync();
                return position;
            }
            set { position = value; }
        }
    }
}