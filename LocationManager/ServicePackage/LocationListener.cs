using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocationManager.ServicePackage
{
    public class LocationListener : Android.Locations.ILocationListener
    {
        IntPtr IJavaObject.Handle => throw new NotImplementedException();

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        void ILocationListener.OnLocationChanged(Location location)
        {
            throw new NotImplementedException();
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