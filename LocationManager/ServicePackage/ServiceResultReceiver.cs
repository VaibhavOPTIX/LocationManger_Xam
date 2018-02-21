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
    public class ServiceResultReceiver : ResultReceiver
    {
        private IReceiver receiver;

        // Constructor takes a handler
        public ServiceResultReceiver(Handler handler):base(handler){ }

        // Setter for assigning the receiver
        public void SetReceiver(IReceiver receiver)
        {
            this.receiver = receiver;
        }

        // Delegate method which passes the result to the receiver if the receiver has been assigned
        protected override void OnReceiveResult(int resultCode, Bundle resultData)
        {
            if (receiver != null)
            {
                receiver.OnReceiveResult(resultCode, resultData);
            }
        }

        // Defines our event interface for communication
        public interface IReceiver
        {
            void OnReceiveResult(int resultCode, Bundle resultData);
        }

    }
}
