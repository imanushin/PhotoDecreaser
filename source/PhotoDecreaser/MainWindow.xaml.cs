using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Timer = System.Windows.Forms.Timer;

namespace PhotoDecreaser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    sealed partial class MainWindow : Window, IDisposable
    {
        private readonly ObservableCollection<PhotoInfo> files = new ObservableCollection<PhotoInfo>();

        private string saveFolderPath = "Поиск каталога...";

        /*private readonly BackgroundWorker openFileWorker = new BackgroundWorker();
        private readonly BackgroundWorker fileSaveWorker = new BackgroundWorker();
        private readonly BackgroundWorker saveFolderFounder = new BackgroundWorker();
        */
        private readonly Timer progressTimer = new Timer();

        private volatile int workingPercent;

        public MainWindow()
        {
            InitializeComponent();

            /*saveFolderFounder.DoWork += new DoWorkEventHandler( saveFolderFounder_DoWork );
            saveFolderFounder.RunWorkerCompleted += new RunWorkerCompletedEventHandler( saveFolderFounder_RunWorkerCompleted );
            saveFolderFounder.RunWorkerAsync();*/

            widthSize_ValueChanged( null, null );

           /* openFileWorker.DoWork += new DoWorkEventHandler( OpenSelectedFiles );
            openFileWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler( OpenSelectedFilesCompleted );*/

           /* fileSaveWorker.DoWork += new DoWorkEventHandler( SaveFileWorker_DoWork );
            fileSaveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler( SaveFileWorker_RunWorkerCompleted );*/

            progressTimer.Tick += progressTimer_Tick;
            progressTimer.Interval = 1000;
        }

        private void saveFolderFounder_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            saveFolder.Text = saveFolderPath;
        }

        private void saveFolderFounder_DoWork( object sender, DoWorkEventArgs e )
        {
            saveFolderPath = FolderSelector.FindFreeDirectory();

            Directory.CreateDirectory( saveFolderPath );
        }

        private void progressTimer_Tick( object sender, EventArgs e )
        {
            busyIndicator.PercentCompleted = workingPercent;
        }

        private async void SelectPhoto( object sender, RoutedEventArgs e )
        {
            var dialog = new OpenFileDialog();

            dialog.Filter = "Фотографии JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg";
            dialog.Multiselect = true;

            var result = dialog.ShowDialog();

            if ( !result.HasValue || !result.Value )
                return;

            busyIndicator.IsBusy = true;

            var fileNames = dialog.FileNames;

            workingPercent = 0;
            progressTimer.Start();

            await Task.Factory.StartNew(
                () => OpenFiles(fileNames), 
                CancellationToken.None, 
                TaskCreationOptions.None, 
                TaskScheduler.Default);

            OpenSelectedFilesCompleted();
        }

        private void OpenSelectedFilesCompleted()
        {
            RefreshPhotoGrid();

            progressTimer.Stop();
        }

        private void RefreshPhotoGrid()
        {
            photosGrid.Children.Clear();

            foreach ( var photo in files )
            {
                var newImage = new Image
                {
                    Source = photo.Photo,
                    Tag = photo,
                    Margin = new Thickness( 10 )
                };

                newImage.MouseLeftButtonUp += newImage_MouseLeftButtonUp;

                photosGrid.Children.Add( newImage );
            }

            photoCount.Text = "Количество фотографий: " + files.Count;

            busyIndicator.IsBusy = false;
        }

        private void OpenFiles( IEnumerable<string> fileNames )
        {
            var filesToOpen = fileNames.ToList();

            var finishedCount = 0;

            filesToOpen.AsParallel().ForAll(name =>
            {
                try
                {
                    var file = new PhotoInfo(name);

                    finishedCount++;

                    files.Add(file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Невозможно открыть файл {name} по причине:\n{ex}", "Конвертер фотографий", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                workingPercent = (100 * (finishedCount + 1)) / filesToOpen.Count;
            });

            GC.Collect();
        }

        private void newImage_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            var img = e.OriginalSource as Image;

            if ( img == null )
                return;

            var photo = img.Tag as PhotoInfo;

            if ( photo == null )
                return;

            var result = MessageBox.Show( "Удалить выбранное изображение?", "Удаление фотографии", MessageBoxButton.YesNo, MessageBoxImage.Question );

            if ( result != MessageBoxResult.Yes )
                return;

            files.Remove( photo );
            photosGrid.Children.Remove( img );
        }

        private async void SavePhoto( object sender, RoutedEventArgs e )
        {
            workingPercent = 0;
            progressTimer.Start();

            busyIndicator.IsBusy = true;

            await Task.Factory.StartNew(SaveFileWorker_DoWork, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            SaveFileWorker_RunWorkerCompleted();
        }

        private void SaveFileWorker_RunWorkerCompleted()
        {
            busyIndicator.IsBusy = false;

            progressTimer.Stop();
        }

        private void SaveFileWorker_DoWork()
        {
            for ( var i = 0; i < files.Count; i++ )
            {
                var file = files[ i ];

                var newFile = Path.Combine( saveFolderPath, ( i + 1 ) + ".jpg" );

                file.SaveFile( newFile );

                workingPercent = ( 100 * ( i + 1 ) ) / files.Count;
            }
        }

        private void SelectSaveFolder( object sender, RoutedEventArgs e )
        {
            using ( var saveDialog = new FolderBrowserDialog() )
            {
                saveDialog.SelectedPath = saveFolderPath;

                var dialogResult = saveDialog.ShowDialog();

                if ( dialogResult != System.Windows.Forms.DialogResult.OK )
                    return;

                saveFolderPath = saveDialog.SelectedPath;

                saveFolder.Text = saveFolderPath;
            }
        }

        private void widthSize_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            var value = Math.Pow( 2, widthSize.Value );

            if ( photosGrid != null )
            {
                photosGrid.ItemHeight = value;
                photosGrid.ItemWidth = value;
            }
        }

        private void ClearPhotoGrid( object sender, RoutedEventArgs e )
        {
            files.Clear();
            RefreshPhotoGrid();
        }

        public void Dispose()
        {
            progressTimer.Dispose();
        }

        private void scrollViewer_MouseWheel( object sender, MouseWheelEventArgs e )
        {
            if ( !Keyboard.IsKeyDown( Key.LeftCtrl ) && !Keyboard.IsKeyDown( Key.RightCtrl ) )
                return;

            widthSize.Value += ( ( double )e.Delta ) / 300;
        }
    }
}
