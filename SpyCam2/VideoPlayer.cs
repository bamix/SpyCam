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

namespace SpyCam2
{
    [Activity(Label = "VideoPlayer",Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen")]
    public class VideoPlayer : Activity
    {        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.VideoLayout);            
            var vv = StartVideo(Intent.GetStringExtra("src") ?? "");
            vv.Completion += delegate { Finish(); };
        }

        private VideoView StartVideo(string src)
        {
            var vv = FindViewById<VideoView>(Resource.Id.videoView1);
            vv.SetVideoPath(src);
            vv.SetMediaController(new MediaController(this));
            vv.RequestFocus();
            vv.Start();
            return vv;
        }
    }
}