using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using locationManager.model;
using Newtonsoft.Json;

namespace locationManager.network
{
    public class NetworkCall:Java.Lang.Object
    {
        static HttpClient client;

        public NetworkCall()
        {
            client = new HttpClient();
        }

        public async void SendCoordinateAsync(StringContent data)
        {
            HttpResponseMessage response = await client.PostAsync("http://test.ninestack.com/pushes/api/push", data);
            response.EnsureSuccessStatusCode();
            //if (response.IsSuccessStatusCode)
            //{

            //    var dataText = await response.Content.ReadAsStringAsync();
            //    return JsonConvert.DeserializeObject<pushObject>(dataText);
            //}
        }
    }
}