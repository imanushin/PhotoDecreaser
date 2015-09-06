﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PhotoDecreaser
{
    internal static class FolderSelector
    {
        public static String FindFreeDirectory()
        {
            var parentFolder = "c:\\";

            var drives = DriveInfo.GetDrives();

            var systemFolder = Environment.GetFolderPath( Environment.SpecialFolder.System ).ToUpperInvariant();

            foreach ( var drive in drives )
            {
                if ( drive.DriveType != DriveType.Fixed )
                    continue;

                var root = drive.RootDirectory.FullName.ToUpperInvariant();

                if ( root.Length > 3 )
                    continue;

                if ( systemFolder.StartsWith( root ) )
                    continue;

                parentFolder = root;

                break;
            }

            var photosFolder = Path.Combine( parentFolder, "Photos" );

            var datePreffix = DateTime.Today.ToString( "yyyy_MM_dd" );

            var result = Path.Combine( photosFolder, datePreffix );

            int i = 1;

            while ( Directory.Exists( result ) )
            {
                var innerDirectoryName = datePreffix + " (" + i.ToString() + ')';

                result = Path.Combine( photosFolder, innerDirectoryName );

                i++;
            }

            return result;
        }
    }
}
