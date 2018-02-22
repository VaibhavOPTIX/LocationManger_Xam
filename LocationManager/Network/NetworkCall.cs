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
using LocationManager.Model;

namespace LocationManager.Network
{
    public class NetworkCall
    {
        static HttpClient client = new HttpClient();


        static async Task<pushObject> SendCoordinateAsync(pushObject data)
        {
            HttpResponseMessage response = await client.PostAsync(
                "api/push", product);
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return response.Headers.Location;
        }
    }
}