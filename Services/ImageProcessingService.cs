using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageSplitter.Services
{
    public class ImageProcessingService
    {
        public void SplitImageToFolder(string filePath, int totalRows, int totalCols, Action<string> logCallback)
        {
            string? baseDirectory = Path.GetDirectoryName(filePath);
            if (baseDirectory == null) return;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            string targetFolder = Path.Combine(baseDirectory, fileNameWithoutExt);
            int folderIndex = 2;
            while (Directory.Exists(targetFolder))
            {
                targetFolder = Path.Combine(baseDirectory, $"{fileNameWithoutExt} ({folderIndex})");
                folderIndex++;
            }

            Directory.CreateDirectory(targetFolder);
            string finalFolderName = Path.GetFileName(targetFolder);

            using (Bitmap sourceBitmap = new Bitmap(filePath))
            {
                int originalWidth = sourceBitmap.Width;
                int originalHeight = sourceBitmap.Height;
                int baseWidth = originalWidth / totalCols;
                int baseHeight = originalHeight / totalRows;
                int index = 1;

                ImageCodecInfo? jpegEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                EncoderParameters? encoderParams = null;
                if (sourceBitmap.RawFormat.Equals(ImageFormat.Jpeg))
                {
                    encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                }

                for (int r = 0; r < totalRows; r++)
                {
                    for (int c = 0; c < totalCols; c++)
                    {
                        int currentSliceWidth = (c == totalCols - 1) ? (originalWidth - (baseWidth * (totalCols - 1))) : baseWidth;
                        int currentSliceHeight = (r == totalRows - 1) ? (originalHeight - (baseHeight * (totalRows - 1))) : baseHeight;

                        int startX = c * baseWidth;
                        int startY = r * baseHeight;

                        using (Bitmap sliceBitmap = new Bitmap(currentSliceWidth, currentSliceHeight, sourceBitmap.PixelFormat))
                        {
                            using (Graphics g = Graphics.FromImage(sliceBitmap))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.CompositingQuality = CompositingQuality.HighQuality;

                                g.DrawImage(sourceBitmap, 
                                    new Rectangle(0, 0, currentSliceWidth, currentSliceHeight), 
                                    new Rectangle(startX, startY, currentSliceWidth, currentSliceHeight), 
                                    GraphicsUnit.Pixel);
                            }

                            string outputFileName = $"{fileNameWithoutExt}({index}){extension}";
                            string outputPath = Path.Combine(targetFolder, outputFileName);

                            if (sourceBitmap.RawFormat.Equals(ImageFormat.Jpeg) && jpegEncoder != null && encoderParams != null)
                                sliceBitmap.Save(outputPath, jpegEncoder, encoderParams);
                            else
                                sliceBitmap.Save(outputPath, sourceBitmap.RawFormat);

                            index++;
                        }
                    }
                }
                logCallback?.Invoke(finalFolderName);
            }
        }
    }
}