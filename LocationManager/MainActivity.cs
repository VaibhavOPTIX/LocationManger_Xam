using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Content;
using System;
using LocationManager.Utility;
using Android.Support.V4.App;
using Android;
using Android.Content.PM;
using Android.Runtime;
using LocationManager.ServicePackage;
using Java.Lang;
using Java.Util.Concurrent;

namespace LocationManager
{
    [Activity(Label = "LocationManager", MainLauncher = true, Theme = "@style/AppTheme.NoActionBar")]
    public class MainActivity : AppCompatActivity, ServiceResultReceiver.IReceiver, IServiceConnection
    {
        Button startService, stopService;
        TextView startTime, status, coordinate;

        public ServiceResultReceiver receiver;

        public string[] serviceStartTime;

        bool permissionGranted;
        Context mContext;
        Handler handler;
        bool mBound = false;
        Messenger mService = null;
        RunnableHelper helper;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            InitializeViewId();
            mContext = this;

            //Check if the required permissions are granted. Required for system after API 23
            CheckLocationPermission();

            // Setup the callback for when data is received from the service
            SetupServiceReceiver();

            startService.Click += (s, e) =>
            {
                StartService();
            };

            stopService.Click += StopServiceListener;

            if (handler == null)
            {
                handler = new Handler();
                helper = new RunnableHelper(this,handler);
            }
        }


        // Setup the callback for when data is received from the service
        public void SetupServiceReceiver()
        {
            receiver = new ServiceResultReceiver(new Handler());
            // This is where we specify what happens when data is received from the service
            receiver.SetReceiver(this);
        }



        /**
        * Initializing the view ID for the activity
        */
        private void InitializeViewId()
        {
            startService = FindViewById<Button>(Resource.Id.startService);
            stopService = FindViewById<Button>(Resource.Id.stopService);
            startTime = FindViewById<TextView>(Resource.Id.startTime);
            status = FindViewById<TextView>(Resource.Id.status);
            coordinate = FindViewById<TextView>(Resource.Id.coordinate);
            startTime.Text = "Initializing";
        }


        /**
         * Check if the necessary permissions were granted before starting the application, in case the
         * permissions were not granted prompt he user to grant the permissions
         */
        private void CheckLocationPermission()
        {
            if (!UtilityClass.HasPermissions(this, UtilityClass.PERMISSIONS))
            {
                permissionGranted = false;
                ActivityCompat.RequestPermissions(this, UtilityClass.PERMISSIONS, UtilityClass.PERMISSION_ALL);
            }
            else
            {
                permissionGranted = true;
            }
        }


        /**
         * this is called after the ActivityCompat.requestPermissions() to get the result for the request permissions
         */

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            switch (requestCode)
            {
                case UtilityClass.PERMISSION_ALL:
                    if (grantResults.Length > 0
                            && grantResults[0] == (int)Android.Content.PM.Permission.Granted)
                    {
                        permissionGranted = true;
                    }
                    else
                    {
                        if (grantResults[0] != (int)Android.Content.PM.Permission.Granted)
                        {
                            if (!ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
                            {
                                Toast.MakeText(this, "The app was not allowed some permissions, some of the app functionality might not work", ToastLength.Long).Show();
                            }
                        }

                    }
                    break;
            }
        }

        public void OnReceiveResult(int resultCode, Bundle resultData)
        {
            if (resultCode == (int)Result.Ok)
            {
                serviceStartTime = resultData.GetString("cordinate").Split(':');
                coordinate.Text = System.String.Format("{0} Latitude:{0} \nLongitude{0}", coordinate.Text, serviceStartTime[0], serviceStartTime[1]);
            }
        }

        private void StartService()
        {
            if (permissionGranted)
            {
                //start Service
                if (UtilityClass.IsLocationEnabled(mContext))
                {
                    StartServiceAndBind();
                    stopService.Enabled = true;
                    stopService.Click += StopServiceListener;
                }
                else
                {
                    /*
                     * In case the user has his location service turned off we prompt the user
                     * to start the service
                     * */
                    Android.Support.V7.App.AlertDialog dialog = UtilityClass.ShowAlertDialog(this, mContext.Resources.GetString(Resource.String.turn_on_location), "OK", "Cancel");
                    Button btnPositive = dialog.GetButton((int)DialogButtonType.Positive);
                    btnPositive.Click += (s, e) =>
                    {
                        Intent myIntent = new Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                        StartActivity(myIntent);
                        dialog.Dismiss();
                    };
                    Button btnNegative = dialog.GetButton((int)DialogButtonType.Negative);
                    btnNegative.Click += (s, e) =>
                    {
                        dialog.Dismiss();
                    };
                }
            }
        }


        private void StopServiceListener(object sender, EventArgs e)
        {
            if (mBound)
            {
                Message msg = Message.Obtain(null, UtilityClass.STOP_SERVICE, 0, 0);
                try
                {
                    mService.Send(msg);
                }
                catch (RemoteException ex)
                {
                    ex.PrintStackTrace();
                }
                UnbindService(this);
                mBound = false;
                handler.RemoveCallbacks(helper);
                status.Text = "Not Running";
                stopService.Enabled = false;
                stopService.Click += null;
            }
        }

        private void StartServiceAndBind()
        {
            startTime.Text = "Initializing";
            AlarmManager mgr = (AlarmManager)GetSystemService(Context.AlarmService);
            Intent i = new Intent(this, typeof(OnAlarmReceiver));
            PendingIntent pi = PendingIntent.GetBroadcast(this, 0,
                    i, 0);

            mgr.SetRepeating(AlarmType.ElapsedRealtime,
                    SystemClock.ElapsedRealtime() + 60000,
                    300000,
                    pi);
            Intent serviceIntent = new Intent(mContext, typeof(LocationServiceHelper));
            serviceIntent.PutExtra("receiver", receiver);
            StartService(serviceIntent);
            if (!mBound)
                BindService(serviceIntent, this, Bind.AutoCreate);
        }

        protected override void OnResume()
        {
            base.OnResume();
            /*Check if the service is running on app resume and  bing to the service to get relevant data*/
            if (UtilityClass.IsMyServiceRunning(this, typeof(LocationServiceHelper).Name))
            {
                StartServiceAndBind();
                status.Text = "Running";
            }
            else
            {
                status.Text = "Not Running";
                stopService.Enabled = false;
                stopService.Click += null;
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (mBound)
            {
                handler.RemoveCallbacks(helper);
                UnbindService(this);
                mBound = false;
            }
        }


        /**
        * Defines callbacks for service binding, passed to bindService()
        */
        void IServiceConnection.OnServiceConnected(ComponentName name, IBinder service)
        {
            // We've bound to LocalService, cast the IBinder and get LocalService instance
            mService = new Messenger(service);
            mBound = true;
            status.Text = "Running";
        }

        void IServiceConnection.OnServiceDisconnected(ComponentName name)
        {
            mService = null;
            mBound = false;
        }








        private class RunnableHelper : Java.Lang.Object, Java.Lang.IRunnable
        {
            MainActivity splashActivity;
            Handler mHandler;

            public RunnableHelper(MainActivity splashActivity, Handler handler)
            {
                this.splashActivity = splashActivity;
                this.MHandler = handler;
            }

            public Handler MHandler { get => mHandler; set => mHandler = value; }

            public new void Dispose()
            {
               
            }

            public void Run()
            {
                if (splashActivity.mBound && splashActivity.serviceStartTime != 0L)
                {
                    long timeDelta = UtilityClass.GetUTC() - splashActivity.serviceStartTime;
                    splashActivity.startTime.Text = TimeUnit.Milliseconds.ToMinutes(timeDelta) + " Minutes";
                }
                else
                {
                    if (splashActivity.mBound)
                    {
                        // Create and send a message to the service, using a supported 'what' value
                        Message msg = Message.Obtain(null, UtilityClass.GET_START_TIME, 0, 0);
                        try
                        {
                            splashActivity.mService.Send(msg);
                        }
                        catch (RemoteException e)
                        {
                            e.PrintStackTrace();
                        }
                    }
                }
                MHandler.PostDelayed(this, 60000);
            }
        }
    }
}
       
