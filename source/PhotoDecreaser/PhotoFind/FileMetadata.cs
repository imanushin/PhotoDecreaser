using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PhotoDecreaser.PhotoFind
{
    internal sealed class FileMetadata
    {
        public FileMetadata()
        {
            CreationTime = DateTime.UtcNow;
            FilePath = String.Empty;
        }

        public FileMetadata( FileInfo baseFile )
        {
            FilePath = baseFile.FullName;
            CreationTime = baseFile.CreationTimeUtc;
        }

        public String FilePath
        {
            get;
            set;
        }

        public DateTime CreationTime
        {
            get;
            set;
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }

        public override bool Equals( object obj )
        {
            var another = obj as FileMetadata;

            if ( another == null )
                return false;

            return another.FilePath == FilePath;
        }

        public override string ToString()
        {
            if ( String.IsNullOrEmpty( FilePath ) )
                return String.Empty;

            if ( FilePath.Length < 16 )
                return FilePath;

            return Path.GetFileName( FilePath );
        }
    }
}
