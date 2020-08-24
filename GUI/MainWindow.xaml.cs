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
        private ImageComparer comparer { get; set; } = new ImageComparer();

        public MainWindow()
        {
            InitializeComponent();
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
            ImageView.Items.Clear();
            comparer.Clear();
            comparer.SetPath((string)DirLabel.Content);

            Progress.Maximum = comparer.Files.Count() * 2;

            Thread thread = new Thread(() => 
            { 
                
                LookForDuplicates();

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    var item = new TreeViewItem();

                    foreach (var orig in comparer.Result)
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
                }));

            });

            thread.Start();
        }

        private void LookForDuplicates()
        {
            Action<int> action = (i) => {
                Progress.Value = i;

                if (i <= comparer.Files.Count())
                {
                    ProgressLabel.Text = $"Preparing files: { i }/{ comparer.Files.Count() }";
                }
                else
                {
                    ProgressLabel.Text = $"Comparing files: { i - comparer.Files.Count() }/{ comparer.Files.Count() }";
                }
            };

            for (int i = 0; i < comparer.Files.Count(); i++)
            {
                comparer.IteratePreparation(i);

                Dispatcher.BeginInvoke(action, i + 1);
            }

            int index = comparer.Files.Count() + 1;

            foreach (var hash in comparer.Hashes)
            {
                comparer.IterateComparison(hash);

                Dispatcher.BeginInvoke(action, index);

                index++;
            }
        }
    }
}
