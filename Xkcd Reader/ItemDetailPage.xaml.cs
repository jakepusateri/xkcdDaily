using Xkcd_Reader.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using System.Net.Http;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace Xkcd_Reader
{
    /// <summary>
    /// A page that displays details for a single item within a group while allowing gestures to
    /// flip through other items belonging to the same group.
    /// </summary>
    public sealed partial class ItemDetailPage : Xkcd_Reader.Common.LayoutAwarePage
    {
        public ItemDetailPage()
        {
            this.InitializeComponent();
            RegisterForShare();
        }
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            SampleDataSource.currentItem = flipView.SelectedItem as SampleDataItem;
            base.GoBack(sender, e);
        }
        #region Share Handlers
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            UnregisterForShare();
            base.OnNavigatingFrom(e);
        }
        private void RegisterForShare()
        {
            try
            {
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(ShareTextHandler); ;
            }
            catch (InvalidOperationException e)
            {
                e.GetType();
            }
        }
        private void UnregisterForShare()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(ShareTextHandler);
        }

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            if (flipView.SelectedItem != null)
            {
                SampleDataItem sditem = flipView.SelectedItem as SampleDataItem;
                DataRequest request = e.Request;
                request.Data.Properties.Description = sditem.Title;
                request.Data.Properties.Title = "Share this comic:";
                request.Data.SetUri(new Uri("http://www.xkcd.com/"+sditem.UniqueId));
                //request.Data.SetText("Hello World!");
            }
        }
        #endregion

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // Allow saved page state to override the initial item to display
            if (pageState != null && pageState.ContainsKey("SelectedItem"))
            {
                navigationParameter = pageState["SelectedItem"];
            }

            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var item = SampleDataSource.GetItem((String)navigationParameter);
            this.DefaultViewModel["Group"] = item.Group;
            this.DefaultViewModel["Items"] = item.Group.Items;
            this.flipView.SelectedItem = item;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            var selectedItem = (SampleDataItem)this.flipView.SelectedItem;
            pageState["SelectedItem"] = selectedItem.UniqueId;
        }

        private async void selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var g = SampleDataSource.GetGroup("XKCD");
            //if((e.AddedItems[0] as SampleDataItem).UniqueId == g.Items[g.Items.Count - 2].UniqueId){
            //  await SampleDataSource.addPreviousN(g.Items.Last(), g, 1);


            //else if(g.Items.Count == 1)
            //  await SampleDataSource.addPreviousN(g.Items.Last(), g, 1);
            if (!g.isLoading)
            {
                
                if (e.AddedItems.Count != 0)
                {
                    var i = (e.AddedItems[0] as SampleDataItem);
                    if (i.Group.Title == "XKCD" && i.Equals(g.Items.Last()))
                        SampleDataSource.addPreviousN(g.Items.Last(), g, 1).Wait(300);
                }
            }
        }

        private async void Favorite_Button_Click(object sender, RoutedEventArgs e)
        {
            var i = (this.flipView.SelectedItem as SampleDataItem);
            var f = SampleDataSource.GetGroup("Favorites");
            i.isFav = true;           
            if(!SampleDataSource.FavoriteManager.isFavorite(int.Parse(i.UniqueId)))
                await SampleDataSource.addToGroupByNum(int.Parse(i.UniqueId),f);
            this.bottomAppBar.IsOpen = false;
            SampleDataSource.FavoriteManager.WriteFavs();
        }

        private async void Open_Button_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("http://xkcd.com/" + (this.flipView.SelectedItem as SampleDataItem).UniqueId));
        }

        private void UnFavorite_Button_Click(object sender, RoutedEventArgs e)
        {
            var i = (this.flipView.SelectedItem as SampleDataItem);
            var f = SampleDataSource.GetGroup("Favorites");
            i.isFav = false;
            f.Items.Remove(f.getIfExists((i.UniqueId + " ")));
            this.bottomAppBar.IsOpen = false;
            
            SampleDataSource.FavoriteManager.WriteFavs();
        }

        private void bottomAppBar_Opened_1(object sender, object e)
        {
            if ((this.flipView.SelectedItem as SampleDataItem).isFav)
            {
                Fav.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                unFav.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                unFav.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Fav.Visibility = Windows.UI.Xaml.Visibility.Visible;            
            }
            
        }

        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.pageTitle.Text != "XKCD")
            {
                this.Frame.GoBack();
                this.Frame.Navigate(typeof(GroupDetailPage), SampleDataSource.GetGroup("Favorites").Items[0].UniqueId);
            }
            else
            {
                this.Frame.GoBack();
            }
            
        }

        private async void Favorites_Button_Click(object sender, RoutedEventArgs e)
        {
           if (SampleDataSource.GetGroup("Favorites").Items.Count != 0)
            {
                if (this.pageTitle.Text != "Favorites")
                    this.Frame.GoBack();
                    this.Frame.Navigate(typeof(GroupDetailPage), SampleDataSource.GetGroup("Favorites").Items[0].UniqueId);
            }
            else
            {
                await new Windows.UI.Popups.MessageDialog("You haven't marked any favorites. Try selecting some comics and favoriting them to view in one place.").ShowAsync();
                
            }
        }

        private async void Save_Button_Click(object sender, RoutedEventArgs e)
        {
               var item = flipView.SelectedItem as SampleDataItem;
            //await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-appdata:///local/" + item.UniqueId));
            if (this.EnsureUnsnapped())
            {
             
                FileSavePicker savePicker = new FileSavePicker();
                
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("PNG Image", new List<string>() { ".png" });
                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = item.Title;



                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    //get file from server because yeah it's hard to copy a local file apparently
                    var client = new HttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, item.OnServerImage);
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                                                            
                    // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    
                    // copy file over
                    //await oldfile.CopyAsync(sf, file.Name, NameCollisionOption.ReplaceExisting);

                    
                    var fs = await file.OpenAsync(FileAccessMode.ReadWrite);
                    var writer = new DataWriter(fs.GetOutputStreamAt(0));
                    writer.WriteBytes(await response.Content.ReadAsByteArrayAsync());
                    await writer.StoreAsync();
                    writer.DetachStream();
                    await fs.FlushAsync();
                                        
                    // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                    // Completing updates may require Windows to ask for user input.
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status == FileUpdateStatus.Complete)
                    {
                        savedTextBox.Text = "File " + file.Name + " was saved.";
                    }
                    else
                    {
                        savedTextBox.Text = "File " + file.Name + " couldn't be saved.";
                    }
                }
                else
                {
                    savedTextBox.Text = "Operation cancelled.";
                }
                await Task.Delay(3000);
                savedTextBox.Text = "";
            }


        }
        internal bool EnsureUnsnapped()
        {
            // FilePicker APIs will not work if the application is in a snapped state.
            // If an app wants to show a FilePicker while snapped, it must attempt to unsnap first
            bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            if (!unsnapped)
            {
                //textblock.text = "Cannot unsnap the sample.";
            }

            return unsnapped;
        }

    }
}
