using System;
using System.IO;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoDecreaser
{
    internal sealed class PhotoInfo
    {
        private const long maxFileLenght = 300 * 1024;
        private readonly byte[] photoData;

        private PhotoInfo(ImageAndSource image)
        {
            photoData = image.source;

            Photo = image.imahe;
        }

        public static async Task<PhotoInfo> CreatePhotoAsync(string fileName)
        {
            var image = await DecreaseImage(fileName);

            return new PhotoInfo(image);
        }

        public BitmapSource Photo
        {
            get;
            private set;
        }

        public int FileLenght => photoData.Length;

        private static async Task<ImageAndSource> DecreaseImage(string inputFile)
        {
            var newLenght = maxFileLenght * 3;

            var initialLength = new FileInfo(inputFile).Length;

            var initialImage = await FileToImage(inputFile).ConfigureAwait(false);
            
            while (true)
            {
                using (var scaledImage = new MemoryStream())
                {
                    var scale = Math.Sqrt((((double)newLenght) / initialLength));

                    var transformed = new TransformedBitmap(initialImage, new ScaleTransform(scale, scale));

                    transformed.Freeze();

                    var saver = new JpegBitmapEncoder();

                    saver.Frames.Add(BitmapFrame.Create(transformed));

                    saver.Save(scaledImage);

                    scaledImage.Seek(0, SeekOrigin.Begin);

                    newLenght = newLenght * 4 / 5;

                    if (scaledImage.Length < maxFileLenght)
                    {
                        return new ImageAndSource
                        {
                            imahe = transformed,
                            source = scaledImage.ToArray()
                        };
                    }
                }
            }
        }

        private static async Task<BitmapImage> FileToImage(string inputFile)
        {
            using (var fileStream = File.OpenRead(inputFile))
            {
                using (var ms = new MemoryStream())
                {
                    await fileStream.CopyToAsync(ms).ConfigureAwait(false);

                    ms.Seek(0, SeekOrigin.Begin);

                    var image = new BitmapImage();
                    image.BeginInit();
                    
                    image.CacheOption = BitmapCacheOption.OnLoad; // here
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();

                    return image;
                }
            }
        }

        public async Task SaveFileAsync(string newFile)
        {
            using (var stream = File.Open(newFile, FileMode.Create))
            {
                await stream.WriteAsync(photoData, 0, photoData.Length).ConfigureAwait(false);
            }
        }

        private class ImageAndSource
        {
            public byte[] source;
            public BitmapSource imahe;
        }
    }
}
