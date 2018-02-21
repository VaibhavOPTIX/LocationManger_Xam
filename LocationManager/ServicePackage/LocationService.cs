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

using System.Runtime.CompilerServices;
using LocationManager.Utility;

namespace LocationManager.ServicePackage
{
    class LocationService : Service
    {
        public const String LOCK_NAME_STATIC = "LocationWriteService.Static";
        private const int LOCATION_INTERVAL = 1000 * 60 * 2;
        private const int SYSTEM_HEALTH_INTERVAL = 1000 * 60 * 10;
        private const float LOCATION_DISTANCE = 0f;
        private static PowerManager.WakeLock lockStatic = null;
        public long serviceStartTime;

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
            mHandler.Post(()=>
            {
                initializeLocationManager();
                //run an handler to get/write battery level and network info every 10 minutes
                if (handler == null)
                {
                    handler = new Handler();
                }
                handler.postDelayed(batteryRunnable, SYSTEM_HEALTH_INTERVAL);
                try
                {
                    mLocationManager.requestLocationUpdates(
                            LocationManager.GPS_PROVIDER, LOCATION_INTERVAL, LOCATION_DISTANCE,
                            mLocationListeners[0]);

                    if (UtilityClass.isBuildNougat())
                    {
                        GnssStatusCallback = new GnssStatus.Callback() {
                            @Override
                            public void onSatelliteStatusChanged(GnssStatus status)
                        {
                            super.onSatelliteStatusChanged(status);
                            if (status != null)
                            {
                                final int length = status.getSatelliteCount();
                                int index = 0;
                                satelliteCount = 0;
                                while (index < length)
                                {
                                    if (status.usedInFix(index))
                                    {
                                        satelliteCount++;
                                    }
                                    index++;
                                }
                            }
                        }
                    };
                    mLocationManager.registerGnssStatusCallback(GnssStatusCallback);
                } else
                        mLocationManager.addGpsStatusListener(GpsStatuslistner);

            } catch (java.lang.SecurityException ex)
            {
                Log.i(TAG, "fail to request location update, ignore", ex);
            }
            catch (IllegalArgumentException ex)
            {
                Log.d(TAG, "gps provider does not exist " + ex.getMessage());
            }
        }
         
    }
}