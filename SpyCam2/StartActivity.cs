using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.IO;
using System.Text.RegularExpressions;
using Android.Views.InputMethods;

namespace SpyCam2
{
    [Activity(Label = "SpyCam", MainLauncher = true, Icon = "@drawable/icon")]
    public class StartActivity : Activity
    {
       private List<string> sessions = new List<string>();
       private ArrayAdapter<String> adapter;
       private ListView listView;
       private string MainPath;
       private const int NEW_SESSION = 0;
       private const int RENAME_SESSION = 1;
       private Dictionary<string, string> Table;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SessionLayout);
            InitializeDirectory();            
            GetSessions();
            InitializeListView();           
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SaveTable();
        }
        private void SaveTable()
        {
            List<string> lines = new List<string>();
            foreach (var entry in Table){
                lines.Add(entry.Key);lines.Add(entry.Value);}
            System.IO.File.WriteAllLines(MainPath + "/settings.txt", lines);
        }

        private void LoadTable()
        {
            Table = new Dictionary<string, string>();
            string path=MainPath + "/settings.txt";
            if (!CheckExist(path)) return;
            using (var sr = new StreamReader(path))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    Table.Add(line, sr.ReadLine());
                }
                sr.Close();
            }
            
        }

        private bool CheckExist(string path)
        {
            var ex=File.Exists(path);
            if (ex == false) { File.Create(path).Dispose();  return false; }
            return true;
        }
        private void InitializeDirectory()
        {
            MainPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SpyCam";
            if (!System.IO.Directory.Exists(MainPath)) { Directory.CreateDirectory(MainPath); }
        }

        private void GetSessions()
        {
            LoadTable();
            foreach (var f in Table)  sessions.Add(f.Key);
        }

        private void InitializeListView()
        {
            listView = this.FindViewById<ListView>(Resource.Id.listView1);
            adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, sessions);
            listView.SetAdapter(adapter);
            listView.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => StartSession(e);
            listView.ItemLongClick += (s, arg) => ItemLongClick(arg);
        }

        private void ItemLongClick(AdapterView.ItemLongClickEventArgs arg)
        {
            PopupMenu menu = new PopupMenu(this, listView.GetChildAt(arg.Position - listView.FirstVisiblePosition));
            menu.Inflate(Resource.Menu.popup_menu);
            menu.MenuItemClick += (s1, arg1) =>
            {
                if (arg1.Item.ItemId == Resource.Id.itemDelete) DeleteSession(arg.Position);
                if (arg1.Item.ItemId == Resource.Id.itemRename) CallDialog(RENAME_SESSION, arg.Position);
            };
            menu.Show();
        }

        private void StartSession(AdapterView.ItemClickEventArgs e)
        {
            var MA = new Intent(this, typeof(MainActivity));
            MA.PutExtra("MainPath", MainPath + "/" + Table[sessions[e.Position]]);
            StartActivity(MA);
        }



        private void DeleteSession(int position)
        {
            DeleteDirectory(MainPath + "/" + Table[sessions[position]]);
            Table.Remove(sessions[position]);
            sessions.RemoveAt(position);
            UpdateAdapter();
        }

        private void RenameSession(int position, string newName)
        {
            if (!isValid(ref newName)) return;
            var k= Table[sessions[position]];
            Table.Remove(sessions[position]);
            Table.Add(newName, k);
            sessions[position] = newName;                  
            UpdateAdapter();
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (string dir in dirs)            
                DeleteDirectory(dir);
            
            Directory.Delete(target_dir, false);
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            CallDialog(NEW_SESSION,0);             
            return true;
        }

        private void CallDialog(int type, int pos)
        {          
            EditText input = NewEditText();
            AlertDialog.Builder builder = new AlertDialog.Builder(this).SetView(input).SetTitle("Enter name")
                .SetCancelable(true).SetNegativeButton("Cancel", delegate {  });
            switch(type)
            {
                case NEW_SESSION:
                    builder.SetPositiveButton("OK", delegate { CreateSession(input.Text.ToString());});break;
                case RENAME_SESSION:
                    builder.SetPositiveButton("OK", delegate { RenameSession(pos,input.Text.ToString());});break; 
            }
            builder.Show();
            input.RequestFocus();
            var imn = (InputMethodManager)GetSystemService(Context.InputMethodService);
            imn.ToggleSoftInput(InputMethodManager.ShowForced, 0);           
        }

        private void CreateSession(string str)
        {
            if (!isValid(ref str)) return;
            var path = Java.Lang.JavaSystem.CurrentTimeMillis().ToString();
            Table.Add(str, path);
            Directory.CreateDirectory(MainPath + "/" + path);
            sessions.Insert(0, str);
            UpdateAdapter();
        }

        private bool isValid(ref string path)
        {
            if (CheckSessionExist(path))
            { Toast.MakeText(this, "This session exist", ToastLength.Short).Show(); return false; }
            if (!CheckValid(ref path))
            { Toast.MakeText(this, "Invalid name", ToastLength.Short).Show(); return false; }
            return true;
        }
        private bool CheckValid(ref string name)
        {
            string invalid = "\n";
            name = name.Replace("\r", string.Empty).Replace("\n", string.Empty);
            if (name == "")            
                name = RandomName(name);              
            return true;
        }

        private string RandomName(string name)
        {
            name = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            int k = 1;
            string mainName = name;
            while (CheckSessionExist(name)) { name = mainName +"_"+ k.ToString(); k++; }            
            return name;
        }

        private bool CheckSessionExist(string name)
        {
            foreach (var n in sessions)
                if (n.Equals(name)) return true;
            return false;
        }

        private void UpdateAdapter()
        {
            adapter.Clear();
            adapter.AddAll(sessions);
        }

        private EditText NewEditText()
        {
            EditText input = new EditText(this);
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent,
                                                                         LinearLayout.LayoutParams.WrapContent);
            input.LayoutParameters = lp;
            return input;
        }
    }
}