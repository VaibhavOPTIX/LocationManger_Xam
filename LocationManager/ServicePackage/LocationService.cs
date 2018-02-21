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
    public class LocationService : Service
    {
        private const String TAG = "LocationWriteService";
        public const String LOCK_NAME_STATIC = "LocationWriteService.Static";
        private const int LOCATION_INTERVAL = 1000 * 60 * 2;
        private const int SYSTEM_HEALTH_INTERVAL = 1000 * 60 * 10;
        private const float LOCATION_DISTANCE = 0f;
        private static PowerManager.WakeLock lockStatic = null;
        public long serviceStartTime;

        private Android.Locations.LocationManager mLocationManager = null;
        LocationListener[] mLocationListeners = new LocationListener[]{
            new LocationListener(Android.Locations.LocationManager.GpsProvider),
            new LocationListener(Android.Locations.LocationManager.NetworkProvider)
        };

        Handler handler;
        HandlerThread handlerThread;

        ResultReceiver receiver;

        /**
        * Target we publish for clients to send messages to IncomingHandler.
        */

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
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
            if (intent != null && intent.GetParcelableExtra("receiver") != null)
                receiver = (ResultReceiver)intent.GetParcelableExtra("receiver");
            return StartCommandResult.Sticky;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            serviceStartTime = UtilityClass.getUTC();

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




        public class LocationListener : Android.Locations.ILocationListener
        {
            Location mLastLocation;

            public LocationListener(string gpsProvider)
            {
                mLastLocation = new Location(gpsProvider);
            }

            IntPtr IJavaObject.Handle => throw new NotImplementedException();

            void IDisposable.Dispose()
            {
                throw new NotImplementedException();
            }

            void ILocationListener.OnLocationChanged(Location location)
            {
                mLastLocation.Set(location);
                /*
                 * Need to make a network call to push the lat long to the server
                 */
            }

            void ILocationListener.OnProviderDisabled(string provider)
            {
                throw new NotImplementedException();
            }

            void ILocationListener.OnProviderEnabled(string provider)
            {
                throw new NotImplementedException();
            }

            void ILocationListener.OnStatusChanged(string provider, Availability status, Bundle extras)
            {
                throw new NotImplementedException();
            }
        }
    }
}