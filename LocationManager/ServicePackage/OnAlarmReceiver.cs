using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using locationManager.ServicePackage;

namespace locationManager.servicePackage
{
    public class OnAlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            LocationServiceHelper.EnqueueWork(context, intent);
        }
    }
}