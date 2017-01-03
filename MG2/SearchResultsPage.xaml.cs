using MG2.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MG2.Data;

// TODO: Connect the Search Results Page to your in-app search.
// The Search Results Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234240

namespace MG2
{
    /// <summary>
    /// This page displays search results when a global search is directed to this application.
    /// </summary>
    public sealed partial class SearchResultsPage : Page
    {
        public Dictionary<String, IEnumerable<SampleDataItem>>
        AllResultsCollection { get; set; }
        
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public SearchResultsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var queryText = e.NavigationParameter as String;

            // TODO: Application-specific searching logic.  The search process is responsible for
            //       creating a list of user-selectable result categories:
            //
            //       filterList.Add(new Filter("<filter name>", <result count>));
            //
            //       Only the first filter, typically "All", should pass true as a third argument in
            //       order to start in an active state.  Results for the active filter are provided
            //       in Filter_SelectionChanged below.

            // Initialise the results list
            AllResultsCollection = new Dictionary<string, IEnumerable<SampleDataItem>>();

            // Keep track of the number of matching items.
            var totalMatchingItems = 0;

            var filterList = new List<Filter>();

            var groups = await SampleDataSource.GetGroupsAsync();

            foreach (var group in groups)
            {
                var matchingItems = group.Items.Where(item => item.Title.IndexOf(queryText,
                    StringComparison.CurrentCultureIgnoreCase) > -1);
                int numberOfMatchingItems = matchingItems.Count();
                totalMatchingItems += numberOfMatchingItems;
                if (numberOfMatchingItems > 0)
                {
                    AllResultsCollection.Add(group.Title, matchingItems);
                    filterList.Add(new Filter(group.Title, numberOfMatchingItems));
                }

            }

            // Create an entry for "All" for viewing all search results.

            filterList.Add(new Filter("All", 0, true));
            filterList.Add(new Filter("Title", 0));
            //filterList.Add(new Filter("Description", 0));

            // Communicate results through the view model
            this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';
            this.DefaultViewModel["Filters"] = filterList;
            this.DefaultViewModel["ShowFilters"] = filterList.Count > 1;
        }

        /// <summary>
        /// Invoked when a filter is selected using a RadioButton when not snapped.
        /// </summary>
        /// <param name="sender">The selected RadioButton instance.</param>
        /// <param name="e">Event data describing how the RadioButton was selected.</param>
        void Filter_Checked(object sender, RoutedEventArgs e)
        {
            // Retrive the data context of the sender (the selected radio button)
            // This gives us the selected Filter object

            var filter = (sender as FrameworkElement).DataContext as Filter;

            // Mirror the change into the CollectionViewSource.
            // This is most likely not needed.
            if (filtersViewSource.View != null)
            {
                filtersViewSource.View.MoveCurrentTo(filter);
            }

            // Determine what filter was selected
            
            if (filter != null)
            {
                // Mirror the results into the corresponding Filter object to allow the
                // RadioButton representation used when not snapped to reflect the change
                filter.Active = true;

                // TODO: Respond to the change in active filter by setting this.DefaultViewModel["Results"]
                //       to a collection of items with bindable Image, Title, Subtitle, and Description properties

                if (filter.Name.Equals("All"))
                {
                    var tempResults = new List<SampleDataItem>();

                    // Add the items for each group to the temporary results lists.
                    foreach (var group in AllResultsCollection)
                    {
                        tempResults.AddRange(group.Value);
                    }

                    // Display the items.
                    this.DefaultViewModel["Results"] = tempResults;
                }

                else if (AllResultsCollection.ContainsKey(filter.Name))
                {
                    this.DefaultViewModel["Results"] = new List<SampleDataItem> (AllResultsCollection[filter.Name]);
                }

                // Ensure results are found

                object results;
                ICollection resultsCollection;
                if (this.DefaultViewModel.TryGetValue("Results", out results) &&
                    (resultsCollection = results as ICollection) != null &&
                    resultsCollection.Count != 0)
                {
                    VisualStateManager.GoToState(this, "ResultsFound", true);
                    return;
                }
            }

            // Display informational text when there are no search results.
            VisualStateManager.GoToState(this, "NoResultsFound", true);
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// View model describing one of the filters available for viewing search results.
        /// </summary>
        private sealed class Filter : INotifyPropertyChanged
        {
            private String _name;
            private int _count;
            private bool _active;

            public Filter(String name, int count, bool active = false)
            {
                this.Name = name;
                this.Count = count;
                this.Active = active;
            }

            public override String ToString()
            {
                return Description;
            }

            public String Name
            {
                get { return _name; }
                set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
            }

            public int Count
            {
                get { return _count; }
                set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
            }

            public bool Active
            {
                get { return _active; }
                set { this.SetProperty(ref _active, value); }
            }

            public String Description
            {
                get { return String.Format("{0} ({1})", _name, _count); }
            }

            /// <summary>
            /// Multicast event for property change notifications.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Checks if a property already matches a desired value.  Sets the property and
            /// notifies listeners only when necessary.
            /// </summary>
            /// <typeparam name="T">Type of the property.</typeparam>
            /// <param name="storage">Reference to a property with both getter and setter.</param>
            /// <param name="value">Desired value for the property.</param>
            /// <param name="propertyName">Name of the property used to notify listeners.  This
            /// value is optional and can be provided automatically when invoked from compilers that
            /// support CallerMemberName.</param>
            /// <returns>True if the value was changed, false if the existing value matched the
            /// desired value.</returns>
            private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
            {
                if (object.Equals(storage, value)) return false;

                storage = value;
                this.OnPropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Notifies listeners that a property value has changed.
            /// </summary>
            /// <param name="propertyName">Name of the property used to notify listeners.  This
            /// value is optional and can be provided automatically when invoked from compilers
            /// that support <see cref="CallerMemberNameAttribute"/>.</param>
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var eventHandler = this.PropertyChanged;
                if (eventHandler != null)
                {
                    eventHandler(this, new PropertyChangedEventArgs(propertyName));
                }
            }

        }

        private void ItemView_ItemClicked(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(ItemDetailPage), itemId);

        }
    }
}
