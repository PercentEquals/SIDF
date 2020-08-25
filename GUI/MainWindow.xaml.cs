using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using SIDFLibrary;
using System.Threading;
using System.IO;
using System.Net.Cache;
using System.Diagnostics;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields and Properties

        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

        private ImageComparer Comparer { get; set; } = new ImageComparer();
        private Thread WorkerThread { get; set; }

        #endregion

        #region Initializer

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region ResetMethod

        /// <summary>
        /// Resets state of application to its initial form
        /// </summary>
        private void ResetToDefault()
        {
            PrgBarBlock.Visibility = Visibility.Hidden;
            DirLabel.Visibility = Visibility.Visible;

            DirLabel.Content = "No folder selected.";

            DirButton.IsEnabled = true;
            CmpButton.IsEnabled = false;

            CmpButtonLabel.Text = "Start Searching";
            CmpButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Search;

            Progress.Value = 0;
            ProgressLabel.Text = "";
        }

        #endregion

        #region Button Clicks

        /// <summary>
        /// Opens folder dialog on button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Multiselect = false;
            dialog.IsFolderPicker = true;
            dialog.Title = "Select folders with images";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DirLabel.Content = dialog.FileName;
                CmpButton.IsEnabled = true;
            }
            else
            {
                DirLabel.Content = "No folder selected.";
                CmpButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Creates (or stops) worker thread that searches for duplicate images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmpButton_Click(object sender, RoutedEventArgs e)
        {
            // If button was 'tranformed' to be cancel button, then cancel thread
            if (CmpButtonLabel.Text == "Cancel Search")
            {
                _shutdownEvent.Set();
                WorkerThread.Join();
                _shutdownEvent.Reset();
                
                Comparer.Clear();
                return;
            }

            // Clear previous search
            Comparer.Clear();

            // Set new path
            Comparer.SetPath((string)DirLabel.Content);

            // Hide label and in place show progressbar
            PrgBarBlock.Visibility = Visibility.Visible;
            DirLabel.Visibility = Visibility.Hidden;

            // Disable folder selection button
            DirButton.IsEnabled = false;

            // Set Maximum for progressbar as 2 times image count
            Progress.Maximum = Comparer.Files.Count() * 2;

            // Change this button to cancel button
            CmpButtonLabel.Text = "Cancel Search";
            CmpButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Times;

            // Look for duplicates in new thread so UI can work indepedently
            WorkerThread = new Thread(() =>
            {
                LookForDuplicates();
                Dispatcher.BeginInvoke(new Action(PopulateImages));
                Dispatcher.BeginInvoke(new Action(ResetToDefault));
            });

            WorkerThread.IsBackground = true;

            WorkerThread.Start();
        }

        /// <summary>
        /// Shows image preview with usefull data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageView_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item == null) return;

            ImgBind file = (ImgBind)item;

            BitmapImage image = new BitmapImage(new Uri(file.FullName));
            ImgPreview.Source = image;

            FileInfo fi = new FileInfo(file.FullName);

            List<DataBind> items = new List<DataBind>();
            items.Add(new DataBind() { Property = "Name", Value = fi.Name });
            items.Add(new DataBind() { Property = "Path", Value = fi.DirectoryName });
            items.Add(new DataBind() { Property = "Size", Value = Utils.GetFileSize(fi.FullName) });
            items.Add(new DataBind() { Property = "Dimensions", Value = $"{ image.Width }x{ image.Height }" });
            items.Add(new DataBind() { Property = "DPI", Value = $"{ image.DpiX }x{ image.DpiY }" });
            DataPreview.ItemsSource = items;
        }

        /// <summary>
        /// Shows image in user's defualt program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgPreview_Click(object sender, RoutedEventArgs e)
        {
            Process.Start((sender as Image).Source.ToString());
        }

        #endregion

        #region Worker Method

        /// <summary>
        /// Looks for duplicate images (prepares them and then compares).
        /// This method was created with multithreading in mind.
        /// </summary>
        private void LookForDuplicates()
        {
            // Action to change UI progressbar that is owned by other thread
            Action action = () => 
            {
                Progress.Value++;

                if (Progress.Value <= Comparer.Files.Count())  ProgressLabel.Text = $"Preparing files: { Progress.Value }/{ Comparer.Files.Count() }";
                else ProgressLabel.Text = $"Comparing files: { Progress.Value - Comparer.Files.Count() }/{ Comparer.Files.Count() }";
            };

            // Generate image hashes
            Parallel.For(0, Comparer.Files.Count(), (i) =>
            {
                if (_shutdownEvent.WaitOne(0)) return; // Check for cancelation

                Comparer.IteratePreparation(i);
                Dispatcher.BeginInvoke(action);
            });

            // Comapare image hashes
            foreach (var hash in Comparer.Hashes)
            {
                if (_shutdownEvent.WaitOne(0)) return; // Check for cancelation

                Comparer.IterateComparison(hash);
                Dispatcher.BeginInvoke(action);
            }
        }

        #endregion

        #region Populate ListView

        /// <summary>
        /// Populate ListView with images and groups
        /// </summary>
        private void PopulateImages()
        {
            List<ImgBind> items = new List<ImgBind>();
            int group = 0;

            foreach (var orig in Comparer.Result)
            {
                group++;

                items.Add(new ImgBind() { Name = System.IO.Path.GetFileName(orig.Key), Group = $"Group { group }", Size = orig.Key, FullName = orig.Key });

                foreach (var copy in orig.Value)
                {
                    items.Add(new ImgBind() { Name = System.IO.Path.GetFileName(copy), Group = $"Group { group }", Size = copy, FullName = copy });
                }
            }

            ImageView.ItemsSource = items;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ImageView.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Group");
            view.GroupDescriptions.Add(groupDescription);
        }

        #endregion
    }
}
