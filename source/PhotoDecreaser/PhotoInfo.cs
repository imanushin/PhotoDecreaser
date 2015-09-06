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
        private readonly byte[] photoData;

        public PhotoInfo( string fileName )
        {
            var image = DecreaseImage( fileName );

            photoData = image.source;

            Photo = image.imahe;
        }

        public BitmapSource Photo
        {
            get;
            private set;
        }

        public int FileLenght => photoData.Length;

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
                result?.Close();

                throw;
            }
        }

        private static ImageAndSource DecreaseImage( string inputFile )
        {
            var newLenght = maxFileLenght * 3;

            var initialLength = new FileInfo( inputFile ).Length;

            var initialImage = new BitmapImage( new Uri( inputFile ), new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore ) );

            initialImage.Freeze();

            while ( true )
            {
                using ( var scaledImage = new MemoryStream() )
                {
                    var scale = Math.Sqrt( ( ( ( double )newLenght ) / initialLength ) );

                    var transformed = new TransformedBitmap( initialImage, new ScaleTransform( scale, scale ) );

                    transformed.Freeze();

                    var saver = new JpegBitmapEncoder();                    

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
