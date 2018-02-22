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

namespace LocationManager.ServicePackage
{
    [BroadcastReceiver(Enabled =true, Exported =false,Name = "LocationManager.ServicePackage.BootCompletedReceiver",Label = "BootCompletedReceiver")]
    [IntentFilter(new[] { Android.Content.Intent.ActionBootCompleted })]
    class BootCompletedReceiver : BroadcastReceiver
    {
        private const int PERIOD = 300000;  // 5 minutes
        public override void OnReceive(Context context, Intent intent)
        {
            AlarmManager mgr = (AlarmManager)context.GetSystemService(Context.AlarmService);
            Intent i = new Intent(context, typeof(OnAlarmReceiver));
            PendingIntent pi = PendingIntent.GetBroadcast(context, 0,
                    i, 0);

            mgr.SetRepeating(AlarmType.ElapsedRealtimeWakeup,
                    SystemClock.ElapsedRealtime() + 60000,
                    PERIOD,
                    pi);
            context.StartService(new Intent(context, typeof(LocationServiceHelper)));
        }
    }
}