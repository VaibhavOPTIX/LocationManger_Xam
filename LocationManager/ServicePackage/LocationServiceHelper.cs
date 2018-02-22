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
using LocationManager.Utility;
using Android.Util;

namespace LocationManager.ServicePackage
{
    [Service(Exported = false, Name = "LocationManager.ServicePackage.LocationServiceHelper", Label ="LocationServiceHelper")]
    public class LocationServiceHelper : Service
    {
        private const String TAG = "LocationWriteService";
        public const String LOCK_NAME_STATIC = "LocationWriteService.Static";
        private const int LOCATION_INTERVAL = 1000 * 30;
        private const float LOCATION_DISTANCE = 10f;
        private static PowerManager.WakeLock lockStatic = null;
        public long serviceStartTime;

        private Android.Locations.LocationManager mLocationManager = null;
        LocationListenerHelper[] mLocationListeners = new LocationListenerHelper[]{
            new LocationListenerHelper(Android.Locations.LocationManager.GpsProvider),
            new LocationListenerHelper(Android.Locations.LocationManager.NetworkProvider)
        };

        HandlerThread handlerThread;

        ResultReceiver receiver;

        /**
        * Target we publish for clients to send messages to IncomingHandler.
        */
        Messenger mMessenger = null;

        public override IBinder OnBind(Intent intent)
        {
            return mMessenger.Binder;
        }

        // this used to get a wakelock when a file is to be written to as it may so happen the CPU is asleep
        public void AcquireStaticLock(Context context) => GetLock(context).Acquire();

        [MethodImpl(MethodImplOptions.Synchronized)]
        private PowerManager.WakeLock GetLock(Context context)
        {
            if (lockStatic == null)
            {
                PowerManager mgr = (PowerManager)context.GetSystemService(Context.PowerService);

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
            return StartCommandResult.Sticky;
        }
    

        public override void OnCreate()
        {
            base.OnCreate();
            serviceStartTime = UtilityClass.GetUTC();
            if( mMessenger ==null)
                mMessenger = new Messenger(new IncomingHandler(this));

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
                            Android.Locations.LocationManager.GpsProvider, LOCATION_INTERVAL, LOCATION_DISTANCE,
                            mLocationListeners[0]);
                mLocationManager.RequestLocationUpdates(
                           Android.Locations.LocationManager.NetworkProvider, LOCATION_INTERVAL, LOCATION_DISTANCE,
                           mLocationListeners[1]);
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
                GetLock(this).Release();

            handlerThread.QuitSafely();
            StopSelf();
            if (mLocationManager != null)
            {
                for (int i = 0; i < mLocationListeners.Length; i++)
                {
                    try
                    {
                        mLocationManager.RemoveUpdates(mLocationListeners[i]);
                    }
                    catch (Exception ex)
                    {
                        Log.Info(TAG, "fail to remove location listners, ignore", ex);
                    }
                }
            }
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


        public class LocationListenerHelper : Java.Lang.Object,Android.Locations.ILocationListener
        {
            Location mLastLocation;

            public LocationListenerHelper(string gpsProvider)
            {
                mLastLocation = new Location(gpsProvider);
            }

            void ILocationListener.OnLocationChanged(Location location)
            {
                mLastLocation.Set(location);
                Log.Error(TAG, "Location Latitude =>>" + location.Latitude + " Longitude=>>" + location.Longitude);
                /*
                 * Need to make a network call to push the lat long to the server
                 */
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
        }
    }
}