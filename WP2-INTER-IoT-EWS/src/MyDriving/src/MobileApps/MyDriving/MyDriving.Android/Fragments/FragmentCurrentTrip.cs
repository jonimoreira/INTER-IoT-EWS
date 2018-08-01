// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Android.OS;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using MyDriving.Droid.Services;
using MyDriving.ViewModel;
using MvvmHelpers;
using MyDriving.DataObjects;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;
using MyDriving.Droid.Activities;
using MyDriving.Utils;
using MyDriving.Droid.Helpers;
using Android.Support.Design.Widget;
using System.Collections.Specialized;
using System.Collections.Generic;
using Android.Views.Animations;
using Android.Animation;
using ShimmerAPI;
using ShimmerLibrary;
using Newtonsoft.Json.Linq;
using Android.Content;
using Android.Util;

namespace MyDriving.Droid.Fragments
{
    public class FragmentCurrentTrip : Android.Support.V4.App.Fragment, IOnMapReadyCallback
    {
        List<LatLng> allPoints;
        Marker carMarker;
        TextView distance, distanceUnits, time, load, consumption, consumptionUnits;
        Polyline driveLine;
        Color? driveLineColor;
        FloatingActionButton fab;
        GoogleMap map;
        MapView mapView;
        bool setZoom = true;
        LinearLayout stats;

        ObservableRangeCollection<TripPoint> trailPointList;
        CurrentTripViewModel viewModel;

        public void OnMapReady(GoogleMap googleMap)
        {
            map = googleMap;
            if (viewModel != null)
                SetupMap();
        }

        public static FragmentCurrentTrip NewInstance() => new FragmentCurrentTrip { Arguments = new Bundle() };

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            HasOptionsMenu = true;
            var view = inflater.Inflate(Resource.Layout.fragment_current_trip, null);

            mapView = view.FindViewById<MapView>(Resource.Id.map);
            mapView.OnCreate(savedInstanceState);

            fab = view.FindViewById<FloatingActionButton>(Resource.Id.fab);

            fabHeart = view.FindViewById<FloatingActionButton>(Resource.Id.fabHeart);
            //sendHeartData = view.FindViewById<FloatingActionButton>(Resource.Id.sendHeartData);
            text_heart = view.FindViewById<TextView>(Resource.Id.text_heart);

            time = view.FindViewById<TextView>(Resource.Id.text_time);
            distance = view.FindViewById<TextView>(Resource.Id.text_distance);
            distanceUnits = view.FindViewById<TextView>(Resource.Id.text_distance_units);
            consumption = view.FindViewById<TextView>(Resource.Id.text_consumption);
            consumptionUnits = view.FindViewById<TextView>(Resource.Id.text_consumption_units);
            load = view.FindViewById<TextView>(Resource.Id.text_load);
            stats = view.FindViewById<LinearLayout>(Resource.Id.stats);
            stats.Visibility = ViewStates.Invisible;
            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            mapView.GetMapAsync(this);
            base.OnActivityCreated(savedInstanceState);
            // Shimmer
            SetTextHeart("Click to connect to ECG unit");
        }


        void StartFadeAnimation(bool fadeIn)
        {
            //handle first run
            if (!viewModel.IsRecording && stats.Visibility == ViewStates.Invisible)
                return;

            var start = fadeIn ? 0f : 1f;
            var end = fadeIn ? 1f : 0f;
            stats.Alpha = fadeIn ? 0f : 1f;
            stats.Visibility = ViewStates.Visible;


            var timerAnimator = ValueAnimator.OfFloat(start, end);
            timerAnimator.SetDuration(Java.Util.Concurrent.TimeUnit.Seconds.ToMillis(1));
            timerAnimator.SetInterpolator(new AccelerateInterpolator());
            timerAnimator.Update +=
                (sender, e) => { Activity.RunOnUiThread(() => stats.Alpha = (float)e.Animation.AnimatedValue); };
            timerAnimator.Start();
        }

        void OnLocationServiceConnected(object sender, ServiceConnectedEventArgs e)
        {
            viewModel = GeolocationHelper.Current.LocationService.ViewModel;
            viewModel.PropertyChanged += OnPropertyChanged;
            ResetTrip();
        }

        void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.CurrentPosition):
                    var latlng = viewModel.CurrentPosition.ToLatLng();
                    UpdateCar(latlng);
                    UpdateCamera(latlng);
                    break;
                case nameof(viewModel.CurrentTrip):
                    TripSummaryActivity.ViewModel = viewModel.TripSummary;
                    ResetTrip();
                    StartActivity(new Android.Content.Intent(Activity, typeof(TripSummaryActivity)));
                    break;
                case "Stats":
                    UpdateStats();
                    break;
                case nameof(viewModel.VehicleCollisionDetection):
                    if (viewModel.VehicleCollisionDetection.collisionDetected)
                    {
                        Activity?.RunOnUiThread(() =>
                        {
                            text_heart.Text = "Collision detected!";
                        });
                    }
                    break;
            }
        }

        void UpdateStats()
        {
            Activity?.RunOnUiThread(() =>
            {
                time.Text = viewModel.ElapsedTime;
                consumption.Text = viewModel.FuelConsumption;
                consumptionUnits.Text = viewModel.FuelConsumptionUnits;
                load.Text = viewModel.EngineLoad;
                distanceUnits.Text = viewModel.DistanceUnits;
                distance.Text = viewModel.CurrentTrip.TotalDistanceNoUnits;
            });
        }

        void ResetTrip()
        {
            trailPointList = viewModel.CurrentTrip.Points as ObservableRangeCollection<TripPoint>;
            trailPointList.CollectionChanged += OnTrailUpdated;
            carMarker = null;
            map?.Clear();
            allPoints?.Clear();
            allPoints = null;
            SetupMap();
            UpdateStats();
            StartFadeAnimation(viewModel.IsRecording);
            Activity.SupportInvalidateOptionsMenu();
        }


        void AddStartMarker(LatLng start)
        {
            Activity?.RunOnUiThread(() =>
            {
                var logicalDensity = Resources.DisplayMetrics.Density;
                var thicknessPoints = (int)Math.Ceiling(20 * logicalDensity + .5f);

                var b = ContextCompat.GetDrawable(Activity, Resource.Drawable.ic_start_point) as BitmapDrawable;
                var finalIcon = Bitmap.CreateScaledBitmap(b.Bitmap, thicknessPoints, thicknessPoints, false);

                var startMarker = new MarkerOptions();
                startMarker.SetPosition(start);
                startMarker.SetIcon(BitmapDescriptorFactory.FromBitmap(finalIcon));
                startMarker.Anchor(.5f, .5f);
                map.AddMarker(startMarker);
            });
        }

        void AddEndMarker(LatLng end)
        {
            Activity?.RunOnUiThread(() =>
            {
                var logicalDensity = Resources.DisplayMetrics.Density;
                var thicknessPoints = (int)Math.Ceiling(20 * logicalDensity + .5f);
                var b = ContextCompat.GetDrawable(Activity, Resource.Drawable.ic_end_point) as BitmapDrawable;
                var finalIcon = Bitmap.CreateScaledBitmap(b.Bitmap, thicknessPoints, thicknessPoints, false);

                var endMarker = new MarkerOptions();
                endMarker.SetPosition(end);
                endMarker.SetIcon(BitmapDescriptorFactory.FromBitmap(finalIcon));
                endMarker.Anchor(.5f, .5f);

                map.AddMarker(endMarker);
            });
        }

        void OnTrailUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            Activity?.RunOnUiThread(() =>
            {
                var item = viewModel.CurrentTrip.Points[viewModel.CurrentTrip.Points.Count - 1];
                if (carMarker != null)
                    UpdateMap(item);
                else
                    SetupMap();
            });
        }

        void UpdateCarIcon(bool recording)
        {
            Activity?.RunOnUiThread(() =>
            {
                var logicalDensity = Resources.DisplayMetrics.Density;
                var thicknessCar = (int)Math.Ceiling(26 * logicalDensity + .5f);
                var b =
                    ContextCompat.GetDrawable(Activity,
                        recording ? Resource.Drawable.ic_car_red : Resource.Drawable.ic_car_blue) as BitmapDrawable;
                //var b = ContextCompat.GetDrawable(Activity, Resource.Drawable.ic_car) as BitmapDrawable;

                var finalIcon = Bitmap.CreateScaledBitmap(b.Bitmap, thicknessCar, thicknessCar, false);

                carMarker?.SetIcon(BitmapDescriptorFactory.FromBitmap(finalIcon));

                fab.SetImageResource(recording ? Resource.Drawable.ic_stop : Resource.Drawable.ic_start);
                fabHeart.SetImageResource(Resource.Drawable.ic_heart);
            });
        }

        void SetupMap()
        {
            if (map == null)
                return;

            if (mapView.Width == 0)
            {
                mapView.PostDelayed(SetupMap, 500);
                return;
            }

            if (viewModel.CurrentTrip.HasSimulatedOBDData)
            {
                var activity = (BaseActivity)Activity;
                activity.SupportActionBar.Title = "Current Trip (Sim OBD)";
            }


            TripPoint start = null;
            if (viewModel.CurrentTrip.Points.Count != 0)
                start = viewModel.CurrentTrip.Points[0];

            UpdateMap(start, false);

            if (start != null)
            {
                UpdateCamera(carMarker.Position);
                AddStartMarker(start.ToLatLng());
            }

            viewModel.MobileDeviceId = Android.OS.Build.Serial;

        }

        void UpdateMap(TripPoint point, bool updateCamera = true)
        {
            if (map == null)
                return;
            //Get trail position or current potion to move car
            var latlng = point == null
                ? viewModel?.CurrentPosition?.ToLatLng()
                : point.ToLatLng();
            Activity?.RunOnUiThread(() =>
            {
                UpdateCar(latlng);
                driveLine?.Remove();
                var polyOptions = new PolylineOptions();

                if (allPoints == null)
                {
                    allPoints = viewModel.CurrentTrip.Points.ToLatLngs();
                }
                else if (point != null)
                {
                    allPoints.Add(point.ToLatLng());
                }

                polyOptions.Add(allPoints.ToArray());

                if (!driveLineColor.HasValue)
                    driveLineColor = new Color(ContextCompat.GetColor(Activity, Resource.Color.recording_accent));

                polyOptions.InvokeColor(driveLineColor.Value);
                driveLine = map.AddPolyline(polyOptions);
                if (updateCamera)
                    UpdateCamera(latlng);
            });
        }

        void UpdateCar(LatLng latlng)
        {
            if (latlng == null || map == null)
                return;
            Activity?.RunOnUiThread(() =>
            {
                if (carMarker == null)
                {
                    var car = new MarkerOptions();
                    car.SetPosition(latlng);
                    car.Anchor(.5f, .5f);
                    carMarker = map.AddMarker(car);
                    UpdateCarIcon(viewModel.IsRecording);
                    return;
                }
                carMarker.Position = latlng;
            });
        }

        void UpdateCamera(LatLng latlng)
        {
            if (map == null)
                return;
            Activity?.RunOnUiThread(() =>
            {
                if (setZoom)
                {
                    map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(latlng, 14));
                    setZoom = false;
                }
                else
                {
                    map.MoveCamera(CameraUpdateFactory.NewLatLng(latlng));
                }
            });
        }

        public override void OnStop()
        {
            base.OnStop();

            GeolocationHelper.Current.LocationServiceConnected -= OnLocationServiceConnected;
            if (viewModel != null)
                viewModel.PropertyChanged -= OnPropertyChanged;
            if (trailPointList != null)
                trailPointList.CollectionChanged -= OnTrailUpdated;
            if (fab != null)
                fab.Click -= OnRecordButtonClick;

            if (fabHeart != null)
                fabHeart.Click -= OnShimmerECGButtonClick;
            if (sendHeartData != null)
                sendHeartData.Click -= OnSendHeartDataButtonClick;


            //If we are recording then don't stop the background service
            if ((viewModel?.IsRecording).GetValueOrDefault())
                return;

            GeolocationHelper.Current.LocationService.StopLocationUpdates();
            GeolocationHelper.StopLocationService();
        }

        public override async void OnStart()
        {
            base.OnStart();

            GeolocationHelper.Current.LocationServiceConnected += OnLocationServiceConnected;

            if (fab != null)
                fab.Click += OnRecordButtonClick;
            if (fabHeart != null)
                fabHeart.Click += OnShimmerECGButtonClick;
            if (sendHeartData != null)
                sendHeartData.Click += OnSendHeartDataButtonClick;
            await StartLocationService();
        }


        async Task StartLocationService()
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted)
                {
                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                    status = results[Permission.Location];
                }

                if (status == PermissionStatus.Granted)
                {
                    if ((viewModel == null || !viewModel.IsRecording) && !GeolocationHelper.Current.IsRunning)
                        await GeolocationHelper.StartLocationService();
                    else
                        OnLocationServiceConnected(null, null);
                }
                else if (status != PermissionStatus.Unknown)
                {
                    Toast.MakeText(Activity, "Location permission is not granted, can't track location",
                        ToastLength.Long);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }
        }

        #region Options Menu & User Actions

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            //if((viewModel?.IsRecording).GetValueOrDefault())
            //     inflater.Inflate(Resource.Menu.menu_current_trip, menu);
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_take_photo:
                    if (!(viewModel?.IsBusy).GetValueOrDefault())
                        viewModel?.TakePhotoCommand.Execute(null);
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        async void OnRecordButtonClick(object sender, EventArgs e)
        {
            if (viewModel?.CurrentPosition == null || viewModel.IsBusy)
                return;

            if (viewModel.NeedSave)
            {
                await viewModel.SaveRecordingTripAsync();
            }

            if (viewModel.IsRecording)
            {
                if (!await viewModel.StopRecordingTrip())
                    return;

                AddEndMarker(viewModel.CurrentPosition.ToLatLng());
                UpdateCarIcon(false);

                var activity = (BaseActivity)Activity;
                activity.SupportActionBar.Title = "Current Trip";

                await viewModel.SaveRecordingTripAsync();
            }
            else
            {
                if (!await viewModel.StartRecordingTrip())
                    return;
                AddStartMarker(viewModel.CurrentPosition.ToLatLng());

                Activity.SupportInvalidateOptionsMenu();
                UpdateCarIcon(true);
                UpdateStats();
                StartFadeAnimation(true);

                if (viewModel.CurrentTrip.HasSimulatedOBDData)
                {
                    var activity = (BaseActivity)Activity;
                    activity.SupportActionBar.Title = "Current Trip (Sim OBD)";
                }
            }
        }

        #endregion

        #region MapView Lifecycle Events

        public override void OnResume()
        {
            base.OnResume();
            mapView?.OnResume();
        }

        public override void OnPause()
        {
            base.OnPause();
            mapView?.OnPause();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            mapView?.OnDestroy();
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView?.OnSaveInstanceState(outState);
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            mapView?.OnLowMemory();
        }

        #endregion


        #region Shimmer3 ECG structures

        // Shimmer 
        ShimmerLogAndStreamXamarin shimmer;
        bool buttonConnectedClicked = false;
        FloatingActionButton fabHeart;
        FloatingActionButton sendHeartData;
        TextView text_heart;

        private bool isStreaming = false;
        private double samplingRate = 256.0;


        async void OnSendHeartDataButtonClick(object sender, EventArgs e)
        {
            if (viewModel?.CurrentPosition == null || viewModel.IsBusy)
                return;
            viewModel.SendECGdataToCloud();
        }

        async void OnShimmerECGButtonClick(object sender, EventArgs e)
        {
            if (viewModel?.CurrentPosition == null || viewModel.IsBusy)
                return;

            if (!viewModel.IsStreamingHealthdevice)
                ConnectShimmer3ECG();
        }

        public void ConnectShimmer3ECG()
        {
            byte[] defaultECGReg1 = ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG1; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1
            byte[] defaultECGReg2 = ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG2; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2

            string bluetoothAddress = "00:06:66:88:db:ca".ToUpper();
            shimmer = new ShimmerLogAndStreamXamarin("ShimmerXamarin", bluetoothAddress);
            shimmer.UICallback += this.HandleEvent;
            buttonConnectedClicked = true;
            shimmer.StartConnectThread();

            //samplingRate = shimmer.GetSamplingRate();
            viewModel.SamplingRate = samplingRate;
        }


        async void HandleEvent(object sender, EventArgs args)
        {
            CustomEventArgs eventArgs = (CustomEventArgs)args;
            int indicator = eventArgs.getIndicator();

            switch (indicator)
            {
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_STATE_CHANGE:
                    isStreaming = false;
                    viewModel.IsStreamingHealthdevice = isStreaming;
                    System.Diagnostics.Debug.Write(((ShimmerBluetooth)sender).GetDeviceName() + " State = " + ((ShimmerBluetooth)sender).GetStateString() + System.Environment.NewLine);
                    int state = (int)eventArgs.getObject();
                    if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTED)
                    {
                        string status = "Connected";
                        System.Diagnostics.Debug.Write(status);

                        SetTextHeart(status);

                        buttonConnectedClicked = false;
                        shimmer.StartStreaming();

                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTING)
                    {
                        string status = "Connecting";
                        System.Diagnostics.Debug.Write(status);

                        SetTextHeart(status);
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_NONE)
                    {
                        string status = "Disconnected";
                        System.Diagnostics.Debug.Write(status);

                        SetTextHeart(status);

                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_STREAMING)
                    {
                        string status = "Streaming";
                        System.Diagnostics.Debug.Write(status);

                        SetTextHeart(status);
                        isStreaming = true;
                        viewModel.IsStreamingHealthdevice = isStreaming;
                    }
                    break;
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_DATA_PACKET:
                    ObjectCluster objectCluster = new ObjectCluster((ObjectCluster)eventArgs.getObject());


                    if (viewModel.IsRecording)
                    {
                        lock (viewModel.ioTFrequencyControl)
                        {
                            bool saved = viewModel.SaveMeasurements(objectCluster);

                            // if aggregated all ECG data in a message according to the frequencies, send to cloud
                            if (viewModel.ioTFrequencyControl.CounterSamplesInMessage >= viewModel.ioTFrequencyControl.FrequencyIoTDeviceToGatewayInHertz * viewModel.ioTFrequencyControl.PeriodGatewayToCloudInSeconds)
                            {
                                viewModel.SendECGdataToCloud();
                                viewModel.ioTFrequencyControl.CounterSamplesInMessage = 0;
                            }

                        }
                    }
                    /*
                    SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
                    SensorData dataTimestamp = objectCluster.GetData(objectCluster.GetNames().IndexOf("System Timestamp"));
                    SensorData dataECG_LL_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LL_RA, "CAL");
                    SensorData dataECG_LA_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LA_RA, "CAL");
                    SensorData dataECG_VX_RL = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_VX_RL, "CAL");

                    SensorData batteryLevel = objectCluster.GetData(Shimmer3Configuration.SignalNames.V_SENSE_BATT, "CAL");

                    double hrdataCalc = double.MinValue;
                    double hrdataCalcInter = double.MinValue;
                    
                    //SetTextHeart(dataTimestamp.Data + " (" + dataTimestamp.Unit + ")");
                    */
                    break;
            }

            if (buttonConnectedClicked)
                shimmer.StartConnectThread();


        }

        private void SetTextHeart(string status)
        {
            Android.Content.Res.ColorStateList color = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.Black });
            switch (status)
            {
                case "Connected":
                    color = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.Green });
                    break;
                case "Connecting":
                    color = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.LightSeaGreen });
                    break;
                case "Disconnected":
                    color = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.Gray });
                    break;
                case "Streaming":
                    color = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.Blue });
                    break;
                default:
                    break;
            }

            SetTextHeart(status, color);
        }

        private void SetTextHeart(string status, Android.Content.Res.ColorStateList color)
        {
            Activity?.RunOnUiThread(() =>
            {
                text_heart.Text = status;
                fabHeart.BackgroundTintList = color;
            });
        }


        #endregion

        public void OnAccelerationChanged(float x, float y, float z)
        {
            float a = x + y + z;

            if (viewModel != null && !viewModel.IsBusy)
                viewModel.OnAccelerationChanged(x, y, z);

        }
        
    }  

    
}