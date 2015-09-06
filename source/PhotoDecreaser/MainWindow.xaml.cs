using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;

namespace PhotoDecreaser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    sealed partial class MainWindow : Window, IDisposable
    {
        private readonly ObservableCollection<PhotoInfo> files = new ObservableCollection<PhotoInfo>();

        private String saveFolderPath = "Поиск каталога...";

        private readonly BackgroundWorker openFileWorker = new BackgroundWorker();
        private readonly BackgroundWorker fileSaveWorker = new BackgroundWorker();
        private readonly BackgroundWorker saveFolderFounder = new BackgroundWorker();

        private readonly Timer progressTimer = new Timer();

        private volatile int workingPercent = 0;

        public MainWindow()
        {
            InitializeComponent();

            saveFolderFounder.DoWork += new DoWorkEventHandler( saveFolderFounder_DoWork );
            saveFolderFounder.RunWorkerCompleted += new RunWorkerCompletedEventHandler( saveFolderFounder_RunWorkerCompleted );
            saveFolderFounder.RunWorkerAsync();

            widthSize_ValueChanged( null, null );

            openFileWorker.DoWork += new DoWorkEventHandler( OpenSelectedFiles );
            openFileWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler( OpenSelectedFilesCompleted );

            fileSaveWorker.DoWork += new DoWorkEventHandler( SaveFileWorker_DoWork );
            fileSaveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler( SaveFileWorker_RunWorkerCompleted );

            progressTimer.Tick += new EventHandler( progressTimer_Tick );
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

        private void SelectPhoto( object sender, RoutedEventArgs e )
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Filter = "Фотографии JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg";
            dialog.Multiselect = true;

            Boolean? result = dialog.ShowDialog();

            if ( !result.HasValue || !result.Value )
                return;

            busyIndicator.IsBusy = true;

            var fileNames = dialog.FileNames;

            workingPercent = 0;
            progressTimer.Start();

            openFileWorker.RunWorkerAsync( fileNames );
        }

        private void OpenSelectedFilesCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            RefreshPhotoGrid();

            progressTimer.Stop();
        }

        private void RefreshPhotoGrid()
        {
            photosGrid.Children.Clear();

            foreach ( PhotoInfo photo in files )
            {
                Image newImage = new Image()
                {
                    Source = photo.Photo,
                    Tag = photo,
                    Margin = new Thickness( 10 )
                };

                newImage.MouseLeftButtonUp += new MouseButtonEventHandler( newImage_MouseLeftButtonUp );

                photosGrid.Children.Add( newImage );
            }

            photoCount.Text = "Количество фотографий: " + files.Count;

            busyIndicator.IsBusy = false;
        }

        private void OpenSelectedFiles( object sender, DoWorkEventArgs e )
        {
            OpenFiles( e.Argument as IEnumerable<String> );
        }

        private void OpenFiles( IEnumerable<String> fileNames )
        {
            var files = fileNames.ToList();

            for ( int i = 0; i < files.Count; i++ )
            {
                string fileName = files[ i ];

                try
                {
                    var file = new PhotoInfo( fileName );

                    this.files.Add( file );
                }
                catch ( Exception ex )
                {
                    System.Windows.MessageBox.Show( string.Format( "Невозможно открыть файл {0} по причине:\n{1}", fileName, ex ), "Конвертер фотографий", MessageBoxButton.OK, MessageBoxImage.Error );
                }

                workingPercent = ( 100 * ( i + 1 ) ) / files.Count;
            }

            GC.Collect();
        }

        private void newImage_MouseLeftButtonUp( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            Image img = e.OriginalSource as Image;

            if ( img == null )
                return;

            PhotoInfo photo = img.Tag as PhotoInfo;

            if ( photo == null )
                return;

            MessageBoxResult result = System.Windows.MessageBox.Show( "Удалить выбранное изображение?", "Удаление фотографии", MessageBoxButton.YesNo, MessageBoxImage.Question );

            if ( result != MessageBoxResult.Yes )
                return;

            files.Remove( photo );
            photosGrid.Children.Remove( img );
        }

        private void SavePhoto( object sender, RoutedEventArgs e )
        {
            workingPercent = 0;
            progressTimer.Start();

            busyIndicator.IsBusy = true;

            fileSaveWorker.RunWorkerAsync();
        }

        private void SaveFileWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            busyIndicator.IsBusy = false;

            progressTimer.Stop();
        }

        private void SaveFileWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            for ( int i = 0; i < files.Count; i++ )
            {
                var file = files[ i ];

                String newFile = Path.Combine( saveFolderPath, ( i + 1 ).ToString() + ".jpg" );

                file.SaveFile( newFile );

                workingPercent = ( 100 * ( i + 1 ) ) / files.Count;
            }
        }

        private void SelectSaveFolder( object sender, RoutedEventArgs e )
        {
            using ( FolderBrowserDialog saveDialog = new FolderBrowserDialog() )
            {
                saveDialog.SelectedPath = saveFolderPath;

                DialogResult dialogResult = saveDialog.ShowDialog();

                if ( dialogResult != System.Windows.Forms.DialogResult.OK )
                    return;

                saveFolderPath = saveDialog.SelectedPath;

                saveFolder.Text = saveFolderPath;
            }
        }

        private void widthSize_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            Double value = Math.Pow( 2, widthSize.Value );

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
            fileSaveWorker.Dispose();
            openFileWorker.Dispose();
            saveFolderFounder.Dispose();
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
