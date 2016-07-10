using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using SpyCam2;
using System;
using System.Collections.Generic;
public class ItemAdapter : BaseAdapter
{
    private List<string> items;
    private Activity context;

    public ItemAdapter(List<string> items, Activity context)
        : base()
    {
        this.items = items;
        this.context = context;
    }

    public override long GetItemId(int position)
    {
        return position;
    }

    public override Java.Lang.Object GetItem(int position)
    {
        if (items != null && position < items.Count && position >= 0) return items[position]; else return null;
    }

    public override int Count
    {
        get { if (items != null)return items.Count; else return 0; }
    }

    public void SetItems(List<string> items) 
    { 
        this.items = items; 
    }

    public override View GetView(int position, View convertView, ViewGroup parent)
    {           
        ViewHolder holder = null;
        if (convertView != null) holder = convertView.Tag as ViewHolder;
        if (holder == null) CreateNewHolder(parent, ref holder, ref convertView);
        FillHolder(items[position], holder);
        return convertView;
    }

    private void FillHolder(string item, ViewHolder holder)
    {
        var t = MainActivity.GetExtension(item);
        Bitmap bitmap = t.Equals(".mp4") ? SetVideoItem(holder, item) : SetPhotoItem(holder, item);
        if (bitmap != null) SetBitmapToView(holder, bitmap);
    }

    private void SetBitmapToView(ViewHolder holder, Bitmap bitmap)
    {
        DisplayMetrics displaymetrics = new DisplayMetrics();
        context.WindowManager.DefaultDisplay.GetMetrics(displaymetrics);
        holder.imageView.LayoutParameters.Height = bitmap.Height * displaymetrics.WidthPixels / bitmap.Width;
        holder.imageView.SetImageBitmap(bitmap);
    }

    private static Bitmap SetPhotoItem(ViewHolder holder, string item)
    {
        SetViewsVisibility(holder,true);
        BitmapFactory.Options options = new BitmapFactory.Options();
        options.InPreferredConfig = Bitmap.Config.Argb8888;
        Bitmap bitmap = BitmapFactory.DecodeFile(item, options);
        return bitmap;
    }

    private static void SetViewsVisibility(ViewHolder holder,bool isPhoto)
    {
        holder.imagePlay.Visibility = isPhoto ? ViewStates.Gone : ViewStates.Visible;      
    }

    private static Bitmap SetVideoItem(ViewHolder holder, string item)
    {
        SetViewsVisibility(holder, false);
        Bitmap bitmap = ThumbnailUtils.CreateVideoThumbnail(item, Android.Provider.ThumbnailKind.MiniKind);        
        return bitmap;
    }

    //private static string GetExtension(string item)
    //{
    //    var t = item.Substring(item.LastIndexOf('.'), item.Length - item.LastIndexOf('.'));
    //    return t;
    //}

    private void CreateNewHolder(ViewGroup parent, ref ViewHolder holder, ref View view)
    {
        holder = new ViewHolder();
        view = context.LayoutInflater.Inflate(Resource.Layout.PhotoItem, parent, false);
        holder.imageView = view.FindViewById<ImageView>(Resource.Id.imageView1);
        holder.imagePlay = view.FindViewById<ImageView>(Resource.Id.imagePlay);
        view.Tag = holder;
    }
}
class ViewHolder : Java.Lang.Object 
{
    public ImageView imageView{ get; set; }
    public ImageView imagePlay { get; set; }
}
    

