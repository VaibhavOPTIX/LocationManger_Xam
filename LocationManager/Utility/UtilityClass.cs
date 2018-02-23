using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Util;
using locationManager.ServicePackage;

namespace locationManager.utility
{
    class UtilityClass
    {
        public const int PERMISSION_ALL = 1;
        public static String[] PERMISSIONS = { Manifest.Permission.AccessFineLocation };
        public const int GET_START_TIME = 1;
        public const int STOP_SERVICE = 2;


        /*Check if the necessary permissions were granted and return boolean */
        public static Boolean HasPermissions(Context context, String[] permissions)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && context != null && permissions != null)
            {
                foreach (String permission in permissions)
                {
                    if (Android.Support.V4.App.ActivityCompat.CheckSelfPermission(context, permission) != Permission.Granted)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /* Check if the phone is running  Nougat and above
         * This was to add GnssStatus.Callback instead of GpsStatus.Listener to get the Satellites
         * information as GpsStatus.Listener is deprecated
         * */
        public static Boolean IsBuildNougat()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                return true;
            }
            return false;
        }

        // to check if the Location setting is enabled or not
        public static Boolean IsLocationEnabled(Context context)
        {
            int locationMode = 0;
            String locationProviders;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                try
                {
                    locationMode = Android.Provider.Settings.Secure.GetInt(context.ContentResolver, Android.Provider.Settings.Secure.LocationMode);

                }
                catch (Android.Provider.Settings.SettingNotFoundException e)
                {
                    e.PrintStackTrace();
                    return false;
                }

                return !locationMode.Equals(Android.Provider.SecurityLocationMode.Off);

            }
            else
            {
                locationProviders = Android.Provider.Settings.Secure.GetString(context.ContentResolver, Android.Provider.Settings.Secure.LocationProvidersAllowed);
                return !TextUtils.IsEmpty(locationProviders);
            }
        }


        public static long GetUTC()
        {
            Calendar cal = Calendar.GetInstance(Java.Util.TimeZone.GetTimeZone("GMT"));
            return cal.TimeInMillis;
        }

        /*Check if the service is running on app resume and  bing to the service to get relevant data*/
        public static Boolean IsMyServiceRunning(Context mContext, string serviceClass)
        {
            ActivityManager manager = (ActivityManager)mContext.GetSystemService(Context.ActivityService);
            foreach (ActivityManager.RunningServiceInfo service in manager.GetRunningServices(Int32.MaxValue))
            {
                if (serviceClass.ToLower() == service.Service.ClassName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        public static Android.Support.V7.App.AlertDialog ShowAlertDialog(Context mContext, String message, String PositiveButton, String NegativeButton)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(mContext);
            builder.SetMessage(message);
            builder.SetCancelable(true);
            Android.Support.V7.App.AlertDialog alertDialog = builder.Create();
            alertDialog.Show();
            return alertDialog;
        }

        /**
         * This function writes to the file with the data passed
         *
         * @param context   : Context of the calling component/service
         * @param sFileName : the file name type indicating the file time being written to. values can
         *                  be "active","idle" or "health"
         * @param sBody     : the content that is to be written
         * @param startTime : the Service start time, that will be used to reference the other file of
         *                  the "sFileName" to query out the most recent file
         */
        public static bool GenerateNoteOnSD(Context context, String sFileName, String sBody, long startTime)
        {
            try
            {
                File root = new File(Android.OS.Environment.ExternalStorageDirectory, "Location_data");
                if (!root.Exists())
                {
                    root.Mkdir();
                }
                File gpxfile = new File(root, sFileName + "_" + startTime + ".txt");
                FileWriter writer = new FileWriter(gpxfile, true);
                writer.Append(sBody);
                writer.Flush();
                writer.Close();
                return true;
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
                return false;
            }
        }

        // schedule the start of the service every 10 - 30 seconds
        public static void scheduleJob(Context context)
        {
            ComponentName serviceComponent = new ComponentName(context, Java.Lang.Class.FromType(typeof(LocationServiceHelper)));
            JobInfo.Builder builder = new JobInfo.Builder(0, serviceComponent);
            builder.SetMinimumLatency(1 * 1000); // wait at least
            builder.SetOverrideDeadline(3 * 1000); // maximum delay
            JobScheduler jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            jobScheduler.Schedule(builder.Build());
        }
    }
}