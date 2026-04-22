using ImageMagick;
using System;

// Convert cat image to ICO format
var inputPath = @"d:\running cat\gif\18bfb6fdd7998e358b2ba5477e0e5470_t.jpeg";
var outputPath = @"d:\running cat\gif\cat.ico";

using var image = new MagickImage(inputPath);
image.Resize(256, 256);
image.Format = MagickFormat.Ico;
image.Write(outputPath);

Console.WriteLine($"Icon created: {outputPath}");
