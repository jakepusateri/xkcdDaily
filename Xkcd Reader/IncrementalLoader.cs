using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Xkcd_Reader
{
    
    /// <summary>
    /// Utility class for implementing incremental loading.  Just subclass, and implement
    /// PullDataAsync().  Don't forget to use the async keyword!
    /// </summary>
    /// <typeparam name="T">Type of item in collection</typeparam>
    public abstract class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading, INotifyPropertyChanged
    {
        private uint _currentPage = 0;
        private bool _hasMoreItems = true;
        private bool _isLoadingData = false;

        // Implement this method to do the actual data pulling (from a web service, database, file, etc.) and return results
        // Make sure you make the implementation async.  count is how many items are being requested by the ListViewBase control
        //
        public abstract Task<IEnumerable<T>> PullDataAsync(uint count);

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<IsLoadingChangedEventArgs> IsLoadingChanged;


        /// <summary>
        /// If you're using pagination, you can assign the starting page from the constructor
        /// </summary>
        /// <param name="startingPage">Starting page</param>
        public IncrementalLoadingCollection(uint startingPage = 0)
        {
            _currentPage = startingPage;
        }

        /// <summary>
        /// The page number (if pagination is being used) to pull data from.  Use this value from within
        /// PullDataAsync().
        /// </summary>
        public uint CurrentPage
        {
            get
            {
                return _currentPage;
            }
            set
            {
                _currentPage = value;
            }
        }

        /// <summary>
        /// Gets/sets whether the ListViewBase (e.g. GridView/ListView) should continue loading data from
        /// source
        /// </summary>
        public bool HasMoreItems
        {
            get
            {
                return _hasMoreItems;
            }
            set
            {
                _hasMoreItems = value;
            }
        }

        /// <summary>
        /// IsLoadingData is true when data is actually being pulled (usually over a network).
        /// Useful with progress bars/rings.
        /// </summary>
        public bool IsLoadingData
        {
            get
            {
                return _isLoadingData;
            }
            set
            {
                bool oldValue = _isLoadingData;
                bool newValue = value;
                _isLoadingData = newValue;

                if (oldValue != newValue)
                {
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("IsLoadingData"));
                    }

                    if (this.IsLoadingChanged != null)
                    {
                        IsLoadingChanged(this, new IsLoadingChangedEventArgs(oldValue, newValue));
                    }
                }
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return new IncrementalLoader<T>(this, count);
        }
    }

    public class IsLoadingChangedEventArgs : EventArgs
    {
        private bool _oldValue;
        private bool _newValue;

        public IsLoadingChangedEventArgs(bool oldValue, bool newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public bool OldValue
        {
            get
            {
                return _oldValue;
            }
        }

        public bool NewValue
        {
            get
            {
                return _newValue;
            }
        }
    }


    /// <summary>
    /// This class is responsible for retrieving data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IncrementalLoader<T> : IAsyncOperation<LoadMoreItemsResult>
    {
        private AsyncStatus _asyncStatus = AsyncStatus.Started;
        private LoadMoreItemsResult _results;

        public IncrementalLoader(IncrementalLoadingCollection<T> incrementalLoadingCollection, uint count)
        {
            PullDataIncrementalData(incrementalLoadingCollection, count);
        }

        public async void PullDataIncrementalData(IncrementalLoadingCollection<T> incrementalLoadingCollection, uint count)
        {
            try
            {
                incrementalLoadingCollection.IsLoadingData = true;
                IEnumerable<T> newItems = await incrementalLoadingCollection.PullDataAsync(count);

                if (newItems != null)
                {
                    if (newItems.Count() > 0)
                    {
                        foreach (T item in newItems)
                        {
                            incrementalLoadingCollection.Add(item);
                        }
                    }
                    else
                    {
                        incrementalLoadingCollection.HasMoreItems = false;
                    }
                }
                else
                {
                    throw new InvalidDataException();
                }

                // On success, increment page
                _asyncStatus = AsyncStatus.Completed;
                incrementalLoadingCollection.CurrentPage++;
            }
            catch
            {
                _results.Count = 0;
                _asyncStatus = AsyncStatus.Error;
                incrementalLoadingCollection.HasMoreItems = false;
            }

            incrementalLoadingCollection.IsLoadingData = false;
            if (Completed != null)
            {
                Completed(this, _asyncStatus);
            }
        }

        public AsyncOperationCompletedHandler<LoadMoreItemsResult> Completed { get; set; }

        public LoadMoreItemsResult GetResults()
        {
            return _results;
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
        }

        public Exception ErrorCode
        {
            get { throw new NotImplementedException(); }
        }

        public uint Id
        {
            get { throw new NotImplementedException(); }
        }

        public AsyncStatus Status
        {
            get { return _asyncStatus; }
        }
    }
}
