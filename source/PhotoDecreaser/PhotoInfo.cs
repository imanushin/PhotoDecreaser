using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Net.Cache;

namespace PhotoDecreaser
{
    internal sealed class PhotoInfo
    {
        private const long maxFileLenght = 300 * 1024;
        private readonly Byte[] photoData;

        public PhotoInfo( String fileName )
        {
            ImageAndSource image = DecreaseImage( fileName );

            photoData = image.source;

            Photo = image.imahe;
        }

        public BitmapSource Photo
        {
            get;
            private set;
        }

        public int FileLenght
        {
            get
            {
                return photoData.Length;
            }
        }

        public MemoryStream CreateStream()
        {
            MemoryStream result = null;

            try
            {
                result = new MemoryStream();

                result.Write( photoData, 0, photoData.Length );

                return result;
            }
            catch
            {
                if ( result != null )
                    result.Close();

                throw;
            }
        }

        private static ImageAndSource DecreaseImage( String inputFile )
        {
            Int64 newLenght = maxFileLenght * 3;

            Int64 initialLength = new FileInfo( inputFile ).Length;

            BitmapImage initialImage = new BitmapImage( new Uri( inputFile ), new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore ) );

            initialImage.Freeze();

            while ( true )
            {
                using ( MemoryStream scaledImage = new MemoryStream() )
                {
                    Double scale = Math.Sqrt( ( ( ( double )newLenght ) / initialLength ) );

                    TransformedBitmap transformed = new TransformedBitmap( initialImage, new ScaleTransform( scale, scale ) );

                    transformed.Freeze();

                    JpegBitmapEncoder saver = new JpegBitmapEncoder();                    

                    saver.Frames.Add( BitmapFrame.Create( transformed ) );

                    saver.Save( scaledImage );

                    scaledImage.Seek( 0, SeekOrigin.Begin );

                    newLenght = newLenght * 4 / 5;

                    if ( scaledImage.Length < maxFileLenght )
                    {
                        transformed.Freeze();

                        return new ImageAndSource
                        {
                            imahe = transformed,
                            source = scaledImage.ToArray()
                        };
                    }                    
                }

            }
        }

        public void SaveFile( string newFile )
        {
            File.WriteAllBytes( newFile, photoData );
        }

        private class ImageAndSource
        {
            public byte[] source;
            public BitmapSource imahe;
        }
    }
}
