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
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Notifications;
using Windows.ApplicationModel.Background;
using Xkcd_Reader.Common;
using BackgroundTask;
using System.Threading.Tasks;

// The Group Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234229

namespace Xkcd_Reader
{
    /// <summary>
    /// A page that displays an overview of a single group, including a preview of the items
    /// within the group.
    /// </summary>
    public sealed partial class GroupDetailPage : Xkcd_Reader.Common.LayoutAwarePage
    {
        public GroupDetailPage()
        {
            this.InitializeComponent();
            RegisterForShare();
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
                            
                DataRequest request = e.Request;
                request.Data.Properties.Description = "XKCD";
                request.Data.Properties.Title = "Share this comic:";
                request.Data.SetUri(new Uri("http://www.xkcd.com/"));
            
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
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            //if (pageState == null)
            //this.Frame.Navigate(typeof(ItemDetailPage),  SampleDataSource.GetGroup("XKCD").Items.First().UniqueId);


            
                var item = SampleDataSource.GetItem((String)navigationParameter);
                SampleDataGroup group = null;
                if (item != null)
                    group = item.Group;





                if (group != null && group.Items != null)
                {
                    this.DefaultViewModel["Group"] = group;
                    if (group.UniqueId != "Favorites")
                        this.DefaultViewModel["Items"] = group.Items;
                    else
                        this.DefaultViewModel["Items"] = group.Items.OrderByDescending(s => s.UniqueId);
                }
                else
                {
                    await SampleDataSource.addMostRecentComic();
                    this.DefaultViewModel["Group"] = group;
                    this.DefaultViewModel["Items"] = group.Items;
                }

                if (group.UniqueId == "Favorites")
                {
                    this.errorBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    this.gotobutton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    this.Numberbox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    this.errorBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    this.gotobutton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    this.Numberbox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }

            
            
        }
 


        

        /// <summary>
        /// Invoked when an item is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(ItemDetailPage), itemId);
        }

        //private async void Button_Tapped_1(object sender, TappedRoutedEventArgs e)
        //{
        //    this.moreButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //    var g = SampleDataSource.GetGroup("XKCD"); 
        //    //if((itemGridView.DataContext as SampleDataGroup).Title == g.Title)
        //        await SampleDataSource.addPreviousN(g.Items.Last(), g, 30);
        //    this.moreButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //    //this.itemGridView.ScrollIntoView(g.Items.Last());
        //}

        #region bunch of confusing shit
        private void PushtoLive_Tapped_1(object sender, TappedRoutedEventArgs e)
        {

            // create a string with the tile template xml
          

        
            string tileXmlString = "<tile>"
                            + "<visual>"
                            + "<binding template='TileWideImageAndText01'>"
                            + "<text id='1'>" + Data.SampleDataSource.GetGroup("XKCD").Items[0].Title + "</text> "
                            + "<image id='1' src='" + Data.SampleDataSource.GetGroup("XKCD").Items[0]._imagePath + "' alt='Web image'/>"
                            + "</binding>"
                            + "<binding template='TileSquareImage'>"
                            + "<image id='1' src='" + Data.SampleDataSource.GetGroup("XKCD").Items[0]._imagePath + "' alt='Web image'/>"
                            + "</binding>"
                            + "</visual>"
                            + "</tile>";
            // create a DOM
            Windows.Data.Xml.Dom.XmlDocument tileDOM = new Windows.Data.Xml.Dom.XmlDocument();
            try
            {
                // load the xml string into the DOM, catching any invalid xml characters 
                tileDOM.LoadXml(tileXmlString);

                // create a tile notification
                TileNotification tile = new TileNotification(tileDOM);

                // send the notification to the app's application tile
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);

                
            }
            catch (Exception)
            {
                
            }

        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {                                
                    AttachProgressAndCompletedHandlers(task.Value);
                    BackgroundTaskSample.UpdateBackgroundTaskStatus(BackgroundTaskSample.TimeTriggeredTaskName, true);                                
            }
            
            base.OnNavigatedTo(e);
            
        }

        /// <summary>
        /// Register a TimeTriggeredTask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RegisterBackgroundTask(object sender, RoutedEventArgs e)
        {
            //
            // Time triggered tasks can only run when the application is on the lock screen.
            // Time triggered tasks can be registered even if the application is not on the lockscreen.
            // 
            await BackgroundExecutionManager.RequestAccessAsync();

            var task = BackgroundTaskSample.RegisterBackgroundTask(BackgroundTaskSample.SampleBackgroundTaskEntryPoint,
                                                                   BackgroundTaskSample.TimeTriggeredTaskName,
                                                                   new TimeTrigger(30, false),
                                                                   null);
            AttachProgressAndCompletedHandlers(task);
            
        }

        /// <summary>
        /// Unregister a TimeTriggeredTask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnregisterBackgroundTask(object sender, RoutedEventArgs e)
        {
            BackgroundTaskSample.UnregisterBackgroundTasks(BackgroundTaskSample.TimeTriggeredTaskName);
            
        }

        /// <summary>
        /// Attach progress and completed handers to a background task.
        /// </summary>
        /// <param name="task">The task to attach progress and completed handlers to.</param>
        private void AttachProgressAndCompletedHandlers(IBackgroundTaskRegistration task)
        {
            task.Progress += new BackgroundTaskProgressEventHandler(OnProgress);
            task.Completed += new BackgroundTaskCompletedEventHandler(OnCompleted);
        }

        /// <summary>
        /// Handle background task progress.
        /// </summary>
        /// <param name="task">The task that is reporting progress.</param>
        /// <param name="e">Arguments of the progress report.</param>
        private void OnProgress(IBackgroundTaskRegistration task, BackgroundTaskProgressEventArgs args)
        {
            var progress = "Progress: " + args.Progress + "%";
            BackgroundTaskSample.TimeTriggeredTaskProgress = progress;
            
        }

        /// <summary>
        /// Handle background task completion.
        /// </summary>
        /// <param name="task">The task that is reporting completion.</param>
        /// <param name="e">Arguments of the completion report.</param>
        private void OnCompleted(IBackgroundTaskRegistration task, BackgroundTaskCompletedEventArgs args)
        {
            PushtoLive_Tapped_1(null, null);
        }

        public async Task<bool> ObtainLockScreenAccess()
        {
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

            if (status == BackgroundAccessStatus.Denied || status == BackgroundAccessStatus.Unspecified)
            {
                return false;
            }

            return true;
        }
#endregion
        private async void Favorites_Button_Click(object sender, RoutedEventArgs e)
        {
            //TODO: sort favs before sending
            
            //SampleDataSource.FavoriteManager.markFavs();
            if (SampleDataSource.GetGroup("Favorites").Items.Count != 0)
            {
                if (this.pageSubtitle.Text != "Favorites")
                    this.Frame.Navigate(typeof(GroupDetailPage), SampleDataSource.GetGroup("Favorites").Items[0].UniqueId);
            }
            else
            {
                await new Windows.UI.Popups.MessageDialog("You haven't marked any favorites. Try selecting some comics and favoriting them to view in one place.").ShowAsync();
                
            }
        }

        private async void Refresh_Button_Click(object sender, RoutedEventArgs e)
        {
            await SampleDataSource.checkAndAddNewComics();
        }

        private void scrollToCurrent(object sender, RoutedEventArgs e)
        {
            if (SampleDataSource.currentItem != null)
            {
                (sender as ListViewBase).ScrollIntoView(SampleDataSource.currentItem);
                (sender as ListViewBase).SelectedItem = SampleDataSource.currentItem;
                SampleDataSource.currentItem = null;
            }
        }

        private void viewSelectChanges(object sender, SelectionChangedEventArgs e)
        {
            this.bottomAppBar.IsOpen = true;
            if ((sender as ListViewBase).SelectedItems.Count > 0)
            {
                Itemops.Visibility = Windows.UI.Xaml.Visibility.Visible;
                Normalops.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                Itemops.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Normalops.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void ClearSeclection_Button_Click(object sender, RoutedEventArgs e)
        {
            this.itemGridView.SelectedItems.Clear();
            this.itemListView.SelectedItems.Clear();
            this.bottomAppBar.IsOpen = false;
        }

        private async void Favorite_Button_Click(object sender, RoutedEventArgs e)
        {
            var y = this.itemGridView.SelectedItems;
            if (this.itemGridView.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                y = this.itemListView.SelectedItems;
            }

            foreach (var x in y)
            {
                SampleDataItem i = x as SampleDataItem;
                var f = SampleDataSource.GetGroup("Favorites");
                i.isFav = true;
                if (!SampleDataSource.FavoriteManager.isFavorite(int.Parse(i.UniqueId)))
                    await SampleDataSource.addToGroupByNum(int.Parse(i.UniqueId), f);

                SampleDataSource.FavoriteManager.WriteFavs();
            }

            
            SampleDataSource.FavoriteManager.WriteFavs();
        }

        private void UnFavorite_Button_Click(object sender, RoutedEventArgs e)
        {
            var y = this.itemGridView.SelectedItems;
            if (this.itemGridView.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                y = this.itemListView.SelectedItems;
            }
            
            foreach (var x in y)
            {
                SampleDataItem i = x as SampleDataItem;
                var f = SampleDataSource.GetGroup("Favorites");
                i.isFav = false;

                f.Items.Remove(f.getIfExists((i.UniqueId+" ")));

            }
            
            
            
            SampleDataSource.FavoriteManager.WriteFavs();
        }

        private void SelectAllSeclection_Button_Click(object sender, RoutedEventArgs e)
        {
            this.itemGridView.SelectAll();
            this.itemListView.SelectAll();
        }

        private async void Goto_Button_Click(object sender, RoutedEventArgs e)
        {
            //add item if needed            
            var g = SampleDataSource.GetGroup("XKCD");
            var l = g.Items.Last();
            var q = g.getIfExists(Numberbox.Text.Trim());
            if (q == null)
            {
                int num;
                if (int.TryParse(Numberbox.Text, out num))
                {
                    if (num < int.Parse(g.Items[0].UniqueId) && num >= 1 && num != 404)
                    {
                        await SampleDataSource.addToGroupByNum(num, g);
                        //goto item                
                        
                        this.Frame.Navigate(typeof(ItemDetailPage), num.ToString());
                        await SampleDataSource.fillGroup(l, g.Items.Last());
                        
                    }
                    else
                    {
                        if (num == 404)
                            this.errorBox.Text = "Error. Comic #404 doesn't exist.";
                        else if (num < 1)
                            this.errorBox.Text = "There aren't actually any negatively numbered comics. Try entering a positive number.";
                        else
                            this.errorBox.Text = "That comic doesn't exist (yet). You might want to wait until it is published";
                                                
                    }
                }
                else
                {
                    this.errorBox.Text = "You might want to try entering a number, rather than something non-numeric.";
                }
            }
            else
            {
                this.Frame.Navigate(typeof(ItemDetailPage), q.UniqueId);
            }
            
            
            Numberbox.Text = "";

            
        }

        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {

            if (this.pageSubtitle.Text != "A Webcomic of romace, sarcasm, math, and language")
                //this.Frame.Navigate(typeof(GroupDetailPage), SampleDataSource.GetGroup("XKCD").Items[0].UniqueId);
                this.Frame.GoBack();
            this.topAppBar.IsOpen = false;
            this.bottomAppBar.IsOpen = false;
        }

        private void submitCheck(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.Goto_Button_Click(null, null);
            }
        }

    }
}
