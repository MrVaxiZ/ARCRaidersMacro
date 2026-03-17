using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Reflection;
using Point = OpenCvSharp.Point;
using static MacroArcRaiders.ImportsDLL;

#pragma warning disable CS8602 // Stupid ass warning who needs it anyways / I know I can disable it in csproj but that would not fix it only cause more probles on it's own

namespace MacroArcRaiders
{
    class MatHandler
    {
        internal static bool CheckResolution(ref Dictionary<State, Mat> templates)
        {
            try
            {
                (double scaleX, double scaleY) = GetResolution();

                if (Math.Abs(scaleX - 1.0) < 0.01 && Math.Abs(scaleY - 1.0) < 0.01)
                {
                    Console.WriteLine("-> Running at native 1440p – no scaling needed");
                }
                else
                {
                    // Scale every template
                    foreach (var kvp in templates)
                    {
                        templates[kvp.Key] = ResizeTemplate(kvp.Value, scaleX, scaleY);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR : MatHandler->CheckResolution : <{ex.Message}>");
                Console.WriteLine($"Resize failed unable to start. Closing makro...");
                return false;
            }
        }

        // Helper method – always returns 3-channel BGR Mat or throws
        internal static Mat LoadAsBgr(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"{nameof(MacroArcRaiders)}.{ConstVars.TEMPLATE_DIRECTORY}.{resourceName}"; // namespace.folder.filename

            using var stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
                throw new Exception($"Cannot find embedded resource: <{fullResourceName}>. Ensure it's marked as 'Embedded Resource' and the name/namespace matches.");

            // Read stream to byte array
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            // Load from bytes (ImDecode handles any format)
            var mat = Cv2.ImDecode(bytes, ImreadModes.AnyColor);
            if (mat.Empty())
                throw new Exception($"Cannot load template from resource: <{resourceName}>");

            // Convert to 3-channel BGR if needed (same as before)
            if (mat.Channels() == 4)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2BGR);
            }
            else if (mat.Channels() == 1)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.GRAY2BGR);
            }
            else if (mat.Channels() == 3)
            {
                if (mat.Type() != MatType.CV_8UC3)
                    Cv2.CvtColor(mat, mat, ColorConversionCodes.RGB2BGR);
            }
            else
            {
                throw new Exception($"Unsupported channels ({mat.Channels()}) in {resourceName}");
            }
            return mat;
        }

        // Returns true if found and clicked false otherwise
        internal static bool TryFindAndClick(Mat screen, Mat template, out Point location, float centerOffsetY = 0f)
        {
            location = new Point(-1, -1);

            if (template.Empty() || screen.Empty()) return false;

            using var result = new Mat();
            Cv2.MatchTemplate(screen, template, result, TemplateMatchModes.CCoeffNormed);

            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            const double threshold = 0.80; //<- adjust (0.75–0.85)
            if (maxVal < threshold) return false;

            int centerX = maxLoc.X + template.Width / 2;
            int centerY = maxLoc.Y + (int)(template.Height * (0.5f + centerOffsetY));

            // Borders – Do NOT go over the screen 
            centerX = Math.Clamp(centerX, 0, screen.Width - 1);
            centerY = Math.Clamp(centerY, 0, screen.Height - 1);

            Console.WriteLine($"Found! ({centerX},{centerY})  Confidence: {maxVal:P2}");
            SetCursorPos(centerX, centerY);
            Thread.Sleep(80);
            Hooks.LeftClick();

            location = new Point(centerX, centerY);
            return true;
        }

        // 4 left presses when continue detected
        internal static bool TryFindAndMultiClick(Mat screen, Mat template, int clickCount, int delayMs)
        {
            if (TryFindAndClick(screen, template, out _, 0f))
            {
                for (int i = 1; i < clickCount; i++)
                {
                    Thread.Sleep(delayMs);
                    Hooks.LeftClick();
                }
                return true;
            }
            return false;
        }

        internal static bool IsScreenAlmostBlack(Mat mat)
        {
            if (mat.Empty())
            {
                Console.WriteLine("Black check: empty Mat");
                return false;
            }

            // Expect CV_8UC3 (3 channels, 8-bit unsigned) – most common for color screenshots
            if (mat.Type() != MatType.CV_8UC3)
            {
                Console.WriteLine($"Black check: unsupported type {mat.Type()} (expected CV_8UC3)");
                return false;
            }

            const int tolerance = 6;
            const double blackPixelRatio = 0.998;

            long blackCount = 0;
            long total = (long)mat.Width * mat.Height;

            for (int y = 0; y < mat.Rows; y++)
            {
                for (int x = 0; x < mat.Cols; x++)
                {
                    // .At<Vec3b>(y, x) returns BGR values (Item0=B, Item1=G, Item2=R)
                    Vec3b pixel = mat.At<Vec3b>(y, x);

                    if (pixel.Item2 <= tolerance &&  // R
                        pixel.Item1 <= tolerance &&  // G
                        pixel.Item0 <= tolerance)    // B
                    {
                        blackCount++;
                    }
                }
            }

            double ratio = (double)blackCount / total;
            Console.Write($"Black check: {ratio:P3} ({blackCount}/{total} black pixels) ");
            return ratio >= blackPixelRatio;
        }

        internal static Mat CaptureScreenAsBgrMat()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            // Create Bitmap WITHOUT alpha channel → Format24bppRgb = 3 channels (RGB)
            using var bmp = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            }

            // Convert to Mat → should now be CV_8UC3 (BGR order, as OpenCV expects)
            var mat = BitmapConverter.ToMat(bmp);

            // Safety fallback: if somehow still not 3 channels, force conversion
            if (mat.Channels() != 3)
            {
                Console.WriteLine($"Screenshot unexpectedly has {mat.Channels()} channels – forcing to BGR");
                var converted = new Mat();
                if (mat.Channels() == 4)
                    Cv2.CvtColor(mat, converted, ColorConversionCodes.BGRA2BGR);
                else if (mat.Channels() == 1)
                    Cv2.CvtColor(mat, converted, ColorConversionCodes.GRAY2BGR);
                else
                    throw new Exception($"Unexpected screenshot channels: {mat.Channels()}");

                mat.Dispose();
                mat = converted;
            }
            return mat;
        }

        private static (double, double) GetResolution()
        {
            var screenBounds = Screen.PrimaryScreen.Bounds;
            double scaleX = screenBounds.Width / (double)ConstVars.BASE_WIDTH;
            double scaleY = screenBounds.Height / (double)ConstVars.BASE_HEIGHT;

            Console.WriteLine($"Screen detected: {screenBounds.Width}x{screenBounds.Height}");
            Console.WriteLine($"Scaling templates by {scaleX:0.###}x / {scaleY:0.###}x");

            return (scaleX, scaleY);
        }

        // Generally I am not proud of this method because it disposes the original Mat immediately which is a bit risky,
        // but it does save a lot of memory and I don't want to keep both versions around
        private static Mat ResizeTemplate(Mat original, double scaleX, double scaleY)
        {
            if (Math.Abs(scaleX - 1.0) < 0.01 && Math.Abs(scaleY - 1.0) < 0.01)
                return original;

            int newWidth = (int)(original.Width * scaleX + 0.5);
            int newHeight = (int)(original.Height * scaleY + 0.5);

            var resized = new Mat();

            InterpolationFlags interp;
            if (scaleX < 1.0 || scaleY < 1.0)
            {
                interp = InterpolationFlags.Area;
            }
            else
            {
                interp = InterpolationFlags.Cubic;
            }

            Cv2.Resize(original, resized, new OpenCvSharp.Size(newWidth, newHeight),
                       interpolation: interp);

            original.Dispose();
            return resized;
        }
    }
}
