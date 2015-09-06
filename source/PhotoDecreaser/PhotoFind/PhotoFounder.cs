using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;

namespace PhotoDecreaser.PhotoFind
{
    internal static class PhotoFounder
    {
        private static readonly int s_minFilesInSecuence = 5;
        private static readonly int s_maxPhotoDifference = 3;

        public static IEnumerable<PhotoInfo> FindPhoto( BackgroundWorker worker )
        {
            worker.ReportProgress( 0, "Поиск устройств..." );

            var drives = DriveInfo.GetDrives()
                .Where( item => !item.RootDirectory.FullName.StartsWith( "A" ) )
                .Where( item => item.IsReady )
                .Where( item => item.DriveType == DriveType.Removable )
                .ToList();

            if ( drives.Count == 0 )
                return null;

            drives.Sort( ( left, right ) => -String.Compare( left.RootDirectory.FullName, right.RootDirectory.FullName ) );

            var root = drives[ 0 ].RootDirectory;

            worker.ReportProgress( 20, "Поиск папки с фотографиями..." );

            var childen = root.GetDirectories().ToList();

            while ( childen.Count > 0 )
            {
                childen.Sort( ( left, right ) => String.Compare( left.Name, right.Name ) );

                root = childen[ 0 ];

                childen = root.GetDirectories().ToList();
            }

            worker.ReportProgress( 30, "Поиск последовательности фотографий..." );

            int progress = 30;
            for ( int maxCount = 15; maxCount < int.MaxValue; maxCount *= 2 )
            {
                var files = root.GetFiles( "*.jp*", SearchOption.TopDirectoryOnly ).Select( ( item ) => new FileMetadata( item ) ).ToList();

                files.Sort( ( l, r ) => ( int )( r.CreationTime - l.CreationTime ).TotalMinutes );

                var foundFiles = FindFiles( files );

                if ( foundFiles != null && foundFiles.Count > 0 && foundFiles[ foundFiles.Count - 1 ] != files[ files.Count - 1 ] )
                {
                    int addition = ( 100 - progress ) / foundFiles.Count;

                    List<PhotoInfo> result = new List<PhotoInfo>();

                    foreach ( var file in foundFiles )
                    {
                        worker.ReportProgress( progress += addition, "Добавление фотографии " + file.FilePath + "..." );

                        result.Add( new PhotoInfo( file.FilePath ) );
                    }

                    return result;
                }

                worker.ReportProgress( progress += 5, "Поиск последовательности фотографий..." );
            }

            return null;
        }

        private static List<FileMetadata> FindFiles( List<FileMetadata> files )
        {
            for ( int i = 0; i < files.Count - s_minFilesInSecuence; i++ )
            {
                if ( !CheckFirstAndLast( files[ i ], files[ i + s_minFilesInSecuence - 1 ] ) )
                    continue;

                Boolean isOk = true;

                for ( int j = 0; j < s_minFilesInSecuence - 1; j++ )
                {
                    if ( !AreClosedDates( files[ i + j ], files[ i + j + 1 ] ) )
                    {
                        isOk = false;
                        break;
                    }
                }

                if ( !isOk )
                    continue;

                int end = i + s_minFilesInSecuence - 1;

                for ( int j = i + s_minFilesInSecuence; j < files.Count - s_minFilesInSecuence; j++ )
                {
                    if ( AreClosedDates( files[ j - 1 ], files[ j ] ) )
                        end = j;
                    else
                        break;
                }

                return files.GetRange( i, end - i + 1 );
            }

            return null;
        }

        private static bool AreClosedDates( FileMetadata first, FileMetadata second )
        {
            return Math.Abs( ( first.CreationTime - second.CreationTime ).TotalMinutes ) < s_maxPhotoDifference;
        }

        private static bool CheckFirstAndLast( FileMetadata first, FileMetadata second )
        {
            return Math.Abs( ( first.CreationTime - second.CreationTime ).TotalMinutes ) < s_maxPhotoDifference * s_minFilesInSecuence;
        }
    }
}
