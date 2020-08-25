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

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageComparer Comparer { get; set; } = new ImageComparer();
        private Thread WorkerThread { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            WorkerThread = new Thread(() =>
            {
                LookForDuplicates();
                Dispatcher.BeginInvoke(new Action(PopulateImages));
                Dispatcher.BeginInvoke(new Action(ResetToDefault));
            });

            WorkerThread.IsBackground = true;
        }

        private void ResetToDefault()
        {
            PrgBarBlock.Visibility = Visibility.Hidden;
            DirLabel.Visibility = Visibility.Visible;

            DirLabel.Content = "No folder selected.";

            DirButton.IsEnabled = true;
            CmpButton.IsEnabled = false;

            CmpButtonLabel.Text = "Start Searching";
            CmpButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Search;
        }

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

        private void CmpButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: SAFE MULTITHREADING
            if (CmpButtonLabel.Text == "Cancel Search")
            {
                WorkerThread.Abort();
                Comparer.Clear();
                ResetToDefault();
                return;
            }

            // Clear previous search
            ImageView.Items.Clear();
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
            WorkerThread.Start();
        }

        private void LookForDuplicates()
        {
            // Action to change UI progressbar that is owned by other thread
            Action<int> action = (i) => 
            {
                Progress.Value = i;

                if (i <= Comparer.Files.Count())  ProgressLabel.Text = $"Preparing files: { i }/{ Comparer.Files.Count() }";
                else ProgressLabel.Text = $"Comparing files: { i - Comparer.Files.Count() }/{ Comparer.Files.Count() }";
            };

            // First prepare image hashes
            for (int i = 0; i < Comparer.Files.Count(); i++)
            {
                Comparer.IteratePreparation(i);
                Dispatcher.BeginInvoke(action, i + 1);
            }

            // Comapare image hashes
            int index = Comparer.Files.Count() + 1;
            foreach (var hash in Comparer.Hashes)
            {
                Comparer.IterateComparison(hash);
                Dispatcher.BeginInvoke(action, index);
                index++;
            }
        }

        private void PopulateImages()
        {
            var item = new TreeViewItem();

            foreach (var orig in Comparer.Result)
            {
                item.Header = orig.Key;

                foreach (var copy in orig.Value)
                {
                    var subitem = new TreeViewItem();

                    subitem.Header = copy;

                    item.Items.Add(subitem);
                }
            }

            ImageView.Items.Add(item);
        }
    }
}
