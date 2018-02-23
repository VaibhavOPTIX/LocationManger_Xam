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
using Android.Locations;

using System.Runtime.CompilerServices;
using locationManager.utility;
using Android.Util;
using locationManager.network;
using locationManager.model;
using static locationManager.model.pushObject;
using Newtonsoft.Json;
using System.Net.Http;
using Android.Support.V4.App;
using Android;

namespace locationManager.ServicePackage
{
    [Service(Exported = false, Name = "locationManager.servicePackage.LocationServiceHelper", Label ="LocationServiceHelper",Permission = "android.permission.BIND_JOB_SERVICE" )]
    public class LocationServiceHelper : JobIntentService,ILocationListener
    {
        const int JOB_ID = 1000;
        private const String TAG = "LocationWriteService";
        public const String LOCK_NAME_STATIC = "LocationWriteService.Static";
        private const int LOCATION_INTERVAL = 1000 * 15;
        private const float LOCATION_DISTANCE = 1f;
        private static PowerManager.WakeLock lockStatic = null;
        public long serviceStartTime;

        Location mLastLocation;
        NetworkCall networkCall;

        private Android.Locations.LocationManager mLocationManager = null;
        Location[] mLocationListeners = null;

        HandlerThread handlerThread;

        ResultReceiver receiver;

        /**
        * Target we publish for clients to send messages to IncomingHandler.
        */
        Messenger mMessenger = null;


        // this used to get a wakelock when a file is to be written to as it may so happen the CPU is asleep
        public void AcquireStaticLock() => GetLock().Acquire();

        [MethodImpl(MethodImplOptions.Synchronized)]
        private PowerManager.WakeLock GetLock()
        {
            if (lockStatic == null)
            {
                PowerManager mgr = (PowerManager)this.GetSystemService(Context.PowerService);

                lockStatic = mgr.NewWakeLock(WakeLockFlags.Partial,
                        LOCK_NAME_STATIC);
                lockStatic.SetReferenceCounted(true);
            }
            return (lockStatic);
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            base.OnStartCommand(intent, flags, startId);
            if (intent != null && intent.GetParcelableExtra("receiver") != null)
                receiver = (ResultReceiver)intent.GetParcelableExtra("receiver");

            mLocationListeners = new Location[] { new Location(Android.Locations.LocationManager.GpsProvider), new Location(Android.Locations.LocationManager.NetworkProvider) };

            return StartCommandResult.Sticky;
        }
    

        public override void OnCreate()
        {
            base.OnCreate();
            serviceStartTime = UtilityClass.GetUTC();
            if( mMessenger ==null)
                mMessenger = new Messenger(new IncomingHandler(this));
            new Location(Android.Locations.LocationManager.GpsProvider);
            new Location(Android.Locations.LocationManager.NetworkProvider);

            // Creates a new background thread for processing messages or runnables sequentially
            handlerThread = new HandlerThread("LocationThread");
            // Starts the background thread
            handlerThread.Start();

            // Create a handler attached to the HandlerThread's Looper
            Handler mHandler = new Handler(handlerThread.Looper);
            mHandler.Post(() =>
            {
                InitializeLocationManager();
                mLocationManager.RequestLocationUpdates(
                            Android.Locations.LocationManager.GpsProvider, LOCATION_INTERVAL, LOCATION_DISTANCE,this);
                mLocationManager.RequestLocationUpdates(
                           Android.Locations.LocationManager.NetworkProvider, LOCATION_INTERVAL, LOCATION_DISTANCE,this);
            });
        }

        private void InitializeLocationManager()
        {
            if (mLocationManager == null)
            {
                mLocationManager = (Android.Locations.LocationManager)ApplicationContext.GetSystemService(Context.LocationService);
            }
        }

        /*
        * removes the handler responsible for getting/writing the battery information and handlerThread
        * is also quit to stop the service process and finally the service itself is stopped , also
        * removes all location updates for the specified LocationListener.
        * */
        public void ReleaseResources()
        {
            base.OnDestroy();
            if (lockStatic != null && lockStatic.IsHeld)
                GetLock().Release();

            handlerThread.QuitSafely();
            StopSelf();
            if (mLocationManager != null)
            {
                for (int i = 0; i < mLocationListeners.Length; i++)
                {
                    try
                    {
                        mLocationManager.RemoveUpdates(this);
                    }
                    catch (Exception ex)
                    {
                        Log.Info(TAG, "fail to remove location listners, ignore", ex);
                    }
                }
            }
        }

        /**
         * Convenience method for enqueuing work in to this service.
         */
        public static void EnqueueWork(Context context, Intent work)
        {
            
                EnqueueWork(context, Java.Lang.Class.FromType(typeof(LocationServiceHelper)), JOB_ID, work);
        }


        protected override void OnHandleWork(Intent intent)
        {
            if (networkCall == null)
                networkCall = new NetworkCall();
            if (intent != null && intent.GetParcelableExtra("receiver") != null)
                receiver = (ResultReceiver)intent.GetParcelableExtra("receiver");

            if (mMessenger == null)
                mMessenger = new Messenger(new IncomingHandler(this));

            Bundle bundle = new Bundle();
            bundle.PutBinder("binder", mMessenger.Binder);
            receiver.Send(Result.Ok, bundle);
            OnCreate();
            UtilityClass.scheduleJob(this);
        }

        void ILocationListener.OnLocationChanged(Location location)
        {
            //mLastLocation.Set(location);

            Bundle bundle = new Bundle();
            bundle.PutString("cordinate", location.Latitude + ":" + location.Longitude);
            receiver.Send(Result.Ok, bundle);

            Log.Error(TAG, "Location Latitude =>>" + location.Latitude + " Longitude=>>" + location.Longitude);
            Console.WriteLine("Location Latitude =>>" + location.Latitude + " Longitude=>>" + location.Longitude);

            pushObject obj = new pushObject();
            obj.items = new List<pushItem>();
            obj.id = 2;
            obj.items.Add(new pushItem { lan = Convert.ToDecimal(location.Longitude), lat = Convert.ToDecimal(location.Latitude), localid = 0, timestamp = UtilityClass.GetUTC().ToString() });
            var stringContent = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            networkCall.SendCoordinateAsync(stringContent);

            AcquireStaticLock();
            if (UtilityClass.GenerateNoteOnSD(this, "Location_log", JsonConvert.SerializeObject(obj), serviceStartTime))
            {
                Toast.MakeText(this, "Write Complete\n" + JsonConvert.SerializeObject(obj), ToastLength.Short).Show();
            }
            GetLock().Release();
        }

        void ILocationListener.OnProviderDisabled(string provider)
        {
        }

        void ILocationListener.OnProviderEnabled(string provider)
        {
        }

        void ILocationListener.OnStatusChanged(string provider, Availability status, Bundle extras)
        {
        }

        public class IncomingHandler: Handler
        {
            LocationServiceHelper mContext;
            public IncomingHandler(LocationServiceHelper context)
            {
                this.mContext = context;
            }

            public override void HandleMessage(Message msg)
            {
               
                Bundle bundle = new Bundle();
                switch (msg.What)
                {
                    case UtilityClass.GET_START_TIME:
                        bundle.PutLong("startTime", mContext.serviceStartTime);
                        mContext.receiver.Send(Result.Ok, bundle);
                        break;
                    case UtilityClass.STOP_SERVICE:
                        mContext.ReleaseResources();
                        break;
                    default:
                        base.HandleMessage(msg);
                        break;
                }
            }
        }
    }
}