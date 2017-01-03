using MG2.Common;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using MG2.Data;


// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace MG2
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Favourites : Page
    {

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


        public Favourites()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            
        }

        async public void loadFavourites()
        {
            try
            {
                // Read favourites.txt if exist
                StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile sampleFile = await storageFolder.GetFileAsync("favourites.txt");
                // Populate favouritesListView with the data

                string strSampleFile = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
                //string strSampleFile = await Windows.Storage.FileIO.ReadLinesAsync(sampleFile).ToString();
                

            }
            catch (Exception)
            {

            }
            
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {

            var sampleDataGroups = await SampleDataSource.GetGroupsAsync();
            this.DefaultViewModel["Groups"] = sampleDataGroups;

            //var group = await SampleDataSource.GetGroupAsync((String)e.NavigationParameter);
            //this.DefaultViewModel["Group"] = group;
            //this.DefaultViewModel["Items"] = group.Items;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
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

        private void allListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            

            try
            {
                var itemID = ((SampleDataItem)e.ClickedItem).UniqueId;
                var titleID = ((SampleDataItem)e.ClickedItem).Title;
                var subtitleID = ((SampleDataItem)e.ClickedItem).Subtitle;
                var clickedItem = titleID.ToString() + ", " + subtitleID.ToString();
                this.ResultTextBlock.Text = "You clicked: " + clickedItem + "\n";

                // Add item to RIGHT listview if not exist => unique

                if (!favouritesListView.Items.Contains(clickedItem))
                {
                    favouritesListView.Items.Add(clickedItem);
                }
                else
                {
                    this.ResultTextBlock.Text += "Error: DUPLICATION\n";
                }

                

            }
            catch (Exception)
            {
                this.ResultTextBlock.Text = "Click FAILED";
            }
          
            
        }

        private void allListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
            
        }

        async private void saveFavourites(object sender, RoutedEventArgs e)
        {
            this.ResultTextBlock.Text = "You clicked SAVE button.";
            
            // Read favouritesListView into text and save to textfile
            int numberOfFavourites = favouritesListView.Items.Count;

            string[] arrFavourites = new string[numberOfFavourites];
             
            favouritesListView.Items.CopyTo(arrFavourites, 0);

            // Read array then write to text file
            string allFavourites="";
            foreach (string dest in arrFavourites)
            {
                allFavourites += dest + " ; ";
            }

            this.ResultTextBlock.Text += "\nYour saved favourites will be: " + allFavourites;

            // Create sample file; replace if exists
            try
            {
                StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile favouritesFile = await folder.CreateFileAsync("favourites.txt", CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteTextAsync(favouritesFile, allFavourites);   

                this.ResultTextBlock.Text += "\nFile favourites.txt successfully created.\n";
            } 
            catch (Exception)
            {
                this.ResultTextBlock.Text += "File Creation Failed.\n";
            }
            
        }


        private void clearFavourites(object sender, RoutedEventArgs e)
        {
            this.ResultTextBlock.Text = "You clicked CLEAR button. Do not forget to press SAVE button.";
            
            // Clear favouritesListView
            this.favouritesListView.Items.Clear();
        }

        async private void viewFavourites(object sender, RoutedEventArgs e)
        {
            // Clear the favouritesListView
            this.favouritesListView.Items.Clear();

            // Read favourites.txt and display in favouritesListView;
            try
            {
                StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile sampleFile = await storageFolder.GetFileAsync("favourites.txt");
                
                this.ResultTextBlock.Text = "You click VIEW button.\n";
                string result = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
                this.ResultTextBlock.Text += result;

                if (result != ""){ 
                    // Convert string result into line;
                    string[] separators = {";"};
                    string[] words = result.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        // Display to favouritesListView
                        favouritesListView.Items.Add(word);
                    }
                }
                else
                {
                    this.ResultTextBlock.Text += "\nfavourites.txt is empty";
                }

                           
            }
            catch (Exception)
            {
                this.ResultTextBlock.Text = "File favourites.txt does not exist";
            }


        }
    }
}
