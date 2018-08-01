// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MyDriving.Helpers;
using MyDriving.DataObjects;
using MyDriving.Utils;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using MvvmHelpers;
using Plugin.Media.Abstractions;
using Plugin.Media;
using Plugin.DeviceInfo;
using MyDriving.Services;
using MyDriving.Utils.Helpers;
using MyDriving.AzureClient;
using MyDriving.Utils.Interfaces;

using ShimmerAPI;
using ShimmerLibrary;
using Newtonsoft.Json.Linq;
using MyDriving.EWS.SAREF4health;
using MyDriving.EWS.Logistics.LogiServ;

namespace MyDriving.ViewModel
{
    public class CurrentTripViewModel : ViewModelBase
    {
        readonly OBDDataProcessor obdDataProcessor;
        readonly List<Photo> photos;
        string distance = "0.0";

        string distanceUnits = "miles";

        string elapsedTime = "0s";

        string engineLoad = "N/A";

        string fuelConsumption = "N/A";

        double fuelConsumptionRate;

        string fuelConsumptionUnits = "Gallons";

        bool isRecording;

        Position position;

        ICommand startTrackingTripCommand;

        ICommand stopTrackingTripCommand;

        ICommand takePhotoCommand;

        public CurrentTripViewModel()
        {
            CurrentTrip = new Trip
            {
                UserId = Settings.Current.UserUID,
                Points = new ObservableRangeCollection<TripPoint>()
            };
            photos = new List<Photo>();
            fuelConsumptionRate = 0;
            FuelConsumptionUnits = Settings.MetricUnits ? "Liters" : "Gallons";
            DistanceUnits = Settings.MetricDistance ? "Kilometers" : "Miles";
            ElapsedTime = "0s";
            Distance = "0.0";
            FuelConsumption = "N/A";
            EngineLoad = "N/A";
            obdDataProcessor = OBDDataProcessor.GetProcessor();

            IsStreamingHealthdevice = false;
        }

        public Trip CurrentTrip { get; private set; }

        public bool IsRecording
        {
            get { return isRecording; }
            private set { SetProperty(ref isRecording, value); }
        }

        public Position CurrentPosition
        {
            get { return position; }
            set { SetProperty(ref position, value); }
        }

        public string ElapsedTime
        {
            get { return elapsedTime; }
            set { SetProperty(ref elapsedTime, value); }
        }

        public string Distance
        {
            get { return distance; }
            set { SetProperty(ref distance, value); }
        }

        public string DistanceUnits
        {
            get { return distanceUnits; }
            set { SetProperty(ref distanceUnits, value); }
        }

        public string FuelConsumption
        {
            get { return fuelConsumption; }
            set { SetProperty(ref fuelConsumption, value); }
        }

        public string FuelConsumptionUnits
        {
            get { return fuelConsumptionUnits; }
            set { SetProperty(ref fuelConsumptionUnits, value); }
        }

        public string EngineLoad
        {
            get { return engineLoad; }
            set { SetProperty(ref engineLoad, value); }
        }

        public bool NeedSave { get; set; }
        public IGeolocator Geolocator => CrossGeolocator.Current;
        public IMedia Media => CrossMedia.Current;

        public TripSummaryViewModel TripSummary { get; set; }

        public ICommand StartTrackingTripCommand =>
            startTrackingTripCommand ??
            (startTrackingTripCommand = new RelayCommand(async () => await ExecuteStartTrackingTripCommandAsync()));

        public ICommand StopTrackingTripCommand =>
            stopTrackingTripCommand ??
            (stopTrackingTripCommand = new RelayCommand(async () => await ExecuteStopTrackingTripCommandAsync()));

        public ICommand TakePhotoCommand =>
            takePhotoCommand ??
            (takePhotoCommand = new RelayCommand(async () => await ExecuteTakePhotoCommandAsync()));

        public async Task<bool> StartRecordingTrip()
        {
            if (IsBusy || IsRecording)
                return false;

            //Since the current trip screen is typically the first screen opened, let's do an up-front check to ensure the user is authenticated
            await AzureClient.AzureClient.CheckIsAuthTokenValid();

            try
            {
                if (CurrentPosition == null)
                {
                    if (CrossDeviceInfo.Current.Platform == Plugin.DeviceInfo.Abstractions.Platform.Android ||
                        CrossDeviceInfo.Current.Platform == Plugin.DeviceInfo.Abstractions.Platform.iOS ||
                        CrossDeviceInfo.Current.Platform == Plugin.DeviceInfo.Abstractions.Platform.WindowsPhone)
                    {
                        Acr.UserDialogs.UserDialogs.Instance.Toast(
                            new Acr.UserDialogs.ToastConfig(Acr.UserDialogs.ToastEvent.Success,
                                "Waiting for current location.")
                            {
                                Duration = TimeSpan.FromSeconds(3),
                                TextColor = System.Drawing.Color.White,
                                BackgroundColor = System.Drawing.Color.FromArgb(96, 125, 139)
                            });
                    }

                    return false;
                }

                //Connect to the OBD device
                if (obdDataProcessor != null)
                {
                    await obdDataProcessor.ConnectToObdDevice(true);

                    CurrentTrip.HasSimulatedOBDData = obdDataProcessor.IsObdDeviceSimulated;
                }

                CurrentTrip.RecordedTimeStamp = DateTime.UtcNow;

                IsRecording = true;
                Logger.Instance.Track("StartRecording");
                //add start point
                CurrentTrip.Points.Add(new TripPoint
                {
                    RecordedTimeStamp = DateTime.UtcNow,
                    Latitude = CurrentPosition.Latitude,
                    Longitude = CurrentPosition.Longitude,
                    Sequence = CurrentTrip.Points.Count,
                });
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }

            // ECG to HR (heart rate) conversion
            if (IsStreamingHealthdevice && ECGtoHRAdaptive == null)
                ECGtoHRAdaptive = new ECGToHRAdaptive(SamplingRate);

            return IsRecording;
        }

        public async Task<bool> SaveRecordingTripAsync(string name = "")
        {
            if (IsRecording)
                return false;

            if (CurrentTrip.Points?.Count < 1)
            {
                Logger.Instance.Track("Attempt to save a trip with no points!");
                return false;
            }
            IsBusy = true;
            var tripId = CurrentTrip.Id;

            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    CurrentTrip.Name = DateTime.Now.ToString("d") + " " + DateTime.Now.ToString("t");
                    var result = await Acr.UserDialogs.UserDialogs.Instance.PromptAsync(new Acr.UserDialogs.PromptConfig
                    {
                        Text = CurrentTrip.Name,
                        OkText = "OK",
                        IsCancellable = false,
                        Title = "Name of Trip",
                        Message = String.Empty,
                        Placeholder = String.Empty
                    });
                    CurrentTrip.Name = result?.Text ?? string.Empty;
                }
                else
                {
                    CurrentTrip.Name = name;
                }

                ProgressDialogManager.LoadProgressDialog("Saving trip...");

                if (Logger.BingMapsAPIKey != "____BingMapsAPIKey____")
                {

                    CurrentTrip.MainPhotoUrl =
                        $"http://dev.virtualearth.net/REST/V1/Imagery/Map/Road/{CurrentPosition.Latitude.ToString(CultureInfo.InvariantCulture)},{CurrentPosition.Longitude.ToString(CultureInfo.InvariantCulture)}/15?mapSize=500,220&key={Logger.BingMapsAPIKey}";
                }
                else
                {
                    CurrentTrip.MainPhotoUrl = string.Empty;
                }
                CurrentTrip.Rating = 90;

                await StoreManager.TripStore.InsertAsync(CurrentTrip);

                foreach (var photo in photos)
                {
                    photo.TripId = CurrentTrip.Id;
                    await StoreManager.PhotoStore.InsertAsync(photo);
                }

                CurrentTrip = new Trip { Points = new ObservableRangeCollection<TripPoint>() };

                ElapsedTime = "0s";
                Distance = "0.0";
                FuelConsumption = "N/A";
                EngineLoad = "N/A";

                OnPropertyChanged(nameof(CurrentTrip));
                OnPropertyChanged("Stats");
                NeedSave = false;
                Logger.Instance.Track("SaveRecording");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }
            finally
            {
                
                IsBusy = false;

                ProgressDialogManager.DisposeProgressDialog();
            }
            Logger.Instance.Track("SaveRecording failed for trip " + tripId);
            return false;
        }

        public async Task<bool> StopRecordingTrip()
        {
            if (IsBusy || !IsRecording)
                return false;


            //always will have 1 point, so we can stop.
            CurrentTrip.EndTimeStamp = DateTime.UtcNow;
            try
            {
                if (obdDataProcessor != null)
                {
                    //Disconnect from the OBD device; if still trying to connect, stop polling for the device
                    await obdDataProcessor.DisconnectFromObdDevice();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }

            List<POI> poiList = new List<POI>();
            try
            {
                poiList = new List<POI>(await StoreManager.POIStore.GetItemsAsync(CurrentTrip.Id));
            }
            catch (Exception ex)
            {
                //Intermittently, Sqlite will cause a crash for WinPhone saying "unable to set temporary directory"
                //If this call fails, simply use an empty list of POIs
                Logger.Instance.Track("Unable to get POI Store Items.");
                Logger.Instance.Report(ex);
            }
           
            CurrentTrip.HardStops = poiList.Where(p => p.POIType == POIType.HardBrake).Count();
            CurrentTrip.HardAccelerations = poiList.Where(p => p.POIType == POIType.HardAcceleration).Count();

            TripSummary = new TripSummaryViewModel
            {
                TotalTime = (CurrentTrip.EndTimeStamp - CurrentTrip.RecordedTimeStamp).TotalSeconds,
                TotalDistance = CurrentTrip.Distance,
                FuelUsed = CurrentTrip.FuelUsed,
                MaxSpeed = CurrentTrip.Points.Max(s => s.Speed),
                HardStops = CurrentTrip.HardStops,
                HardAccelerations = CurrentTrip.HardAccelerations,
                Date = DateTime.UtcNow
            };

            IsRecording = false;
            NeedSave = true;
            Logger.Instance.Track("StopRecording");
            return true;
        }

        public async Task ExecuteStartTrackingTripCommandAsync()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                if (Geolocator.IsListening)
                {
                    await Geolocator.StopListeningAsync();
                }

				if (Geolocator.IsGeolocationAvailable && (CrossDeviceInfo.Current.Platform == Plugin.DeviceInfo.Abstractions.Platform.iOS || Geolocator.IsGeolocationEnabled))
                {
                    Geolocator.AllowsBackgroundUpdates = true;
                    Geolocator.DesiredAccuracy = 25;

                    Geolocator.PositionChanged += Geolocator_PositionChanged;
                    //every 3 second, 5 meters
                    await Geolocator.StartListeningAsync(3000, 5);
                    //await Geolocator.StartListeningAsync(10000, 20);
                    //await Geolocator.StartListeningAsync(10000, 5);
                }
                else
                {
                    Acr.UserDialogs.UserDialogs.Instance.Alert(
                        "Please ensure that geolocation is enabled and permissions are allowed for MyDriving to start a recording.",
                        "Geolocation Disabled", "OK");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }
        }

        public async Task ExecuteStopTrackingTripCommandAsync()
        {
            if (IsBusy || !IsRecording)
                return;

            try
            {
                //Unsubscribe because we were recording and it is alright
                Geolocator.PositionChanged -= Geolocator_PositionChanged;
                await Geolocator.StopListeningAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }
        }

        async Task AddOBDDataToPoint(TripPoint point)
        {
            //Read data from the OBD device
            point.HasOBDData = false;
            Dictionary<string, string> obdData = null;

            if (obdDataProcessor != null)
                obdData = await obdDataProcessor.ReadOBDData();

            if (obdData != null)
            {
                double speed = -255,
                    rpm = -255,
                    efr = -255,
                    el = -255,
                    stfb = -255,
                    ltfb = -255,
                    fr = -255,
                    tp = -255,
                    rt = -255,
                    dis = -255,
                    rtp = -255;
                var vin = String.Empty;

                if (obdData.ContainsKey("el") && !string.IsNullOrWhiteSpace(obdData["el"]))
                {
                    if (!double.TryParse(obdData["el"], out el))
                        el = -255;
                }
                if (obdData.ContainsKey("stfb"))
                    double.TryParse(obdData["stfb"], out stfb);
                if (obdData.ContainsKey("ltfb"))
                    double.TryParse(obdData["ltfb"], out ltfb);
                if (obdData.ContainsKey("fr"))
                {
                    double.TryParse(obdData["fr"], out fr);
                    if (fr != -255)
                    {
                        fuelConsumptionRate = fr;
                    }
                }
                if (obdData.ContainsKey("tp"))
                    double.TryParse(obdData["tp"], out tp);
                if (obdData.ContainsKey("rt"))
                    double.TryParse(obdData["rt"], out rt);
                if (obdData.ContainsKey("dis"))
                    double.TryParse(obdData["dis"], out dis);
                if (obdData.ContainsKey("rtp"))
                    double.TryParse(obdData["rtp"], out rtp);
                if (obdData.ContainsKey("spd"))
                    double.TryParse(obdData["spd"], out speed);
                if (obdData.ContainsKey("rpm"))
                    double.TryParse(obdData["rpm"], out rpm);
                if (obdData.ContainsKey("efr") && !string.IsNullOrWhiteSpace(obdData["efr"]))
                {
                    if (!double.TryParse(obdData["efr"], out efr))
                        efr = -255;
                }
                else
                {
                    efr = -255;
                }
                if (obdData.ContainsKey("vin"))
                    vin = obdData["vin"];

                point.EngineLoad = el;
                point.ShortTermFuelBank = stfb;
                point.LongTermFuelBank = ltfb;
                point.MassFlowRate = fr;
                point.ThrottlePosition = tp;
                point.Runtime = rt;
                point.DistanceWithMalfunctionLight = dis;
                point.RelativeThrottlePosition = rtp;
                point.Speed = speed;
                point.RPM = rpm;
                point.EngineFuelRate = efr;
                point.VIN = vin;

                #if DEBUG
                foreach (var kvp in obdData)
                    Logger.Instance.Track($"{kvp.Key} {kvp.Value}");
                #endif

                point.HasOBDData = true;
            }
        }


        async void Geolocator_PositionChanged(object sender, PositionEventArgs e)
        {
            // Only update the route if we are meant to be recording coordinates
            if (IsRecording)
            {
                var userLocation = e.Position;

                TripPoint previous = null;
                double newDistance = 0;
                if (CurrentTrip.Points.Count > 1)
                {
                    previous = CurrentTrip.Points[CurrentTrip.Points.Count - 1];
                    newDistance = DistanceUtils.CalculateDistance(userLocation.Latitude,
                        userLocation.Longitude, previous.Latitude, previous.Longitude);

                    if (newDistance > 4) // if more than 4 miles then gps is off don't use
                        return;
                }

                var point = new TripPoint
                    {
                        TripId = CurrentTrip.Id,
                        RecordedTimeStamp = DateTime.UtcNow,
                        Latitude = userLocation.Latitude,
                        Longitude = userLocation.Longitude,
                        Sequence = CurrentTrip.Points.Count,
                        Speed = -255,
                        RPM = -255,
                        ShortTermFuelBank = -255,
                        LongTermFuelBank = -255,
                        ThrottlePosition = -255,
                        RelativeThrottlePosition = -255,
                        Runtime = -255,
                        DistanceWithMalfunctionLight = -255,
                        EngineLoad = -255,
                        MassFlowRate = -255,
                        EngineFuelRate = -255,
                        VIN = "-255"
                    };

                double lat = point.Latitude;
                double lon = point.Longitude;

                lock (lastPoint)
                    lastPoint = point;

                //Add OBD data
                if (obdDataProcessor != null)
                    point.HasSimulatedOBDData = obdDataProcessor.IsObdDeviceSimulated;
                await AddOBDDataToPoint(point);

                CurrentTrip.Points.Add(point);

                // Send to IoT Hub: Original message
                try
                {
                    if (obdDataProcessor != null)
                    {
                        //Push the trip data packaged with the OBD data to the IOT Hub
                        obdDataProcessor.SendTripPointToIOTHub(CurrentTrip.Id, CurrentTrip.UserId, point);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Report(ex);
                }


                // Send to IoT Hub: SAREF4health if not streaming from ECG device (without "consistsOf")
                if (!IsStreamingHealthdevice)
                    SendECGdataToCloud();


                // Send to IoT Hub: Logistics
                try
                {
                    if (obdDataProcessor != null)
                    {
                        JObject logisticsData = FormatMessageLogistics(CurrentTrip, point);
                        obdDataProcessor.SendLogisticsDataToIOTHub(logisticsData);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Report(ex);
                }

                if (CurrentTrip.Points.Count > 1 && previous != null)
                {
                    CurrentTrip.Distance += newDistance;
                    Distance = CurrentTrip.TotalDistanceNoUnits;

                    //calculate gas usage
                    var timeDif1 = point.RecordedTimeStamp - previous.RecordedTimeStamp;
                    CurrentTrip.FuelUsed += fuelConsumptionRate * 0.00002236413 * timeDif1.TotalSeconds;
                    if (CurrentTrip.FuelUsed == 0)
                        FuelConsumption = "N/A";
                    else
                        FuelConsumption = Settings.MetricUnits
                            ? (CurrentTrip.FuelUsed * 3.7854).ToString("N2")
                            : CurrentTrip.FuelUsed.ToString("N2");
                }
                else
                {
                    CurrentTrip.FuelUsed = 0;
                    FuelConsumption = "N/A";
                }

                var timeDif = point.RecordedTimeStamp - CurrentTrip.RecordedTimeStamp;

                //track seconds, minutes, then hours
                if (timeDif.TotalMinutes < 1)
                    ElapsedTime = $"{timeDif.Seconds}s";
                else if (timeDif.TotalHours < 1)
                    ElapsedTime = $"{timeDif.Minutes}m {timeDif.Seconds}s";
                else
                    ElapsedTime = $"{(int)timeDif.TotalHours}h {timeDif.Minutes}m {timeDif.Seconds}s";

                if (point.EngineLoad != -255)
                    EngineLoad = $"{(int)point.EngineLoad}%";

                FuelConsumptionUnits = Settings.MetricUnits ? "Liters" : "Gallons";
                DistanceUnits = Settings.MetricDistance ? "Kilometers" : "Miles";

                OnPropertyChanged("Stats");
            }

            CurrentPosition = e.Position;
        }

        async Task ExecuteTakePhotoCommandAsync()
        {
            try
            {
                await Media.Initialize();

                if (!Media.IsCameraAvailable || !Media.IsTakePhotoSupported)
                {
                    Acr.UserDialogs.UserDialogs.Instance.Alert(
                        "Please ensure that camera is enabled and permissions are allowed for MyDriving to take photos.",
                        "Camera Disabled", "OK");

                    return;
                }

                var locationTask = Geolocator.GetPositionAsync(2500);
                var photo = await Media.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    DefaultCamera = CameraDevice.Rear,
                    Directory = "MyDriving",
                    Name = "MyDriving_",
                    SaveToAlbum = true,
                    PhotoSize = PhotoSize.Small
                });

                if (photo == null)
                {
                    return;
                }

                if (CrossDeviceInfo.Current.Platform == Plugin.DeviceInfo.Abstractions.Platform.Android ||
                    CrossDeviceInfo.Current.Platform == Plugin.DeviceInfo.Abstractions.Platform.iOS)
                {
                    Acr.UserDialogs.UserDialogs.Instance.Toast(
                        new Acr.UserDialogs.ToastConfig(Acr.UserDialogs.ToastEvent.Success, "Photo taken!")
                        {
                            Duration = TimeSpan.FromSeconds(3),
                            TextColor = System.Drawing.Color.White,
                            BackgroundColor = System.Drawing.Color.FromArgb(96, 125, 139)
                        });
                }

                var local = await locationTask;
                var photoDb = new Photo
                {
                    PhotoUrl = photo.Path,
                    Latitude = local.Latitude,
                    Longitude = local.Longitude,
                    TimeStamp = DateTime.UtcNow
                };

                photos.Add(photoDb);
                photo.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }
        }

        #region Shimmer3: SAREF4health

        private JsonLD jsonLdSaref4health = new JsonLD();
        private VehicleCollisionDetection vehicleCollisionDetection = new VehicleCollisionDetection(3*Gforce);
        public string MobileDeviceId { get; set; }
        public bool IsStreamingHealthdevice { get; set; }
        
        private List<Measurement> batteryLevelList;
        private List<Measurement> GetBatteryLevelList()
        {
            if (batteryLevelList == null)
                batteryLevelList = new List<Measurement>();
            return batteryLevelList;
        }

        private List<Measurement> accelerationCrossAxialList;
        private List<Measurement> GetAccelerationCrossAxialList()
        {
            if (accelerationCrossAxialList == null)
            {
                accelerationCrossAxialList = new List<Measurement>();
            }

            return accelerationCrossAxialList;
        }

        private List<Measurement> accelerationX_ECGdevice_List;
        private List<Measurement> GetAccelerationX_ECGdevice_List()
        {
            if (accelerationX_ECGdevice_List == null)
                accelerationX_ECGdevice_List = new List<Measurement>();
            return accelerationX_ECGdevice_List;
        }

        private List<Measurement> accelerationY_ECGdevice_List;
        private List<Measurement> GetAccelerationY_ECGdevice_List()
        {
            if (accelerationY_ECGdevice_List == null)
                accelerationY_ECGdevice_List = new List<Measurement>();
            return accelerationY_ECGdevice_List;
        }

        private List<Measurement> accelerationZ_ECGdevice_List;
        private List<Measurement> GetAccelerationZ_ECGdevice_List()
        {
            if (accelerationZ_ECGdevice_List == null)
                accelerationZ_ECGdevice_List = new List<Measurement>();
            return accelerationZ_ECGdevice_List;
        }
        
        private List<KeyValuePair<double, double>> valuesLead1_ECG_LA_RA;
        private List<KeyValuePair<double, double>> GetValuesLead1_ECG_LA_RA()
        {
            if (valuesLead1_ECG_LA_RA == null)
                valuesLead1_ECG_LA_RA = new List<KeyValuePair<double, double>>();
            return valuesLead1_ECG_LA_RA;
        }

        private List<KeyValuePair<double, double>> valuesLead2_ECG_LL_RA;
        private List<KeyValuePair<double, double>> GetValuesLead2_ECG_LL_RA()
        {
            if (valuesLead2_ECG_LL_RA == null)
                valuesLead2_ECG_LL_RA = new List<KeyValuePair<double, double>>();
            return valuesLead2_ECG_LL_RA;
        }

        private List<KeyValuePair<double, double>> valuesLead3_ECG_LL_LA;
        private List<KeyValuePair<double, double>> GetValuesLead3_ECG_LL_LA()
        {
            if (valuesLead3_ECG_LL_LA == null)
                valuesLead3_ECG_LL_LA = new List<KeyValuePair<double, double>>();
            return valuesLead3_ECG_LL_LA;
        }

        private List<KeyValuePair<double, double>> valuesUnipolarLeadVx_RL;
        private List<KeyValuePair<double, double>> GetValuesUnipolarLeadVx_RL()
        {
            if (valuesUnipolarLeadVx_RL == null)
                valuesUnipolarLeadVx_RL = new List<KeyValuePair<double, double>>();
            return valuesUnipolarLeadVx_RL;
        }

        private const string ECG_LL_LA = "LL_LA";


        private List<Measurement> heartRate_ECGdevice_List;
        private List<Measurement> GetHeartRate_ECGdevice_List()
        {
            if (heartRate_ECGdevice_List == null)
                heartRate_ECGdevice_List = new List<Measurement>();
            return heartRate_ECGdevice_List;
        }

        private ECGToHRAdaptive ECGtoHRAdaptive;
        public double SamplingRate { get; set; }

        public VehicleCollisionDetection VehicleCollisionDetection
        {
            get
            {
                return vehicleCollisionDetection;
            }

            set
            {
                vehicleCollisionDetection = value;
            }
        }

        public IoTFrequencyControl ioTFrequencyControl = new IoTFrequencyControl();

        //public async Task<bool> SaveMeasurements(ObjectCluster objectCluster)
        public bool SaveMeasurements(ObjectCluster objectCluster)
        {
            ioTFrequencyControl.CounterSamplesInMessage++;

            List<Double> data = objectCluster.GetData();
            List<String> dataNames = objectCluster.GetNames();
            List<String> dataUnits = objectCluster.GetUnits();

            SensorData dataTimestamp = objectCluster.GetData(dataNames.IndexOf("System Timestamp"));

            SensorData dataECG_LL_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LL_RA, "CAL");
            SensorData dataECG_LA_RA = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_LA_RA, "CAL");
            SensorData dataECG_VX_RL = objectCluster.GetData(Shimmer3Configuration.SignalNames.ECG_VX_RL, "CAL");

            CompactECGData(dataTimestamp, dataECG_LL_RA, dataECG_LA_RA, dataECG_VX_RL);

            SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
            SensorData dataAccelY = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, "CAL");
            SensorData dataAccelZ = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, "CAL");

            accelerationX_ECGdevice_List = GetAccelerationX_ECGdevice_List();
            lock (accelerationX_ECGdevice_List)
            {
                Measurement accelerationX_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, dataAccelX, dataTimestamp.Data);
                accelerationX_ECGdevice_List.Add(accelerationX_ECGdeviceMeasurement);
            }

            accelerationY_ECGdevice_List = GetAccelerationY_ECGdevice_List();
            lock (accelerationY_ECGdevice_List)
            {
                Measurement accelerationY_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, dataAccelY, dataTimestamp.Data);
                accelerationY_ECGdevice_List.Add(accelerationY_ECGdeviceMeasurement);
            }

            accelerationZ_ECGdevice_List = GetAccelerationZ_ECGdevice_List();
            lock (accelerationZ_ECGdevice_List)
            {
                Measurement accelerationZ_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, dataAccelZ, dataTimestamp.Data);
                accelerationZ_ECGdevice_List.Add(accelerationZ_ECGdeviceMeasurement);
            }
            
            // Compute cross-axial energy from ECG Device accelermoter
            double crossAxialEnergy = ComputeCrossAxialFunction(dataAccelX, dataAccelY, dataAccelZ);

            SensorData crossAxialEnergyObj = new SensorData("cross-axial-energy", crossAxialEnergy);
            Measurement accelerationCrossAxial = jsonLdSaref4health.TranslateMeasurement("accelerationCrossAxial", crossAxialEnergyObj, dataTimestamp.Data, "Measurement of average cross-axial function (x^2 + y^2 + x^2)");
            accelerationCrossAxialList = GetAccelerationCrossAxialList();
            lock (accelerationCrossAxialList) //lock(vehicleCollisionDetection)
            {
                accelerationCrossAxialList.Add(accelerationCrossAxial);

                if (!VehicleCollisionDetection.collisionDetected && ioTFrequencyControl.CounterSamplesInMessage > 0)
                {
                    VehicleCollisionDetection.collisionDetected = DetectCollision(crossAxialEnergy);

                    if (VehicleCollisionDetection.collisionDetected)
                    {
                        VehicleCollisionDetection.timestampCollisionDetected = dataTimestamp.Data;
                        OnPropertyChanged(nameof(VehicleCollisionDetection));
                        //Console.WriteLine("Collision detected!!!");
                    }
                }
            }

            // Battery level (in mVolts) of ECG device
            SensorData batteryLevel = objectCluster.GetData(Shimmer3Configuration.SignalNames.V_SENSE_BATT, "CAL");
            batteryLevelList = GetBatteryLevelList();
            lock (batteryLevelList)
            {
                Measurement batteryLevelMeasurement = jsonLdSaref4health.TranslateMeasurement("batteryLevel", batteryLevel, dataTimestamp.Data, "Measurement of battery level");
                batteryLevelList.Add(batteryLevelMeasurement);
            }

            heartRate_ECGdevice_List = GetHeartRate_ECGdevice_List();
            lock (heartRate_ECGdevice_List)
            {
                
                SensorData sensorDataHR;
                try
                {
                    if (ECGtoHRAdaptive == null) ECGtoHRAdaptive = new ECGToHRAdaptive(SamplingRate);
                    ECGToHRAdaptive.DataECGToHROutput dataHeartRate = ECGtoHRAdaptive.ecgToHrConversion(dataECG_LL_RA.Data, dataTimestamp.Data);
                    sensorDataHR = new SensorData("beats/min", dataHeartRate.getHeartRate());
                }
                catch (Exception ex)
                {
                    sensorDataHR = new SensorData("beats/min", double.MinValue);
                }
                Measurement heartRate_ECGdeviceMeasurement = jsonLdSaref4health.TranslateMeasurement("heartrate", sensorDataHR, dataTimestamp.Data);
                heartRate_ECGdevice_List.Add(heartRate_ECGdeviceMeasurement);
            }

            return true;
        }

        const double Gforce = 9.806; // m/s2 

        public double ComputeCrossAxialFunction(SensorData dataAccelX, SensorData dataAccelY, SensorData dataAccelZ)
        {
            double x = Math.Abs(dataAccelX.Data);
            double y = Math.Abs(dataAccelY.Data);
            double z = Math.Abs(dataAccelZ.Data) - Gforce;

            // Verify if the measure is out of range (some error in the measurement sent by the device)
            if (x > 8 * Gforce) x = VehicleCollisionDetection.Threshold;
            if (y > 8 * Gforce) y = VehicleCollisionDetection.Threshold;
            if (z > 8 * Gforce) z = VehicleCollisionDetection.Threshold;

            return Math.Pow(x, 2.0) + Math.Pow(y, 2.0) + Math.Pow(z, 2.0);
        }

        public bool DetectCollision(double currentCrossAxialEnergy)
        {
            VehicleCollisionDetection.crossAxialEnergyWhenCollisionDetected = currentCrossAxialEnergy;
            bool result = false;
            VehicleCollisionDetection.sumCrossAxialValues += currentCrossAxialEnergy;
            VehicleCollisionDetection.countCrossAxialValues++;
            VehicleCollisionDetection.meanCrossAxialValues = VehicleCollisionDetection.sumCrossAxialValues / VehicleCollisionDetection.countCrossAxialValues;
            VehicleCollisionDetection.varianceCrossAxialValues += Math.Pow(currentCrossAxialEnergy - VehicleCollisionDetection.meanCrossAxialValues, 2);

            VehicleCollisionDetection.sumOfDerivation += (currentCrossAxialEnergy) * (currentCrossAxialEnergy);
            VehicleCollisionDetection.sumOfDerivationAverage = VehicleCollisionDetection.sumOfDerivation / VehicleCollisionDetection.countCrossAxialValues;
            VehicleCollisionDetection.standardDeviation = Math.Sqrt(VehicleCollisionDetection.sumOfDerivationAverage - (VehicleCollisionDetection.meanCrossAxialValues * VehicleCollisionDetection.meanCrossAxialValues));

            //double standardDeviation = CalculateStandardDeviation(); // 100; // Math.Sqrt(varianceCrossAxialValues);
            // Anomaly detection
            double minVal = double.MinValue; // vehicleCollisionDetection.Threshold + vehicleCollisionDetection.meanCrossAxialValues - vehicleCollisionDetection.standardDeviation;
            double maxVal = VehicleCollisionDetection.Threshold + VehicleCollisionDetection.meanCrossAxialValues + VehicleCollisionDetection.standardDeviation;
            /*
            if ((currentCrossAxialEnergy < minVal) 
                || (currentCrossAxialEnergy > maxVal))
                result = true;
                */
            if (Math.Sqrt(currentCrossAxialEnergy) > VehicleCollisionDetection.Threshold)
                result = true;

            return result;
            //return true;
        }
        
        public void CompactECGData(SensorData dataTimestamp, SensorData dataECG_LL_RA, SensorData dataECG_LA_RA, SensorData dataECG_VX_RL)
        {

            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LA_RA, dataTimestamp.Data, dataECG_LA_RA.Data);
            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_LL_RA, dataTimestamp.Data, dataECG_LL_RA.Data);

            // Derived LL-LA
            StoreECGleadValue(ECG_LL_LA, dataTimestamp.Data, dataECG_LA_RA.Data - dataECG_LL_RA.Data);

            StoreECGleadValue(Shimmer3Configuration.SignalNames.ECG_VX_RL, dataTimestamp.Data, dataECG_VX_RL.Data);

        }

        public void StoreECGleadValue(string leadName, double timestamp, double leadValue)
        {
            KeyValuePair<double, double> timeseriesECGlead = new KeyValuePair<double, double>(timestamp, leadValue);

            if (leadName == Shimmer3Configuration.SignalNames.ECG_LA_RA)
            {
                valuesLead1_ECG_LA_RA = GetValuesLead1_ECG_LA_RA();
                lock (valuesLead1_ECG_LA_RA)
                {
                    valuesLead1_ECG_LA_RA.Add(timeseriesECGlead);
                }
            }
            else if (leadName == Shimmer3Configuration.SignalNames.ECG_LL_RA)
            {
                valuesLead2_ECG_LL_RA = GetValuesLead2_ECG_LL_RA();
                lock (valuesLead2_ECG_LL_RA)
                {
                    valuesLead2_ECG_LL_RA.Add(timeseriesECGlead);
                }
            }
            else if (leadName == ECG_LL_LA)
            {
                valuesLead3_ECG_LL_LA = GetValuesLead3_ECG_LL_LA();
                lock (valuesLead3_ECG_LL_LA)
                {
                    valuesLead3_ECG_LL_LA.Add(timeseriesECGlead);
                }
            }
            else if (leadName == Shimmer3Configuration.SignalNames.ECG_VX_RL)
            {
                valuesUnipolarLeadVx_RL = GetValuesUnipolarLeadVx_RL();
                lock (valuesUnipolarLeadVx_RL)
                {
                    valuesUnipolarLeadVx_RL.Add(timeseriesECGlead);
                }
            }

        }

        private JObject FormatMessageSAREF4health_FieldGateway(double lat, double lon)
        {
            JObject contextJSON = jsonLdSaref4health.GetContextJSON_SAREF4health();
            List<JObject> devicesComposingMobile = new List<JObject>();

            if (IsStreamingHealthdevice && false)
            {
                // ECG device (unit)
                JObject eCGDeviceJSON = FormatMessageSAREF4health_ECGDevice();

                devicesComposingMobile.Add(eCGDeviceJSON);

                AddProcessedAccelerometerFromHealthDevice(devicesComposingMobile);
            }

            AddSmartphoneAccelerationData(devicesComposingMobile);

            JObject mobileDevice = jsonLdSaref4health.GetFieldGatewayMobileDeviceJSON_SAREF4health("MobileDeviceId", contextJSON, devicesComposingMobile, lat, lon);

            return mobileDevice;
        }

        private void AddSmartphoneAccelerationData(List<JObject> devicesComposingMobile)
        {
            lock (smartphoneAccelerationManager)
            {
                if (smartphoneAccelerationManager.Accelerations.Count > 0)
                {
                    
                    string _id = "sarefInst:AccelerometerSensor_Triaxial_Smartphone_MotoG5Plus_MobileDeviceId";
                    string _label = "Accelerometer smartphone: average acceleration within device-cloud frequency";

                    List<Measurement> accelAxes = GetAverageAcceleration(this.smartphoneAccelerationManager);
                    JObject sensor_Accelerometer = jsonLdSaref4health.GetShimmerAccelerometerSensorJSON_SAREF4health(_id, _label, accelAxes[0], accelAxes[1], accelAxes[2]);
                    devicesComposingMobile.Add(sensor_Accelerometer);

                    smartphoneAccelerationManager.Accelerations.Clear();
                }
            }
        }

        private JObject FormatMessageSAREF4health_ECGDevice()
        {
            
            // Generate ECGSampleSequence
            JObject measurementSeriesLA_RA = CreateMessageAndDeleteList_SAREF4health("Lead1_ECG_LA_RA");
            JObject measurementSeriesLL_RA = CreateMessageAndDeleteList_SAREF4health("Lead2_ECG_LL_RA");
            JObject measurementSeriesLL_LA = CreateMessageAndDeleteList_SAREF4health("Lead3_ECG_LL_LA");
            JObject measurementSeriesVx_RL = CreateMessageAndDeleteList_SAREF4health("UnipolarLeadVx_RL");

            // Generate ECGLeadBipolarLimb (a ECGSensor)
            JObject sensor_Lead1_ECG_LA_RA = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesLA_RA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_I_code131329", "Lead I (LA-RA)", jsonLdSaref4health.measuresPropertyECGdata);
            JObject sensor_Lead2_ECG_LL_RA = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesLL_RA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_II_code131330", "Lead II (LL-RA)", jsonLdSaref4health.measuresPropertyECGdata);
            JObject sensor_Lead3_ECG_LL_LA = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesLL_LA, "saref4health:ECGLeadBipolarLimb", "sarefInst:ECGLead_III_code131389", "Lead III (LL-LA)", jsonLdSaref4health.measuresPropertyECGdata);

            // Generate ECGLeadUnipolar (a ECGSensor)
            JObject sensor_LeadVx_RL = jsonLdSaref4health.GetSensorJSON_SAREF4health(measurementSeriesVx_RL, "saref4health:ECGLeadUnipolar", "sarefInst:ECGLead_Vx_RL_code131389", "Lead Vx-RL", jsonLdSaref4health.measuresPropertyECGdata);

            // Generate ECGDevice message (Device composed at least of an ECGSensor ("container"))
            List<JObject> listSensorsOfDevice = new List<JObject>();
            listSensorsOfDevice.Add(sensor_Lead1_ECG_LA_RA);
            listSensorsOfDevice.Add(sensor_Lead2_ECG_LL_RA);
            listSensorsOfDevice.Add(sensor_Lead3_ECG_LL_LA);
            listSensorsOfDevice.Add(sensor_LeadVx_RL);

            lock (accelerationX_ECGdevice_List) lock(accelerationY_ECGdevice_List) lock (accelerationZ_ECGdevice_List)
            {
                if (accelerationX_ECGdevice_List.Count > 0)
                {
                    string _id = "sarefInst:AccelerometerSensor_Triaxial_ECGdevice_T9J-RN42";
                    string _label = "Accelerometer ECG device: average acceleration within device-cloud frequency (N x ECG unit sampling rate)";

                    JObject sensor_Accelerometer = jsonLdSaref4health.GetShimmerAccelerometerSensorJSON_SAREF4health(_id, _label, GetAverageAcceleration(accelerationX_ECGdevice_List), GetAverageAcceleration(accelerationY_ECGdevice_List), GetAverageAcceleration(accelerationZ_ECGdevice_List));
                    listSensorsOfDevice.Add(sensor_Accelerometer);
                    accelerationX_ECGdevice_List.Clear();
                    accelerationY_ECGdevice_List.Clear();
                    accelerationZ_ECGdevice_List.Clear();
                }
            }

            // Add battery consumption sensor with last received data 
            lock (batteryLevelList)
            {
                if (batteryLevelList.Count > 0)
                {
                    Measurement lastBatteryLevel = batteryLevelList[batteryLevelList.Count - 1];
                    JObject sensor_BatteryLevel = jsonLdSaref4health.GetSensorJSON_SAREF4health(lastBatteryLevel.JSONLDobject, "saref:Sensor", "sarefInst:Shimmer3BatteryLevelSensor_T9JRN42", "Battery level sensor of Shimmer 3  (id: T9JRN42)", jsonLdSaref4health.sarefInst_BatteryLevel);
                    listSensorsOfDevice.Add(sensor_BatteryLevel);
                    batteryLevelList.Clear();
                }
            }

            lock (heartRate_ECGdevice_List)
            { 
                if (heartRate_ECGdevice_List.Count > 0)
                {
                    JObject sensor_HeartRate = jsonLdSaref4health.GetSensorJSON_SAREF4health(heartRate_ECGdevice_List[heartRate_ECGdevice_List.Count-1].JSONLDobject, "saref:Sensor", "sarefInst:Shimmer3HeartRate_T9JRN42", "Heart rate computed by Shimmer API", jsonLdSaref4health.sarefInst_ProcessedHeartRate);
                    listSensorsOfDevice.Add(sensor_HeartRate);
                    heartRate_ECGdevice_List.Clear();
                }
            }

            JObject eCGDeviceJSON = jsonLdSaref4health.GetECGDeviceJSON_SAREF4health(listSensorsOfDevice);

            return eCGDeviceJSON;
        }

        private JObject CreateMessageAndDeleteList_SAREF4health(string lead)
        {
            JObject measurementSeries = null;
            switch (lead)
            {
                case "Lead1_ECG_LA_RA":
                    valuesLead1_ECG_LA_RA = GetValuesLead1_ECG_LA_RA();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesLead1_ECG_LA_RA, lead);
                    break;
                case "Lead2_ECG_LL_RA":
                    valuesLead2_ECG_LL_RA = GetValuesLead2_ECG_LL_RA();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesLead2_ECG_LL_RA, lead);
                    break;
                case "Lead3_ECG_LL_LA":
                    valuesLead3_ECG_LL_LA = GetValuesLead3_ECG_LL_LA();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesLead3_ECG_LL_LA, lead);
                    break;
                case "UnipolarLeadVx_RL":
                    valuesUnipolarLeadVx_RL = GetValuesUnipolarLeadVx_RL();
                    measurementSeries = jsonLdSaref4health.CreateMessageAndDeleteList_SAREF4health(valuesUnipolarLeadVx_RL, lead);
                    break;
                default:
                    break;
            }
            return measurementSeries;
        }

        private Measurement GetAverageAcceleration(List<Measurement> measurementsAxis)
        {
            if (measurementsAxis.Count == 0)
                return null;

            Measurement result = new Measurement();
            lock (measurementsAxis)
            {
                double accelValueTotal = 0;
                int accelMeasurementsCount = 0;
                double timestamp = double.MinValue;
                foreach (Measurement measurement in measurementsAxis)
                {
                    timestamp = measurement.HasTimestamp;
                    if (accelMeasurementsCount == 0)
                        result = measurement;

                    accelValueTotal += measurement.HasValue;
                    accelMeasurementsCount++;
                }
                result.HasValue = accelValueTotal / accelMeasurementsCount;
                result.HasTimestamp = timestamp;
            }
            return result;
        }

        private List<Measurement> GetAverageAcceleration(SmartphoneAccelerationManager accelMgr)
        {
            if (accelMgr.Accelerations.Count == 0)
                return null;

            List<Measurement> result = new List<EWS.SAREF4health.Measurement>();

            double accelValueTotal = 0;
            int accelCount = 0;
            DateTime timestampAsDateTime = DateTime.MinValue;
            Dictionary<string, double> averages = new Dictionary<string, double>();
            averages.Add("x", 0);
            averages.Add("y", 0);
            averages.Add("z", 0);

            foreach (KeyValuePair<DateTime, SmartphoneAcceleration> acceleration in accelMgr.Accelerations)
            {
                if (timestampAsDateTime == DateTime.MinValue)
                    timestampAsDateTime = acceleration.Key;

                averages["x"] += acceleration.Value.X;
                averages["y"] += acceleration.Value.Y;
                averages["z"] += acceleration.Value.Z;
                
                accelCount++;
            }

            if (accelCount > 0)
            {
                averages["x"] = averages["x"] / accelCount;
                averages["y"] = averages["y"] / accelCount;
                averages["z"] = averages["z"] / accelCount;
            }

            string measurementLabel = "Measurement acceleration from smartphone";

            SensorData dataAccelX = new SensorData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, averages["x"]);
            Measurement accelerationX = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, dataAccelX, double.MinValue, measurementLabel);
            accelerationX.HasTimestampAsDateTime = timestampAsDateTime;

            SensorData dataAccelY = new SensorData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, averages["y"]);
            Measurement accelerationY = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, dataAccelY, double.MinValue, measurementLabel);
            accelerationY.HasTimestampAsDateTime = timestampAsDateTime;

            SensorData dataAccelZ = new SensorData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, averages["z"]);
            Measurement accelerationZ = jsonLdSaref4health.TranslateMeasurement(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, dataAccelZ, double.MinValue, measurementLabel);
            accelerationZ.HasTimestampAsDateTime = timestampAsDateTime;

            result.Add(accelerationX);
            result.Add(accelerationY);
            result.Add(accelerationZ);

            return result;
        }

        private void AddProcessedAccelerometerFromHealthDevice(List<JObject> devicesComposingMobile)
        {
            // Add accelerometer sensor with last received data (cross axial energy - tri-axial based) and a measurement indicating a collision was detected during the time interval
            lock (accelerationCrossAxialList) lock (VehicleCollisionDetection)
            {
                if (accelerationCrossAxialList.Count > 0)
                {
                    List<JObject> processedMeasurements = new List<JObject>();

                    Measurement lastCrossAxialComputed = accelerationCrossAxialList[accelerationCrossAxialList.Count - 1];
                    double avgCrossAxial = GetAverage(accelerationCrossAxialList);
                    lastCrossAxialComputed.HasValue = avgCrossAxial;
                    lastCrossAxialComputed.Label = "Average value of the calculated cross-axial function within the smartphone_to_cloud frequency";
                    processedMeasurements.Add(lastCrossAxialComputed.JSONLDobject);

                    // Add collisionDetected, mean, stdDeviation and value as measurements if a collision was detected
                    SensorData collisionDetectedObj = new SensorData("boolean", Convert.ToDouble(VehicleCollisionDetection.collisionDetected));
                    if (VehicleCollisionDetection.collisionDetected)
                    {
                        // Measurement collision detected (timestamp when it was detected, bool = true)
                        Measurement collisionDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("collisionDetected", collisionDetectedObj, VehicleCollisionDetection.timestampCollisionDetected, "Boolean (0 or 1): collision detected based on threshold");
                        processedMeasurements.Add(collisionDetectedMeasurement.JSONLDobject);

                        // Measurements of mean, standard deviation and cross axial value when detected
                        SensorData collisionDetectedObjMean = new SensorData("crossaxialfunction", VehicleCollisionDetection.meanCrossAxialValues);
                        Measurement meanDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("cross-axial-energy_mean", collisionDetectedObjMean, VehicleCollisionDetection.timestampCollisionDetected, "Cross-axial function: mean value");
                        processedMeasurements.Add(meanDetectedMeasurement.JSONLDobject);

                        SensorData collisionDetectedObjStd = new SensorData("crossaxialfunction", VehicleCollisionDetection.standardDeviation);
                        Measurement stddevDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("cross-axial-energy_std-dev", collisionDetectedObjStd, VehicleCollisionDetection.timestampCollisionDetected, "Cross-axial function: standard deviation");
                        processedMeasurements.Add(stddevDetectedMeasurement.JSONLDobject);

                        SensorData collisionDetectedObjCrossAxial = new SensorData("crossaxialfunction", VehicleCollisionDetection.crossAxialEnergyWhenCollisionDetected);
                        Measurement crossaxDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("cross-axial-energy_max", collisionDetectedObjCrossAxial, VehicleCollisionDetection.timestampCollisionDetected, "Cross-axial function: maximum");
                        crossaxDetectedMeasurement.Label = "value processed when collision detected";
                        processedMeasurements.Add(crossaxDetectedMeasurement.JSONLDobject);

                        SensorData collisionDetectedObjThreshold = new SensorData("m/(sec^2)", VehicleCollisionDetection.Threshold);
                        Measurement thresholdDetectedMeasurement = jsonLdSaref4health.TranslateMeasurement("ThresholdGforce", collisionDetectedObjThreshold, VehicleCollisionDetection.timestampCollisionDetected, "Value of threshold used to detect collision");
                        processedMeasurements.Add(thresholdDetectedMeasurement.JSONLDobject);

                    }

                    // Create Processed Accelerometer: accelerometer that is processed by the smartphone using acceleration data from ECG device
                    JObject sensorProcessedAccelerometer = jsonLdSaref4health.GetSensorJSON_SAREF4health(processedMeasurements, "saref:Sensor", "sarefInst:ProcessedAccelerometer", "Accelerometer measurements processed by mobile app with acceleration data from Shimmer3 ECG unit", jsonLdSaref4health.sarefInst_ProcessedAccelerometer);
                    devicesComposingMobile.Add(sensorProcessedAccelerometer);
                    VehicleCollisionDetection.ClearDetectCollisionVariables(accelerationCrossAxialList);
                }
            }
        }

        private double GetAverage(List<Measurement> measurements)
        {
            double result = 0;
            int counter = 0;

            foreach (Measurement measurement in measurements)
            {
                result += measurement.HasValue;
                counter++;
            }

            if (counter > 0)
                result = result / counter;

            return result;
        }

        TripPoint lastPoint = new TripPoint();

        public void SendECGdataToCloud()
        {
            // Send to IoT Hub: SAREF4health
            try
            {
                if (isRecording && obdDataProcessor != null)
                {
                    double lat = 0.0;
                    double lon = 0.0;
                    
                    lock (lastPoint)
                    {
                        lat = lastPoint.Latitude;
                        lon = lastPoint.Longitude;
                    }
                    
                    JObject fieldGatewayDevice_Smartphone = FormatMessageSAREF4health_FieldGateway(lat, lon);
                    obdDataProcessor.SendHealthDataToIOTHub(fieldGatewayDevice_Smartphone);

                    lock (VehicleCollisionDetection)
                    {
                        VehicleCollisionDetection.collisionDetected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Report(ex);
            }
        }

        private SmartphoneAccelerationManager smartphoneAccelerationManager = new SmartphoneAccelerationManager();

        public void OnAccelerationChanged(float x, float y, float z)
        {
            
            if (smartphoneAccelerationManager != null)
            {
                SmartphoneAcceleration acceleration = new SmartphoneAcceleration(x, y, z);

                lock (smartphoneAccelerationManager)
                {
                    smartphoneAccelerationManager.Accelerations.Add(DateTime.Now, acceleration);
                }
            }
        }

        #endregion

            #region Logistics: LogiCO/LogiServ/LogiTran

        private JObject FormatMessageLogistics(Trip currentTrip, TripPoint currentPoint)
        {
            Transport transport = TranslateTransport(currentTrip, currentPoint);
            
            string transportEventId = transport.Identifier + "_" + Guid.NewGuid();
            JObject location = GetLogicoLocation(currentPoint);
            JObject time_now = GetTimeInstant(DateTime.Now);
            JObject truck = GetLogicoTruck(transport);
            JObject transport_jo = GetLogicoTransport(transport);

            JObject result = new JObject();

            JObject contextJSON = GetContextLogistics();
            result.Add("@context", contextJSON);
            result.Add("@id", "LogiTrans:TransportEvent_" + transportEventId);
            result.Add("@type", "LogiTrans:TransportEvent");
            result.Add("LogiCO:hasLocation", location);
            result.Add("LogiServ:hasTime", time_now);
            result.Add("LogiTrans:hasTransportHandlingUnit", truck);
            result.Add("dul:isComponentOf", transport_jo);

            return result;
        }

        private JObject GetLogicoTransport(Transport transport)
        {
            JObject result = new JObject();
            JObject time_Instant_Begin = GetTimeInstant(transport.hasBeginning);
            JObject time_Instant_End = GetTimeInstant(transport.hasEnd);
            
            result.Add("@id", "LogiServInst:Transport_" + transport.Identifier);
            result.Add("@type", "LogiServ:Transport");
            result.Add("time:hasBeginning", time_Instant_Begin);
            //result.Add("time:hasEnd", time_Instant_End);

            return result;
        }

        private JObject GetLogicoTruck(Transport transport)
        {
            JObject result = new JObject();

            string truck_plate = "XPT01298";
            JObject cargo = GetLogicoCargo(transport);
            JObject plate = GetLogicoTruckId(truck_plate);

            result.Add("@id", "LogiCO:Truck_" + truck_plate);
            result.Add("@type", "LogiCO:Truck");
            result.Add("LogiCO:contains", cargo);
            result.Add("LogiCO:hasID", plate);

            return result;
        }

        private JObject GetLogicoTruckId(string truck_plate)
        {
            JObject result = new JObject();
            
            result.Add("@id", "LogiCO:Identifier_Truck_" + truck_plate);
            result.Add("@type", "LogiCO:Identifier");
            result.Add("rdfs:label", truck_plate);

            return result;
        }

        private JObject GetLogicoCargo(Transport transport)
        {
            JObject result = new JObject();

            string cargoId = transport.Identifier + "_CargoId_TransportingGoodsAt_Location_" + DateTime.Now.ToString("o");
            bool isDangerous_GoodsBeingTransported = true;
            JArray goodsItem = GetLogicoGoodsItem(transport);

            result.Add("@id", "LogiTrans:Cargo_" + cargoId);
            result.Add("@type", "LogiServ:Cargo");
            result.Add("rdfs:label", "Cargo being transported by a truck, contains goods");
            result.Add("LogiCO:isDangerous", isDangerous_GoodsBeingTransported);
            result.Add("LogiCO:contains", goodsItem);

            return result;
        }

        private JArray GetLogicoGoodsItem(Transport transport)
        {
            JArray array = new JArray();
            for (int i = 0; i < 5; i++)
            {
                JObject result = new JObject();

                string itemId = transport.Identifier + "_GoodsItemId";

                result.Add("@id", "LogiTrans:GoodsItem_" + itemId);
                result.Add("@type", "LogiTrans:GoodsItem");
                result.Add("rdfs:label", "Goods item " + i);

                array.Add(result);
            }
            return array;
        }

        private JObject GetLogicoLocation(TripPoint currentPoint)
        {
            JObject result = new JObject();

            JObject locationId = GetLogicoLocationId(currentPoint);

            result.Add("@id", "LogiTrans:TransportLocation_" + currentPoint.Latitude + "_" + currentPoint.Longitude);
            result.Add("@type", "LogiTrans:TransportLocation");
            result.Add("LogiCO:hasID", locationId);
            
            return result;
        }

        private JObject GetLogicoLocationId(TripPoint currentPoint)
        {
            JObject result = new JObject();

            JObject lat = new JObject();
            lat.Add("@type", "http://www.w3.org/2001/XMLSchema#float");
            lat.Add("@value", currentPoint.Latitude);

            JObject lon = new JObject();
            lon.Add("@type", "http://www.w3.org/2001/XMLSchema#float");
            lon.Add("@value", currentPoint.Longitude);

            result.Add("@id", "LogiCO:Identifier_Location_" + currentPoint.Latitude + "_" + currentPoint.Longitude);
            result.Add("@type", JArray.Parse("['LogiCO:Identifier','geo:Point']"));
            result.Add("geo:lat", lat);
            result.Add("geo:long", lon);

            return result;
        }

        private JObject GetTimeInstant(DateTime hasBeginning)
        {
            JObject result = new JObject();

            result.Add("@id", "timeInst:Instant_" + hasBeginning.ToString("o"));
            result.Add("@type", "time:Instant");
            result.Add("xsd:inXSDDateTime", hasBeginning.ToString("o"));

            return result;
        }

        public JObject GetContextLogistics()
        {
            JObject contextJO = new JObject();

            string context = @"
{
    'hasConstituent' : {
      '@id' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#hasConstituent',
      '@type' : '@id'
    },
    'hasConsumer' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasConsumer',
      '@type' : '@id'
    },
    'isConstituentOf' : {
      '@id' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#isConstituentOf',
      '@type' : '@id'
    },
    'label' : {
      '@id' : 'http://www.w3.org/2000/01/rdf-schema#label'
    },
    'hasDestination' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasDestination',
      '@type' : '@id'
    },
    'hasEnd' : {
      '@id' : 'http://www.w3.org/2006/time#hasEnd',
      '@type' : '@id'
    },
    'hasBeginning' : {
      '@id' : 'http://www.w3.org/2006/time#hasBeginning',
      '@type' : '@id'
    },
    'hasProvider' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasProvider',
      '@type' : '@id'
    },
    'hasOrigin' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasOrigin',
      '@type' : '@id'
    },
    'hasStatus' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasStatus'
    },
    'inXSDDateTime' : {
      '@id' : 'http://www.w3.org/2006/time#inXSDDateTime',
      '@type' : 'http://www.w3.org/2001/XMLSchema#dateTime'
    },    
    'hasLocation' : {
      '@id' : 'http://ontology.tno.nl/logico#hasLocation',
      '@type' : '@id'
    },
    'hasTime' : {
      '@id' : 'http://ontology.tno.nl/logiserv#hasTime',
      '@type' : '@id'
    },
    'hasTransportHandlingUnit' : {
      '@id' : 'http://ontology.tno.nl/transport#hasTransportHandlingUnit',
      '@type' : '@id'
    },
    'isComponentOf' : {
      '@id' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#isComponentOf',
      '@type' : '@id'
    },
    'contains' : {
      '@id' : 'http://ontology.tno.nl/logico#contains',
      '@type' : '@id'
    },
    'hasID' : {
      '@id' : 'http://ontology.tno.nl/logico#hasID',
      '@type' : '@id'
    },
    'isDangerous' : {
      '@id' : 'http://ontology.tno.nl/logico#isDangerous',
      '@type' : 'http://www.w3.org/2001/XMLSchema#boolean'
    },
    'lat' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#lat',
      '@type' : 'http://www.w3.org/2001/XMLSchema#decimal'
    },
    'long' : {
      '@id' : 'http://www.w3.org/2003/01/geo/wgs84_pos#long',
      '@type' : 'http://www.w3.org/2001/XMLSchema#decimal'
    },
    'LogiServ' : 'http://ontology.tno.nl/logiserv#',
    'LogiCO' : 'http://ontology.tno.nl/logico#',
    'LogiTrans' : 'http://ontology.tno.nl/transport#',
    'LogisticsInst' : 'http://ontology.tno.nl/transport/instances/#',
    'owl' : 'http://www.w3.org/2002/07/owl#',
    'xsd' : 'http://www.w3.org/2001/XMLSchema#',
    'skos' : 'http://www.w3.org/2004/02/skos/core#',
    'rdfs' : 'http://www.w3.org/2000/01/rdf-schema#',
    'geo' : 'http://www.w3.org/2003/01/geo/wgs84_pos#',
    'dct' : 'http://purl.org/dc/terms/',
    'rdf' : 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
    'xml' : 'http://www.w3.org/XML/1998/namespace',
    'dcterms' : 'http://purl.org/dc/terms/',
    'dul' : 'http://www.ontologydesignpatterns.org/ont/dul/DUL.owl#',
    'time' : 'http://www.w3.org/2006/time#',
    'dc' : 'http://purl.org/dc/elements/1.1/'
  }
            ";

            contextJO = JObject.Parse(context);

            return contextJO;
        }

        private Transport TranslateTransport(Trip currentTrip, TripPoint currentPoint)
        {
            Transport result = new EWS.Logistics.LogiServ.Transport(currentTrip);

            return result;
        }

        #endregion

    }
}