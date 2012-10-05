using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net;
using Windows.Data.Json;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Storage;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage.Streams;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace Xkcd_Reader.Data
{
    /// <summary>
    /// Base class for <see cref="SampleDataItem"/> and <see cref="SampleDataGroup"/> that
    /// defines properties common to both.
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public abstract class SampleDataCommon : Xkcd_Reader.Common.BindableBase
    {
        private static Uri _baseUri = new Uri("ms-appx:///");

        public SampleDataCommon(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this._uniqueId = uniqueId;
            this._title = title;
            this._subtitle = subtitle;
            this._description = description;
            this._imagePath = imagePath;
        }

        private string _uniqueId = string.Empty;
        public string UniqueId
        {
            get { return this._uniqueId; }
            set { this.SetProperty(ref this._uniqueId, value); }
        }

        private string _title = string.Empty;
        public string Title
        {
            get { return this._title; }
            set { this.SetProperty(ref this._title, value); }
        }

        private string _subtitle = string.Empty;
        public string Subtitle
        {
            get { return this._subtitle; }
            set { this.SetProperty(ref this._subtitle, value); }
        }

        private string _description = string.Empty;
        public string Description
        {
            get { return this._description; }
            set { this.SetProperty(ref this._description, value); }
        }

        //public for livetile access
        private ImageSource _image = null;
        //public for livetile access
        public String _imagePath = null;
        public ImageSource Image
        {
            get
            {
                if (this._image == null && this._imagePath != null)
                {
                    this._image = new BitmapImage(new Uri(SampleDataCommon._baseUri, this._imagePath));                    
                }
                return this._image;
            }

            set
            {
                this._imagePath = null;
                this.SetProperty(ref this._image, value);
            }
        }

        public void SetImage(String path)
        {
            this._image = null;
            this._imagePath = path;
            this.OnPropertyChanged("Image");
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class SampleDataItem : SampleDataCommon
    {
        public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, SampleDataGroup group, string osi)
            : base(uniqueId, title, subtitle, imagePath, description)
        {
            this._content = content;
            this._group = group;
            this._isFav = false;
            this._onserverimage = osi;
        }

        private string _content = string.Empty;
        public string Content
        {
            get { return this._content; }
            set { this.SetProperty(ref this._content, value); }
        }
        private string _onserverimage = string.Empty;
        public string OnServerImage
        {
            get { return this._onserverimage; }
            set { this.SetProperty(ref this._onserverimage, value); }
        }

        private SampleDataGroup _group;
        public SampleDataGroup Group
        {
            get { return this._group; }
            set { this.SetProperty(ref this._group, value); }
        }
        private bool _isFav;
        public bool isFav
        {
            get { return this._isFav; }
            set { this.SetProperty(ref this._isFav, value); }
        }
        
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class SampleDataGroup : SampleDataCommon 
    {
        public SampleDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
            : base(uniqueId, title, subtitle, imagePath, description)
        {
            //Items.CollectionChanged += ItemsCollectionChanged;            
        }


        public class comicItems : IncrementalLoadingCollection<SampleDataItem>
        {
            public async override Task<IEnumerable<SampleDataItem>> PullDataAsync(uint count)
            {
                //return await SampleDataSource.returnPreviousN(SampleDataSource.GetGroup("XKCD").Items.Last(), SampleDataSource.GetGroup("XKCD"), (int)count);
                var l = new List<SampleDataItem>();
                l.Add(await SampleDataSource.getComicByNum(int.Parse(SampleDataSource.GetGroup("XKCD").Items.Last().UniqueId) - 1, SampleDataSource.GetGroup("XKCD")));
                return l;
            }
            
            
        }
        
        private comicItems _items = new comicItems();
        public comicItems Items 
        {
            get { return this._items; }
        }
        public bool isLoading
        {
            get { return this._items.IsLoadingData; }
            
        }
        
        public SampleDataItem getIfExists(string num)
        {
            
            foreach (var x in Items)
            {
                if (x.UniqueId == num)
                    return x;
            }

            return null;
        }


        private ObservableCollection<SampleDataItem> _topItem = new ObservableCollection<SampleDataItem>();
        public ObservableCollection<SampleDataItem> TopItems
        {
            get {return this._topItem; }
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with hard-coded content.
    /// 
    /// SampleDataSource initializes with placeholder data rather than live production
    /// data so that sample data is provided at both design-time and run-time.
    /// </summary>
    public sealed class SampleDataSource
    {
        
        public static SampleDataSource _sampleDataSource; 

        private ObservableCollection<SampleDataGroup> _allGroups = new ObservableCollection<SampleDataGroup>();
        public ObservableCollection<SampleDataGroup> AllGroups
        {
            get { return this._allGroups; }
        }

        public static IEnumerable<SampleDataGroup> GetGroups(string uniqueId)
        {
            if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("Only 'AllGroups' is supported as a collection of groups");
            
            return _sampleDataSource.AllGroups;
        }

        public static SampleDataGroup GetGroup(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.AllGroups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static SampleDataItem GetItem(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.AllGroups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() >= 1) return matches.First();
             return null;
        }

        public SampleDataSource()
        {
            
                String ITEM_CONTENT = String.Format("Item Content: {0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}",
                            "Curabitur clas mus congue fermentum parturient fringilla euismod feugiat");

                var group1 = new SampleDataGroup("Group-1",
                        "Group Title: 1",
                        "Group Subtitle: 1",
                        "Assets/DarkGray.png",
                        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
                group1.Items.Add(new SampleDataItem("Group-1-Item-1",
                        "Item Title: 1",
                        "Item Subtitle: 1",
                        "Assets/LightGray.png",
                        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
                        ITEM_CONTENT,
                        group1, ""));
                group1.Items.Add(new SampleDataItem("Group-1-Item-2",
                        "Item Title: 2",
                        "Item Subtitle: 2",
                        "Assets/DarkGray.png",
                        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
                        ITEM_CONTENT,
                        group1, ""));
                group1.Items.Add(new SampleDataItem("Group-1-Item-3",
                        "Item Title: 3",
                        "Item Subtitle: 3",
                        "Assets/MediumGray.png",
                        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
                        ITEM_CONTENT,
                        group1, ""));
                group1.Items.Add(new SampleDataItem("Group-1-Item-4",
                        "Item Title: 4",
                        "Item Subtitle: 4",
                        "Assets/DarkGray.png",
                        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
                        ITEM_CONTENT,
                        group1, ""));

                group1.Items.Add(new SampleDataItem("Group-1-Item-5",
                        "Item Title: 5",
                        "Item Subtitle: 5",
                        "Assets/MediumGray.png",
                        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
                        ITEM_CONTENT,
                        group1, ""));
                group1.getIfExists("Group-1-Item-2").isFav = true;
                this.AllGroups.Add(group1);

            

        }

        public static async Task<JsonObject> GetAsync(string uri)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("user-agent", "XKCD Daily Win8 Reader");
            var content = await httpClient.GetStringAsync(uri);
            return await Task.Run(() => JsonObject.Parse(content));
        }
        public static SampleDataItem currentItem;
        //this also makes the group
        public static async Task addMostRecentComic()
        {
            string url = "http://xkcd.com/info.0.json";


            JsonObject json = await GetAsync(url);

            string month = json.GetNamedString("month");
            int num = (int)json.GetNamedNumber("num");
            string link = json.GetNamedString("link");
            string year = json.GetNamedString("year");
            string news = json.GetNamedString("news");
            string safe_title = json.GetNamedString("safe_title");
            string transcript = json.GetNamedString("transcript");
            string alt = json.GetNamedString("alt");
            string img = json.GetNamedString("img");
            string title = json.GetNamedString("title");
            string day = json.GetNamedString("day");

            //localize?
            string fulldate = month + "/" + day + "/" + year;

            SampleDataGroup Comics = new SampleDataGroup("XKCD", "XKCD", "A Webcomic of romace, sarcasm, math, and language", "310x150.png", "");

            SampleDataItem newcomic = new SampleDataItem(num.ToString(), title, fulldate, img, transcript, alt, Comics, img);

            if (FavoriteManager.isFavorite(num))
                newcomic.isFav = true;
            Comics.Items.Add(newcomic);
            //SampleDataSource._sampleDataSource._allGroups.Clear();
            SampleDataSource._sampleDataSource._allGroups.Add(Comics);



        }
         
            

        
        public static class FavoriteManager
        {
            private static Windows.Storage.StorageFolder roamingFolder;
            
            
            public static void createFavorites()
            {
                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                roamingFolder = Windows.Storage.ApplicationData.Current.RoamingFolder;
                SampleDataGroup Comics = new SampleDataGroup("Favorites", "Favorites", "Favorites", "", "");
                SampleDataSource._sampleDataSource._allGroups.Add(Comics);
            }
            public static async Task addFavsFromFileAsync()
            {
                //subredditlistfile = await roamingFolder.CreateFileAsync("subreddits.txt", Windows.Storage.CreationCollisionOption.OpenIfExists);
                var settings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                string favs = string.Empty;

                if (settings.Values.ContainsKey("favs"))
                {
                    //settings.Values["localSubreddits"] = "";
                    //settings.Values.Remove("localSubreddits");
                    favs = (string)settings.Values["favs"];
                }
                else
                {
                    settings.Values.Add("favs", "");
                }

                string[] srs = favs.Split(',');


                foreach (var item in srs)
                {
                    if (item != "404")
                        await addToGroupByNum(Int32.Parse(item), GetGroup("Favorites"));
                }

            }
            //TODO: call this in the right place, and make sure new items get added with isfave properly flagged
            public static void markFavs()
            {
                var group = GetGroup("XKCD");
                foreach (var x in GetGroup("Favorites").Items)
                {
                    var i = group.getIfExists(x.UniqueId.Trim());
                    if(i != null)
                        i.isFav = true;
                }
            }
            public static bool isFavorite(int num)
            {
                var settings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                string favs = (string)settings.Values["favs"];
                if (favs != null)
                {
                    string[] srs = favs.Split(',');
                    return srs.Contains(num.ToString() + " ");
                }
                else 
                    return false;

            }
            public static void WriteFavs()
            {
                List<string> favs = new List<string>();
                foreach (var x in SampleDataSource.GetGroup("Favorites").Items)
                {
                    favs.Add(x.UniqueId);
                }
                
                var settings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                settings.Values["favs"] = string.Join(",", favs);

            }
        }
        
        
        public static async Task checkAndAddNewComics()
        {
            string url = "http://xkcd.com/info.0.json";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("user-agent", "XKCD Daily Win8 Reader");
            HttpResponseMessage response = await client.GetAsync(url);
            string responseText = await response.Content.ReadAsStringAsync();
            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SampleDataGroup Comics = SampleDataSource.GetGroup("XKCD");

                    JsonObject json = JsonObject.Parse(responseText);


                    string month = json.GetNamedString("month");
                    int num = (int)json.GetNamedNumber("num");

                    if (Comics.Items[0].UniqueId != num.ToString())
                    {

                        string link = json.GetNamedString("link");
                        string year = json.GetNamedString("year");
                        string news = json.GetNamedString("news");
                        string safe_title = json.GetNamedString("safe_title");
                        string transcript = json.GetNamedString("transcript");
                        string alt = json.GetNamedString("alt");
                        string img = json.GetNamedString("img");
                        string title = json.GetNamedString("title");
                        string day = json.GetNamedString("day");

                        //localize?
                        string fulldate = month + "/" + day + "/" + year;



                        SampleDataItem newcomic = new SampleDataItem(num.ToString(), title, fulldate, img, transcript, alt, Comics, img);
                        if (FavoriteManager.isFavorite(num))
                            newcomic.isFav = true;
                        Comics.Items.Insert(0,newcomic);
                        //SampleDataSource._sampleDataSource._allGroups.Clear();
                        //SampleDataSource._sampleDataSource._allGroups.Add(Comics);
                    }
                }
                else if (response.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    var dia = new Windows.UI.Popups.MessageDialog("It appears xkcd.com is under heavy load. Please try again later.");
                    await dia.ShowAsync();
                }
                response.ToString();

            }
            catch (Exception)
            {
            }
            
        }

        public static async Task<IEnumerable<SampleDataItem>> returnPreviousN(SampleDataItem sdi, SampleDataGroup group, int n)
        {
            if (group.UniqueId != "Favorites")
            {
                List<SampleDataItem> l = new List<SampleDataItem>();
                int c = int.Parse(sdi.UniqueId);
                for (int x = 0; x < n; x++)
                {
                    if (c == 405) //lol randall monroe - doesn't have a comic #404
                        c--;
                    if (c > 1)
                        l.Add(await getComicByNum(--c, group));
                }
                return l;
            }
            else 
                return null;
        }

        public static async Task<SampleDataItem> getComicByNum(int number, SampleDataGroup group)
        {
            string url = "http://xkcd.com/" + number + "/info.0.json";

            JsonObject json = await GetAsync(url);

            string month = json.GetNamedString("month");
            int num = (int)json.GetNamedNumber("num");
            string link = json.GetNamedString("link");
            string year = json.GetNamedString("year");
            string news = json.GetNamedString("news");
            string safe_title = json.GetNamedString("safe_title");
            string transcript = json.GetNamedString("transcript");
            string alt = json.GetNamedString("alt");
            string img = json.GetNamedString("img");
            string title = json.GetNamedString("title");
            string day = json.GetNamedString("day");

            //localize?
            string fulldate = month + "/" + day + "/" + year;


            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var oldimg = img;

            if (!await Download(img, num))
                img = "ms-appdata:///local/" + num + ".png";
            string ns = num.ToString();
            SampleDataItem newcomic;
            if (group.Title == "Favorites")
            {
                //return null; //don't want to load more favorites??
                ns = ns + " ";
                newcomic = new SampleDataItem(ns, title, fulldate, img, transcript, alt, group, oldimg);
                newcomic.isFav = true;
            }
            else
            {
                newcomic = new SampleDataItem(ns, title, fulldate, img, transcript, alt, group, oldimg);
            }
            if (FavoriteManager.isFavorite(num))
            {
                newcomic.isFav = true;
            }

            return newcomic;
        }
        public static async Task addToGroupByNum(int number, SampleDataGroup group)
        {
            string url = "http://xkcd.com/" + number + "/info.0.json";

            JsonObject json = await GetAsync(url);

            string month = json.GetNamedString("month");
            int num = (int)json.GetNamedNumber("num");
            string link = json.GetNamedString("link");
            string year = json.GetNamedString("year");
            string news = json.GetNamedString("news");
            string safe_title = json.GetNamedString("safe_title");
            string transcript = json.GetNamedString("transcript");
            string alt = json.GetNamedString("alt");
            string img = json.GetNamedString("img");
            string title = json.GetNamedString("title");
            string day = json.GetNamedString("day");

            //localize?
            string fulldate = month + "/" + day + "/" + year;


            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var oldimg = img;
            if (!await Download(img, num))
                img = "ms-appdata:///local/" + num + ".png";
            string ns = num.ToString();
            SampleDataItem newcomic;
            if (group.Title == "Favorites")
            {
                ns = ns + " ";
                newcomic = new SampleDataItem(ns, title, fulldate, img, transcript, alt, group, oldimg);
                newcomic.isFav = true;
            }
            else
            {
                newcomic = new SampleDataItem(ns, title, fulldate, img, transcript, alt, group, oldimg);
            }
            if (FavoriteManager.isFavorite(num))
            {
                newcomic.isFav = true;
            }

            group.Items.Add(newcomic);
        }
                
    


        
        //a caching function. downloads and image if and returns whether it hasn't already been downloaded
        
        //TODO: Error handling including non-200 response header and lack of internet
        private static async Task<String> DownloadJSON(string url, int num)
        {
            var filename = num.ToString() + ".json";
            StorageFolder sf = Windows.Storage.ApplicationData.Current.LocalFolder;

            Task<StorageFile> task = GetFileIfExistsAsync(sf, filename);
            StorageFile file = await task;            
            if (file == null)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("user-agent", "XKCD Daily Win8 Reader");
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);


                var imageFile = await sf.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting); 
                var fs = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                var writer = new DataWriter(fs.GetOutputStreamAt(0));
                writer.WriteBytes(await response.Content.ReadAsByteArrayAsync());

                await writer.StoreAsync();
                writer.DetachStream();
                await fs.FlushAsync();

            }


            return await Windows.Storage.FileIO.ReadTextAsync(file);
        }
            
        
        private static async Task<bool> Download(string url, int num)
        {

            var client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                
                var filename = num.ToString() + ".png";

                StorageFolder sf = Windows.Storage.ApplicationData.Current.LocalFolder;

                Task<StorageFile> task = GetFileIfExistsAsync(sf, filename);
                StorageFile file = await task;

                if (file == null)
                {
                    var imageFile = await sf.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    var fs = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                    var writer = new DataWriter(fs.GetOutputStreamAt(0));
                    writer.WriteBytes(await response.Content.ReadAsByteArrayAsync());
                    await writer.StoreAsync();
                    writer.DetachStream();
                    await fs.FlushAsync();
                    return true;
                }
                else
                    return false;

            }
            return true;
            
        }
        
        public static async Task<StorageFile> GetFileIfExistsAsync(StorageFolder folder, string fileName)
        {
            try
            {
                return await folder.GetFileAsync(fileName);
            }
            catch
            {
                return null;
            }
        }

        public static async Task addPrevious20(SampleDataItem sdi, SampleDataGroup group)
        {
            int c = int.Parse(sdi.UniqueId);
            for (int x = 0; x < 20; x++)
            {
                await addToGroupByNum(--c, group); 
            }
        }
        public static async Task addPreviousN(SampleDataItem sdi, SampleDataGroup group, int n)
        {
            int c = int.Parse(sdi.UniqueId);                        
                for (int x = 0; x < n; x++)
                {
                    if (c == 405) //lol randall monroe - doesn't have a comic #404
                        c--;
                    if(c > 1)
                        await addToGroupByNum(--c, group);
                }            
        }
        public static async Task DownloadAllXKCD()
        {
            var g = SampleDataSource.GetGroup("XKCD");
            int n = int.Parse(g.Items.Last().UniqueId);
            
            await addPreviousN(g.Items.Last(), g, n - g.Items.Count());
        }



        internal static async Task fillGroup(SampleDataItem n, SampleDataItem o)
        {
            int newest = int.Parse(n.UniqueId);
            int oldest = int.Parse(o.UniqueId);
            int si = n.Group.Items.IndexOf(n) + 1;
            

            if (newest > oldest)
            {
                for (int i = oldest + 1; i < newest; i++)
                {
                    if (i == 404)
                        i++;
                    n.Group.Items.Insert(si, await getComicByNum(i, n.Group));
                }
            }
            else
            {
                //how did you get here what the fuck? newer has got to be greater than older
            }

            
        }
    }   
}

