using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Hardware;
using Android.Graphics;
using Android.Media;
using Java.IO;
using System.IO;
using Android.Util;
using System.Collections.Generic;



namespace SpyCam2
{
    [Activity(Label = "MainActivity",Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity, ISurfaceHolderCallback, Android.Hardware.Camera.IPictureCallback
    {
        private static Android.Hardware.Camera camera;
        private MediaRecorder recorder;
        private ISurfaceHolder surfaceHolder;
        private bool previewing = false;
        private bool recording = false;    
        private string recordPath;
        private int PhotoCount = 0;
        private List<string> Items = new List<string>();
        private ItemAdapter adapter;
        private SurfaceView surfaceView;
        private ListView listView;
        private long SwipeItemId;
        private int SwipeXOld;
        private int TouchPositionX, TouchPositionY;
        static public string MainPath; 
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
            MainPath = Intent.GetStringExtra("MainPath") ?? "";
            TotalInitialize();           
        }

        private void TotalInitialize()
        {
            InitializeSurfaceHolder();
            FindFiles();
            InitializeListView();
            AddLayoutEvent(FindViewById<FrameLayout>(Resource.Id.layout));
            AddListViewEvents();
        }

        private void InitializeListView()
        {
            listView = FindViewById<ListView>(Resource.Id.MainListView);
            adapter = new ItemAdapter(Items, this);
            listView.Adapter = adapter;
        }

        private void InitializeSurfaceHolder()
        {
            surfaceView = FindViewById<SurfaceView>(Resource.Id.surfaceView1);
            surfaceHolder = surfaceView.Holder;
            surfaceHolder.AddCallback(this);
            surfaceHolder.SetType(SurfaceType.PushBuffers);            
        }

        private void RestartPreview()
        {
            StopPreview();
            StartPreview();
        }
        private void AddLayoutEvent(FrameLayout layout)
        {
            layout.Click += delegate { TakePhoto(); };
            layout.LongClick += delegate { Record(); };
            layout.Touch += layout_Touch;
        }

        private void AddListViewEvents()
        {
            listView.ItemClick += listView_ItemClick;
            listView.Touch += listView_Touch;
            listView.ItemLongClick += delegate { Record(); };
        }

        void layout_Touch(object sender, View.TouchEventArgs e)
        {            
            if (e.Event.Action==MotionEventActions.Up&&recording)                    
                StopRecord();                       
            e.Handled = false;
        }

        private void Record()
        {
            MakeToast("Record");
            if (recording)
                StopRecord();
            else
                StartRecord();
        }

        private void StartRecord()
        {
            if (PrepareVideoRecorder())            
                {recorder.Start();
                 recording = true;}
            else ReleaseMediaRecorder();
        }

        private void MakeToast(string str)
        {
            Toast.MakeText(this, str, ToastLength.Short).Show();
        }

        void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ImageView view=e.View.FindViewById<ImageView>(Resource.Id.imagePlay);                       
            var Coordinate = GetViewFirstPoint(view);
            if ((TouchPositionX > Coordinate[0] && TouchPositionX < (Coordinate[0] + view.Width)) &&
            (TouchPositionY > Coordinate[1] && TouchPositionY < (Coordinate[1] + view.Height)))
                StartVideoActivity(e.Position); else TakePhoto();   
        }

        private void StartVideoActivity(int i)
        {
            if (!GetExtension(Items[i]).Equals(".mp4")) return;
            var VP = new Intent(this, typeof(VideoPlayer));
            VP.PutExtra("src", Items[i]);
            StartActivity(VP);
        }

        private static int[] GetViewFirstPoint(ImageView view)
        {            
            int[] location = new int[2];
            view.GetLocationOnScreen(location);
            return location;            
        }

        void listView_Touch(object sender, View.TouchEventArgs e)
        {
            TouchPositionX = (int)e.Event.RawX;
            TouchPositionY = (int)e.Event.RawY;
            if (e.Event.Action == MotionEventActions.Down) ItemTouchDown((int)e.Event.GetX(), (int)e.Event.GetY());
            else if (e.Event.Action == MotionEventActions.Up) ItemTouchUp((int)e.Event.GetX(), (int)e.Event.GetY());           
            e.Handled = false;
        }

        private void ItemTouchUp(int X, int Y)
        {
            if (recording)
                StopRecord();
            else            
                CheckSwipe(X,Y);            
        }

        private void CheckSwipe(int X, int Y)
        {
            DisplayMetrics displaymetrics = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetMetrics(displaymetrics);
            var ItemId = listView.GetItemIdAtPosition(listView.PointToPosition(X, Y));
            if (ItemId == SwipeItemId && (SwipeXOld - X > displaymetrics.WidthPixels / 2)) 
                DeleteItem((int)SwipeItemId);
        }

        private void ItemTouchDown(int X, int Y)
        {
            SwipeXOld =X;
            SwipeItemId = listView.GetItemIdAtPosition(listView.PointToPosition(X,Y));
        }

        private bool PrepareVideoRecorder()
        {            
            camera = GetCameraInstance();           
            camera.Unlock();
            recorder = CreateMediaRecorder();
            try { recorder.Prepare(); return true; }
            catch { ReleaseMediaRecorder(); return false; }
        }

        private MediaRecorder CreateMediaRecorder()
        {
            recorder = new MediaRecorder();
            recorder.SetCamera(camera);
            recorder.SetAudioSource(AudioSource.Camcorder);
            recorder.SetVideoSource(VideoSource.Camera);
            if(!isLandscape())recorder.SetOrientationHint(90);
            var cameraProfile = CamcorderProfile.Get(0, CamcorderQuality.High);
            recorder.SetProfile(cameraProfile);
            recordPath = System.IO.Path.Combine(MainPath, Java.Lang.JavaSystem.CurrentTimeMillis().ToString() + ".mp4");
            recorder.SetOutputFile(recordPath);
            recorder.SetPreviewDisplay(surfaceHolder.Surface);
            return recorder;
        }

        void DeleteItem(int id)
        {
            System.IO.File.Delete(Items[id]);
            Items.RemoveAt(id);            
            adapter.NotifyDataSetChanged();
        }

        private void StopRecord()
        {
            recording = false;            
            ReleaseMediaRecorder();
            if (!System.IO.File.Exists(recordPath)) { MakeToast("Some shit happend"); return; }
            MakeToast("Record taked");                
            AddNewItem(recordPath);
        }

        private void AddNewItem(string path)
        {
            Items.Insert(0, path);
            adapter.NotifyDataSetChanged();
        }

        public  Android.Hardware.Camera GetCameraInstance()
        {
            if (camera != null) return camera;
            Android.Hardware.Camera cam;
            try { cam = Android.Hardware.Camera.Open(); }// setCameraDisplayOrientation(cam);}
            catch (Java.Lang.Exception e) { e.PrintStackTrace(); MakeToast("Camera open error!"); return null; }
            return cam;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseMediaRecorder();
            ReleaseCamera();
        }

        private void ReleaseCamera()
        {
            if (camera == null) return;
            camera.Release(); // release the camera for other applications, VERY important!!!
            camera.Dispose();
            camera = null;
        }

        private void ReleaseMediaRecorder()
        {
            if (recorder == null) return;
            recorder.Stop();
            recorder.Reset();
            recorder.Release();
            recorder.Dispose();
            recorder = null;
            camera.Lock();
        }

        public void TakePhoto()
        {
            PhotoCount++;
            if (PhotoCount != 1) { MakeToast(PhotoCount.ToString()); return; }
            camera = GetCameraInstance();
            while (PhotoCount > 0)
            {
                PhotoCount--;       
                try { camera.TakePicture(null, null, this);}
                catch (Exception e) { RestartPreview(); PhotoCount++;}
            }  
        }

        void FindFiles()
        {
            if (!System.IO.Directory.Exists(MainPath)) { Directory.CreateDirectory(MainPath); return; }
            FileInfo[] sortedFiles = new DirectoryInfo(MainPath).GetFiles();
            Array.Sort(sortedFiles, delegate(FileInfo f1, FileInfo f2)
            {return f2.CreationTime.CompareTo(f1.CreationTime);});
            foreach (FileInfo f in sortedFiles)
                if (f.Extension.Equals(".png") || f.Extension.Equals(".mp4")) Items.Add(f.FullName);
        }

        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {            
            if (previewing)            
                StopPreview();    
            if (camera != null)           
                StartPreview();           
        }

        private void StartPreview()
        {            
            try
            {
                camera.SetPreviewDisplay(surfaceHolder);
                camera.StartPreview();
                previewing = true;
            }
            catch (Java.IO.IOException e){  e.PrintStackTrace();  }
        }

        private void StopPreview()
        {
            camera.StopPreview();
            previewing = false;
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            camera = GetCameraInstance();           
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {            
            ReleaseCamera();
            ReleaseMediaRecorder();            
            previewing = false;
        }

        public void OnPictureTaken(byte[] data, Android.Hardware.Camera camera)
        {
            Bitmap bitmapPicture = CreateBitmap(data);
            bitmapPicture = RotateBitmap(bitmapPicture);
            var filePath = SaveBitmap(bitmapPicture);
            if (!System.IO.File.Exists(filePath)) { MakeToast("Some shit happend"); return; }
            MakeToast("Photo taked");
            AddNewItem(filePath);
           // RestartPreview();
        }

        private  Bitmap RotateBitmap(Bitmap bitmapOrg)
        {
            if (isLandscape()) return bitmapOrg;
            Matrix matrix = new Matrix();
            matrix.PostRotate(90f);  
            return Bitmap.CreateBitmap(bitmapOrg, 0, 0, bitmapOrg.Width, bitmapOrg.Height,matrix,true);
        }

        private static bool isLandscape()
        {
            var windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var rotation = windowManager.DefaultDisplay.Rotation;
            bool isLandscape = rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270;
            return isLandscape;
        }

        private static string SaveBitmap(Bitmap bitmapPicture)
        {
            var filePath = System.IO.Path.Combine(MainPath, Java.Lang.JavaSystem.CurrentTimeMillis().ToString() + ".png");
            var stream = new FileStream(filePath, FileMode.Create);
            bitmapPicture.Compress(Bitmap.CompressFormat.Png, 100, stream);
            stream.Close();
            return filePath;
        }

        public static string GetExtension(string item)
        {
            var t = item.Substring(item.LastIndexOf('.'), item.Length - item.LastIndexOf('.'));
            return t;
        }

        private static Bitmap CreateBitmap(byte[] data)
        {
            Bitmap bitmapPicture = BitmapFactory.DecodeByteArray(data, 0, data.Length);
            bitmapPicture = Bitmap.CreateScaledBitmap(bitmapPicture, bitmapPicture.Width / 5, bitmapPicture.Height / 5, false);
            return bitmapPicture;
        }

    }
}

