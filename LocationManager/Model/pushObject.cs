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

namespace LocationManager.Model
{
    public class pushItem
    {
        public int localid { get; set; }
        public decimal lat { get; set; }
        public decimal lan { get; set; }
        public string timestamp { get; set; }
    }
    public class pushObject
    {
        public int id { get; set; }
        public List<pushItem> items { get; set; }
    }
}