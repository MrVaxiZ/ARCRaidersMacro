using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Reflection;
using System.Runtime.InteropServices;
using Point = OpenCvSharp.Point;

class Program
{
    // resolution templates were made with
    private const int BASE_WIDTH = 2560;
    private const int BASE_HEIGHT = 1440;

    private static bool isRunning = false;           // main toggle
    private static bool keepProgramAlive = true;     // exit flag
    private static LowLevelKeyboardProc _hookProc;   // prevent GC
    private static IntPtr _hookId = IntPtr.Zero;

    private const short WH_KEYBOARD_LL = 13;
    private const short WM_KEYDOWN = 0x0100;
    private const byte VK_F1 = 0x70;
    private const byte VK_F2 = 0x71;
    private const byte VK_ESCAPE = 0x1B; // For menu
    private const byte VK_F = 0x46; // For flashlight
    private const byte VK_W = 0x57; // In case if surreder was not found for a long time it moves charackter forward in hope that it will trigger some difference in brightness that will make surrender easier to find

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint WM_QUIT = 0x0012;

    private static State currentState = State.WaitingForStart;
    private static bool isStellaMontisClicked = false;
    private static bool isFreeLoadoutClicked = false;
    private static uint amountOfCompletedLoops = 0;
    private static ushort amountOfAttemptToFindSurrender = 0;

    private enum State
    {
        WaitingForStart,
        WaitingForStellaMontisA,
        WaitingForStellaMontisB,
        WaitingForConfirm,
        WaitingForFreeLoadoutA,
        WaitingForFreeLoadoutB,
        WaitingForReadyUp,
        WaitingForBlackScreen,
        WaitingForNonBlackAfterBlackA,
        WaitingForNonBlackAfterBlackB,
        WaitingForSurrender,
        WaitingForYes,
        WaitingForContinue
    }

    [STAThread]
    public static void Main(string[] args)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Magenta;

        string[] asciiART =
        {
            "\n",
            "+---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+\n",
            @"   ___  ______  _____ ___  ___                        _              _   _            _  ______",
            @"  / _ \ | ___ \/  __ \|  \/  |                       | |            | | | |          (_)|___  /",
            @" / /_\ \| |_/ /| /  \/| .  . | __ _  ___ _ __ ___    | |__  _   _   | | | | __ ___  ___    / / ",
            @" |  _  ||    / | |    | |\/| |/ _` |/ __| '__/ _ \   | '_ \| | | |  | | | |/ _` \ \/ / |  / /  ",
            @" | | | || |\ \ | \__/\| |  | | (_| | (__| | | (_) |  | |_) | |_| |  \ \_/ / (_| |>  <| |./ /___",
            @" \_| |_/\_| \_| \____/\_|  |_/\__,_|\___|_|  \___/   |_.__/ \__, |   \___/ \__,_/_/\_\_|\_____/",
            @"                                                             __/ |                            ",
            @"                                                            |___/                             ",
            "+---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+\n",
        };

        foreach (string ascii in asciiART)
        {
            Console.WriteLine(ascii);
        }

        Console.ResetColor();

        uint amountOfLoops = 0;
        if (args.Length == 1 && args[0].All(x => char.IsDigit(x)))
        {
            amountOfLoops = uint.Parse(args[0]);
            Console.WriteLine($"Bot will run for [{amountOfLoops}] loops");
        }

        Console.WriteLine("Bot ready. Press [F1] to START / PAUSE the macro");
        Console.WriteLine("You can also reset macro back to stage 1 with [F2]");
        Console.WriteLine("Press [Ctrl+C] to completely exit");
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("REMEMBER! Start it when you have focus on the game and leave it like that!");
        Console.WriteLine("If you want to [Alt+Tab] to something pause it first.");
        Console.WriteLine("-----------------------------------------------");

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (sender, e) =>
        {
            keepProgramAlive = false;
            e.Cancel = true; // Prevent immediate termination
        };

        // Install global low-level keyboard hook
        _hookProc = HookCallback;
        _hookId = SetHook(_hookProc);
        if (_hookId == IntPtr.Zero)
        {
            Console.WriteLine("ERROR: Could not install keyboard hook. Exiting.");
            return;
        }

        var templates = new Dictionary<State, Mat>
        {
            { State.WaitingForStart,         LoadAsBgr("Start.png") },
            { State.WaitingForStellaMontisA, LoadAsBgr("Stella_Montis_NotClicked.png") },
            { State.WaitingForStellaMontisB, LoadAsBgr("Stella_Montis.png") },
            { State.WaitingForConfirm,       LoadAsBgr("Confirm.png") },
            { State.WaitingForFreeLoadoutA,  LoadAsBgr("Free_Loadout_NotClicked.png") },
            { State.WaitingForFreeLoadoutB,  LoadAsBgr("Free_Loadout.png") },
            { State.WaitingForReadyUp,       LoadAsBgr("Ready_Up.png") },
            { State.WaitingForYes,           LoadAsBgr("Yes.png") },
            { State.WaitingForContinue,      LoadAsBgr("Continue.png") }
        };
        var surrenderTemplateA = LoadAsBgr("Surrender.png");
        var surrenderTemplateB = LoadAsBgr("Surrender_black.png");

        uint mainThreadId = GetCurrentThreadId();

        var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        double scaleX = screenBounds.Width / (double)BASE_WIDTH;
        double scaleY = screenBounds.Height / (double)BASE_HEIGHT;

        Console.WriteLine($"Screen detected: {screenBounds.Width}x{screenBounds.Height}");
        Console.WriteLine($"Scaling templates by {scaleX:0.###}x / {scaleY:0.###}x");

        if (Math.Abs(scaleX - 1.0) < 0.01 && Math.Abs(scaleY - 1.0) < 0.01)
        {
            Console.WriteLine("-> Running at native 1440p – no scaling needed");
        }
        else
        {
            // Scale every template (including surrender)
            foreach (var kvp in templates)
            {
                templates[kvp.Key] = ResizeTemplate(kvp.Value, scaleX, scaleY);
            }
            surrenderTemplateA = ResizeTemplate(surrenderTemplateA, scaleX, scaleY);
            surrenderTemplateB = ResizeTemplate(surrenderTemplateB, scaleX, scaleY);
        }

        // Start bot logic in background thread
        var workerThread = new Thread(() => BotLoop(templates, surrenderTemplateA, surrenderTemplateB, amountOfLoops, mainThreadId));
        workerThread.Start();

        // Main thread: blocking message pump
        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
            Thread.Sleep(50);
        }

        // Cleanup
        UnhookWindowsHookEx(_hookId);

        Console.WriteLine("Exiting...");

        // Dispose templates
        foreach (var mat in templates.Values) mat.Dispose();
        surrenderTemplateA.Dispose();
        surrenderTemplateB.Dispose();
    }

    private static void BotLoop(Dictionary<State, Mat> templates, Mat surrenderTemplateA, Mat surrenderTemplateB, uint amountOfLoops, uint mainThreadId)
    {
        while (keepProgramAlive)
        {
            if (!isRunning)
            {
                Thread.Sleep(200);
                continue;
            }
            using var screenMat = CaptureScreenAsBgrMat();
            switch (currentState)
            {
                case State.WaitingForStart:
                    Console.WriteLine(" --- [State 1 - WaitingForStart] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForStart], out _, centerOffsetY: 0))
                        currentState = State.WaitingForStellaMontisA;
                    break;
                case State.WaitingForStellaMontisA:
                    Console.WriteLine(" --- [State 2A - WaitingForStellaMontis] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForStellaMontisA], out _, centerOffsetY: -0.3f))
                    {
                        isStellaMontisClicked = true;
                        currentState = State.WaitingForConfirm;
                    }
                    else
                        currentState = State.WaitingForStellaMontisB;
                    break;
                case State.WaitingForStellaMontisB:
                    Console.WriteLine(" --- [State 2B - WaitingForStellaMontis] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForStellaMontisB], out _, centerOffsetY: -0.3f))
                    {
                        isStellaMontisClicked = true;
                        currentState = State.WaitingForConfirm;
                    }
                    else if(!isStellaMontisClicked)
                        currentState = State.WaitingForStellaMontisA;
                    break;
                case State.WaitingForConfirm:
                    Console.WriteLine(" --- [State 3 - WaitingForConfirm] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForConfirm], out _, centerOffsetY: 0))
                        currentState = State.WaitingForFreeLoadoutA;
                    break;
                case State.WaitingForFreeLoadoutA:
                    Console.WriteLine(" --- [State 4A - WaitingForFreeLoadout] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForFreeLoadoutA], out _, centerOffsetY: 0))
                    {
                        isFreeLoadoutClicked = true;
                        currentState = State.WaitingForReadyUp;
                    }
                    else
                        currentState = State.WaitingForFreeLoadoutB;
                    break;
                case State.WaitingForFreeLoadoutB:
                    Console.WriteLine(" --- [State 4B - WaitingForFreeLoadout] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForFreeLoadoutB], out _, centerOffsetY: 0))
                    {
                        isFreeLoadoutClicked = true;
                        currentState = State.WaitingForReadyUp;
                    }
                    else if (!isFreeLoadoutClicked)
                        currentState = State.WaitingForFreeLoadoutA;
                    break;
                case State.WaitingForReadyUp:
                    isFreeLoadoutClicked = false;
                    Console.WriteLine(" --- [State 5 - WaitingForReadyUp] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForReadyUp], out _, centerOffsetY: 0))
                        currentState = State.WaitingForBlackScreen;
                    break;
                case State.WaitingForBlackScreen:
                    Console.WriteLine(" --- [State 6 - WaitingForBlackScreen] ---");
                    if (IsScreenAlmostBlack(screenMat))
                    {
                        Console.WriteLine("-> Screen is black – moving to wait section...");
                        currentState = State.WaitingForNonBlackAfterBlackA;
                    }
                    break;
                case State.WaitingForNonBlackAfterBlackA:
                    Console.WriteLine(" --- [State 7A - WaitingForNonBlackAndSurrender] ---");
                    Console.WriteLine("-> Pressing [ESC]...");

                    PressKey(VK_F); // Sometimes it's not too bright and not too dark so pressing F for flashlight seems to fix that
                    PressKey(VK_ESCAPE);

                    Thread.Sleep(200);

                    var postEscMatA = CaptureScreenAsBgrMat();
                    if (TryFindAndClick(postEscMatA, surrenderTemplateA, out _, centerOffsetY: 0))
                    {
                        currentState = State.WaitingForYes;
                    }
                    else if(amountOfAttemptToFindSurrender >= 20)
                    {
                        Console.WriteLine("-> Cannot find surrender for over 20 attempts.");
                        Console.WriteLine("-> Seems like spawn point is not dark/bright enough.");
                        Console.WriteLine("-> Attempting to move in order to fix that.");
                        HoldKeyFor(VK_W, 3000); // Walk for 3 seconds forward in hope that it will trigger some difference in brightness that will make surrender easier to find
                        amountOfAttemptToFindSurrender = 0;
                    }
                    else
                    {
                        ++amountOfAttemptToFindSurrender;
                        Console.WriteLine("Surrender NOT found – will retry on next loop");
                        currentState = State.WaitingForNonBlackAfterBlackB;
                    }
                    break;
                case State.WaitingForNonBlackAfterBlackB:
                    Console.WriteLine(" --- [State 7B - WaitingForNonBlackAndSurrender] ---");
                    Console.WriteLine("-> Pressing [ESC]...");

                    PressKey(VK_ESCAPE);

                    Thread.Sleep(200);

                    var postEscMatB = CaptureScreenAsBgrMat();
                    if (TryFindAndClick(postEscMatB, surrenderTemplateB, out _, centerOffsetY: 0))
                    {
                        currentState = State.WaitingForYes;
                    }
                    else if(amountOfAttemptToFindSurrender > 20)
                    {
                        Console.WriteLine("-> Cannot find surrender for over 20 attempts.");
                        Console.WriteLine("-> Seems like spawn point is not dark/bright enough.");
                        Console.WriteLine("-> Attempting to move in order to fix that.");
                        HoldKeyFor(VK_W, 3000); // Walk for 3 seconds forward in hope that it will trigger some difference in brightness that will make surrender easier to find
                        amountOfAttemptToFindSurrender = 0;
                    }
                    else
                    {
                        ++amountOfAttemptToFindSurrender;
                        Console.WriteLine("Surrender NOT found – will retry on next loop");
                        currentState = State.WaitingForNonBlackAfterBlackA;
                    }
                    break;
                case State.WaitingForYes:
                    Console.WriteLine(" --- [State 8 - WaitingForYes] ---");
                    if (TryFindAndClick(screenMat, templates[State.WaitingForYes], out _, centerOffsetY: 0))
                        currentState = State.WaitingForContinue;
                    break;
                case State.WaitingForContinue:
                    Console.WriteLine(" --- [State 9 - WaitingForContinue] ---");
                    if (TryFindAndMultiClick(screenMat, templates[State.WaitingForContinue], 6, 50))
                    {
                        Console.WriteLine("Loop ended – Going back to beginning");
                        if (amountOfLoops != 0)
                        {
                            ++amountOfCompletedLoops;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n---------------------------------------------------------------");
                            Console.WriteLine($"    ---[ COMPLETED [{amountOfCompletedLoops}/{amountOfLoops}] LOOPS ]---");
                            Console.WriteLine("---------------------------------------------------------------\n");
                            Console.ResetColor();
                            if (amountOfCompletedLoops == amountOfLoops)
                            {
                                Console.WriteLine("Everything Done! \\[^_^]/");
                                Console.WriteLine("Program will close in [3]");
                                Thread.Sleep(1000);
                                Console.WriteLine("Program will close in [2]");
                                Thread.Sleep(1000);
                                Console.WriteLine("Program will close in [1]");
                                keepProgramAlive = false;
                            }
                        }
                        else
                        {
                            ++amountOfCompletedLoops;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n---------------------------------------------------------------");
                            Console.WriteLine($"    ---[ COMPLETED [{amountOfCompletedLoops}] LOOPS ]---");
                            Console.WriteLine("---------------------------------------------------------------\n");
                            Console.ResetColor();
                        }
                        currentState = State.WaitingForStart;
                    }
                    break;
            }
            Thread.Sleep(500);
        }
        // Post WM_QUIT to main thread to exit message loop
        PostThreadMessage(mainThreadId, WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == VK_F1)
            {
                isRunning = !isRunning;
                Console.ForegroundColor = isRunning ? ConsoleColor.Green : ConsoleColor.Yellow;
                Console.WriteLine(isRunning ? "\nMACRO STARTED" : "\nMACRO PAUSED");
                Console.ResetColor();

                return (IntPtr)1;
            }
            else if(vkCode == VK_F2)
            {
                isRunning = false;

                currentState = State.WaitingForStart;
                isStellaMontisClicked = false;
                amountOfCompletedLoops = 0;

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("\nMACRO RESET -> BACK TO STAGE [1] (WaitingForStart)");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Bot ready. Press [F1] to START / PAUSE the macro");
                Console.ResetColor();

                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // Helper method to resize templates based on screen scaling – uses InterArea for best results on downscaling
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

    // Helper method – always returns 3-channel BGR Mat or throws
    private static Mat LoadAsBgr(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"MacroArcRaiders.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
            throw new Exception($"Cannot find embedded resource: {fullResourceName}. Ensure it's marked as 'Embedded Resource' and the name/namespace matches.");

        // Read stream to byte array
        byte[] bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);

        // Load from bytes (ImDecode handles any format)
        var mat = Cv2.ImDecode(bytes, ImreadModes.AnyColor);
        if (mat.Empty())
            throw new Exception($"Cannot load template from resource: {resourceName}");

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
    private static bool TryFindAndClick(Mat screen, Mat template, out Point location, float centerOffsetY = 0f)
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
        LeftClick();

        location = new Point(centerX, centerY);
        return true;
    }

    // 4 left presses when continue detected
    private static bool TryFindAndMultiClick(Mat screen, Mat template, int clickCount, int delayMs)
    {
        if (TryFindAndClick(screen, template, out _, 0f))
        {
            for (int i = 1; i < clickCount; i++)
            {
                Thread.Sleep(delayMs);
                LeftClick();
            }
            return true;
        }
        return false;
    }

    private static bool IsScreenAlmostBlack(Mat mat)
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

    private static Mat CaptureScreenAsBgrMat()
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

    private static void LeftClick()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(40);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    private static void PressKey(byte key)
    {
        keybd_event(key, 0, 0, 0);           // down
        Thread.Sleep(50);
        keybd_event(key, 0, 2, 0);           // up
    }

    private static void HoldKeyFor(byte key, int milliseconds)
    {
        keybd_event(key, 0, 0, UIntPtr.Zero);          // 0 = down

        Thread.Sleep(milliseconds);

        keybd_event(key, 0, 2, UIntPtr.Zero);          // 2 = KEYEVENTF_KEYUP
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, UIntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
}