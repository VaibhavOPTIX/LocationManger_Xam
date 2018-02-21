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
using LocationManager.Utility;

namespace LocationManager.ServicePackage
{
    public class IncomingHandler: Handler
    {
        public override void HandleMessage(Message msg)
        {
            Bundle bundle = new Bundle();
            switch (msg.What)
            {
                case UtilityClass.GET_START_TIME:
                    bundle.PutLong("startTime", serviceStartTime);
                    receiver.send(Result.Ok, bundle);
                    break;
                case UtilityClass.STOP_SERVICE:
                    releaseResources();
                    break;
                default:
                    base.HandleMessage(msg);
            }
        }
    }
}