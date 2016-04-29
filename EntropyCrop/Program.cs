using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Colors;
using ImageProcessor.Imaging.Filters.Binarization;
using ImageProcessor.Imaging.Filters.EdgeDetection;

namespace ConsoleApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("croping...");

            var image = new ImageFactory();
            image.Load("docInput.jpg");
            EntropyCropFull(image.Image, 128).Save("docOutput.jpg");
        }

        private static Image EntropyCropFull(Image image, byte treshold)
        {
            Bitmap newImage = null;
            Bitmap grey = null;

            try
            {
                // Detect the edges then strip out middle shades.
                grey = new ConvolutionFilter(new SobelEdgeFilter(), true).Process2DFilter(image);
                grey = new BinaryThreshold(treshold).ProcessFilter(grey);

                // Search for the first white pixels
                Rectangle rectangle = GetFilteredBoundingRectangle(grey, 0, RgbaComponent.R);
                grey.Dispose();

                newImage = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppPArgb);
                newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                using (Graphics graphics = Graphics.FromImage(newImage))
                {
                    graphics.DrawImage(
                                     image,
                                     new Rectangle(0, 0, rectangle.Width, rectangle.Height),
                                     rectangle.X,
                                     rectangle.Y,
                                     rectangle.Width,
                                     rectangle.Height,
                                     GraphicsUnit.Pixel);
                }

                // Reassign the image.
                image.Dispose();
                image = newImage;

                return image;
                
                //if (factory.PreserveExifData && factory.ExifPropertyItems.Any())
                //{
                //    // Set the width EXIF data.
                //    factory.SetPropertyItem(ExifPropertyTag.ImageWidth, (ushort)image.Width);

                //    // Set the height EXIF data.
                //    factory.SetPropertyItem(ExifPropertyTag.ImageHeight, (ushort)image.Height);
                //}
            }
            catch (Exception ex)
            {
                if (grey != null)
                {
                    grey.Dispose();
                }

                if (newImage != null)
                {
                    newImage.Dispose();
                }

                throw new Exception("Error processing image", ex);
            }

            return image;
        }

        private static Rectangle GetFilteredBoundingRectangle(Image bitmap, byte componentValue, RgbaComponent channel = RgbaComponent.B)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            Point topLeft = new Point();
            Point bottomRight = new Point();

            Func<FastBitmap, int, int, byte, bool> delegateFunc;

            // Determine which channel to check against
            switch (channel)
            {
                case RgbaComponent.R:
                    delegateFunc = (fastBitmap, x, y, b) => fastBitmap.GetPixel(x, y).R != b;
                    break;

                case RgbaComponent.G:
                    delegateFunc = (fastBitmap, x, y, b) => fastBitmap.GetPixel(x, y).G != b;
                    break;

                case RgbaComponent.A:
                    delegateFunc = (fastBitmap, x, y, b) => fastBitmap.GetPixel(x, y).A != b;
                    break;

                default:
                    delegateFunc = (fastBitmap, x, y, b) => fastBitmap.GetPixel(x, y).B != b;
                    break;
            }

            Func<FastBitmap, int> getMinY = fastBitmap =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (delegateFunc(fastBitmap, x, y, componentValue))
                        {
                            return y;
                        }
                    }
                }

                return 0;
            };

            Func<FastBitmap, int> getMaxY = fastBitmap =>
            {
                for (int y = height - 1; y > -1; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (delegateFunc(fastBitmap, x, y, componentValue))
                        {
                            return y;
                        }
                    }
                }

                return height;
            };

            Func<FastBitmap, int> getMinX = fastBitmap =>
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (delegateFunc(fastBitmap, x, y, componentValue))
                        {
                            return x;
                        }
                    }
                }

                return 0;
            };

            Func<FastBitmap, int> getMaxX = fastBitmap =>
            {
                for (int x = width - 1; x > -1; x--)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (delegateFunc(fastBitmap, x, y, componentValue))
                        {
                            return x;
                        }
                    }
                }

                return height;
            };

            using (FastBitmap fastBitmap = new FastBitmap(bitmap))
            {
                topLeft.Y = getMinY(fastBitmap);
                topLeft.X = getMinX(fastBitmap);
                bottomRight.Y = getMaxY(fastBitmap) + 1;
                bottomRight.X = getMaxX(fastBitmap) + 1;
            }

            return GetBoundingRectangle(topLeft, bottomRight);
        }

        private static Rectangle GetBoundingRectangle(Point topLeft, Point bottomRight)
        {
            return new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}
