using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using Java.Util;

namespace LocationManager.Utility
{
    class UtilityClass
    {
        public const int PERMISSION_ALL = 1;
        public static String[] PERMISSIONS = {Manifest.Permission.AccessFineLocation};
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
                if (serviceClass.Equals(service.Service.ClassName))
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


    }
}