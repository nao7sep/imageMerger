using System.Text;
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

                bool? xIsLeftToRight;

                while (true)
                {
                    Console.Write ("Left to right? (Y/N): ");
                    ConsoleKeyInfo xInput = Console.ReadKey (true);

                    if (xInput.Key == ConsoleKey.Y)
                    {
                        xIsLeftToRight = true;
                        Console.WriteLine ("Y");
                        break;
                    }

                    else if (xInput.Key == ConsoleKey.N)
                    {
                        xIsLeftToRight = false;
                        Console.WriteLine ("N");
                        break;
                    }

                    Console.WriteLine ();
                }

                int? xPagesPerGroup = null;

                if (args.Length >= 4 && Enumerable.Range (2, args.Length - 2).Select (x => args.Length % x).Any (y => y == 0))
                {
                    Console.WriteLine ("Total pages: " + args.Length);

                    while (true)
                    {
                        Console.Write ("Pages per group: ");
                        string? xInput = Console.ReadLine ();

                        if (int.TryParse (xInput, out int xValue) && xValue >= 2 && xValue < args.Length && args.Length % xValue == 0)
                        {
                            xPagesPerGroup = xValue;
                            break;
                        }
                    }
                }

                void _Merge (string [] filePaths)
                {
                    var xImages = filePaths.Select (x =>
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

                    string xMergedFilePath = Path.Join (Path.GetDirectoryName (xImages.First ().Path), "Merged", "Merged.txt"),
                        xMergedFileContent = $"{xMergedImageName}: {string.Join (" | ", filePaths.Select (x => Path.GetFileName (x)))}";

                    File.AppendAllLines (xMergedFilePath, [xMergedFileContent], Encoding.UTF8);
                }

                var xSortedFilePaths = args.Order (StringComparer.OrdinalIgnoreCase).ToArray ();

                string [] _Sort (string [] sortedFilePaths, int firstIndex, int count, bool isLeftToRight)
                {
                    var xFilePaths = sortedFilePaths.AsSpan (firstIndex, count);

                    if (isLeftToRight == false)
                        xFilePaths.Reverse ();

                    return xFilePaths.ToArray ();
                }

                if (xPagesPerGroup.HasValue)
                {
                    for (int temp = 0; temp < xSortedFilePaths.Length; temp += xPagesPerGroup.Value)
                    {
                        // No individual try-catch block for each group because, if one fails, we'll have to abort the entire operation and fix everything.
                        Console.WriteLine ($"Group {temp / xPagesPerGroup.Value + 1} of {xSortedFilePaths.Length / xPagesPerGroup.Value}");
                        _Merge (_Sort (xSortedFilePaths, temp, xPagesPerGroup.Value, xIsLeftToRight.Value));
                    }
                }

                else _Merge (_Sort (xSortedFilePaths, 0, xSortedFilePaths.Length, xIsLeftToRight.Value));
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
