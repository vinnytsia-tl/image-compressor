using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ImageCompressor
{
    class Program
    {
        const string SourceDirectory = "source";
        const string ResultDirectory = "converted";
        const string LogFile = "log.log";

        static StreamWriter logWriter;
        public static void WriteLog(string format,  params object[] args)
        {
            var msg = string.Format(format, args);
            logWriter.WriteLine(msg);
            Console.WriteLine(msg);
        }

        public static Bitmap GetResizedBitmap(Image image)
        {
            int width = image.Width;
            int height = image.Height;

            while (width > 1920 || height > 1920)
            {
                width /= 2;
                height /= 2;
            }

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        static void CompressJpeg(string source, string dest)
        {
            var sourceImage = Image.FromFile(source);

            using (var resizedImage = GetResizedBitmap(sourceImage))
            using (var memory = new MemoryStream())
            {
                resizedImage.Save(memory, ImageFormat.Png);

                using (var pngImage = Image.FromStream(memory))
                    pngImage.Save(dest, ImageFormat.Jpeg);
            }
        }

        static bool Compress(string source, string dest)
        {
            try
            {
                var ext = new FileInfo(source).Extension.ToLower();
                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        CompressJpeg(source, dest);
                        return true;
                    default:
                        WriteLog("Unknown extention: {0}. Can't compress {1}.", ext, source);
                        File.Copy(source, dest);
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog("Can't compress {0}. Exception: {1}.",
                    source, ex.Message);

                return false;
            }
        }

        static void Main(string[] args)
        {
            var regex = new Regex(Regex.Escape(SourceDirectory));
            if (!Directory.Exists(SourceDirectory))
                Directory.CreateDirectory(SourceDirectory);
            if (!Directory.Exists(ResultDirectory))
                Directory.CreateDirectory(ResultDirectory);

            logWriter = new StreamWriter(LogFile);
            logWriter.WriteLine("Started at {0}", DateTime.Now.ToString());

            var sourceFiles = Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories);

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 16;

            int processed = 0;
            Parallel.ForEach(sourceFiles, parallelOptions, (file) =>
                {
                    var dest = regex.Replace(file, ResultDirectory, 1);
                    var destDir = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    var res = Compress(file, dest);

                    processed++;
                    WriteLog("{0} of {1}: {2} - {3}",
                        processed, sourceFiles.Length, file, res ? "ok" : "fail");
                }
            );

            WriteLog("Job done!");
            Console.ReadLine();
        }
    }
}
