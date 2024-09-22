using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace _imageMerger
{
    class Program
    {
        static void Main (string [] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine ("Usage: _imageMerger.exe <input1> <input2> ...");
                    return;
                }

                var xImages = args.Order (StringComparer.OrdinalIgnoreCase).Select (x =>
                {
                    try
                    {
                        Image xImage = Image.Load (x);
                        return (Path: x, Image: xImage);
                    }

                    catch
                    {
                        throw new ArgumentException ($"Invalid image file: {x}");
                    }
                });

                int xTotalWidth = xImages.Sum (x => x.Image.Width),
                    xMaxHeight = xImages.Max (x => x.Image.Height);

                using Image xMerged = new Image <Rgba32> (xTotalWidth, xMaxHeight);

                // Requires package: SixLabors.ImageSharp.Drawing
                xMerged.Mutate (x => x.Fill (Color.White));

                int xHorizontalOffset = 0;

                foreach (var xImage in xImages)
                {
                    int xVerticalOffset = (xMaxHeight - xImage.Image.Height) / 2;
                    xMerged.Mutate (x => x.DrawImage (xImage.Image, new Point (xHorizontalOffset, xVerticalOffset), opacity: 1));
                    xHorizontalOffset += xImage.Image.Width;
                }

                string xMergedImageName = Path.GetFileNameWithoutExtension (xImages.First ().Path) + "-Merged.jpg",
                       xMergedImagePath = Path.Join (Path.GetDirectoryName (xImages.First ().Path), xMergedImageName);

                // Overwrites the existing file.
                xMerged.SaveAsJpeg (xMergedImagePath);
                Console.WriteLine ($"Merged image saved to: {xMergedImagePath}");

                foreach (var xImage in xImages)
                {
                    string xMergedDirectoryPath = Path.Join (Path.GetDirectoryName (xImage.Path), "Merged"),
                           xNewFilePath = Path.Join (xMergedDirectoryPath, Path.GetFileName (xImage.Path));

                    Directory.CreateDirectory (xMergedDirectoryPath);
                    File.Move (xImage.Path, xNewFilePath, overwrite: false);
                    Console.WriteLine ($"Moved original image to: {xNewFilePath}");
                }
            }

            catch (Exception xException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine (xException.ToString ());
                Console.ResetColor ();
            }

            finally
            {
                Console.Write ("Press any key to exit: ");
                Console.ReadKey (true);
                Console.WriteLine ();
            }
        }
    }
}
