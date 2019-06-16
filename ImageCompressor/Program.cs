using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageCompressor
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = Directory.GetFiles("source\\", "*", SearchOption.AllDirectories);
            int countok = 0;
            int counterr = 0;
            int length = files.Length;
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 16;
            Action<int> body = i =>
            {
                try
                {
                    string path = Path.GetDirectoryName(files[i]).Replace("source\\", "converted\\");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    string str = string.Format("tmp\\tmp{0}.png", (object)i);
                    string filename = string.Format("{0}\\{1}.jpg", (object)path, (object)Path.GetFileNameWithoutExtension(files[i]));
                    Image image1 = Image.FromFile(files[i]);
                    image1.Save(str, ImageFormat.Png);
                    image1.Dispose();
                    Image image2 = Image.FromFile(str);
                    image2.Save(filename, ImageFormat.Jpeg);
                    image2.Dispose();
                    File.Delete(str);
                    ++countok;
                }
                catch
                {
                    ++counterr;
                    Console.WriteLine("Error in converting file: {0}", (object)files[i]);
                }
                Console.WriteLine("{0} Images converted from {1}. Error count: {2}", (object)countok, (object)files.Length, (object)counterr);
            };
            Parallel.For(0, length, parallelOptions, body);
            int num = (int)MessageBox.Show(string.Format("{0} Images converted from {1}. Error count: {2}\n Job done!", (object)countok, (object)files.Length, (object)counterr));
        }
    }
}
