using static MacroArcRaiders.ImportsDLL;
using OpenCvSharp;

namespace MacroArcRaiders
{
    class Start
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.Clear();
            Decorator.PrintASCII_Art();
            Validator.CheckInputArgs(ref args); // By ref to save memory
            Decorator.PrintStartInfo();

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                StaticVars.keepProgramAlive = false;
                e.Cancel = true; // Prevent immediate termination
            };

            // Install low-level keyboard hook
            StaticVars.hookProc = Hooks.HookCallback;
            StaticVars.hookId = Hooks.SetHook(StaticVars.hookProc);
            if (StaticVars.hookId == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: Could not install keyboard hook. Exiting.");
                return;
            }

            Dictionary<State, Mat> templates = LoadTemplates();
            if (!MatHandler.CheckResolution(ref templates))
                return;

            // Start bot logic in background thread
            uint threadId = GetCurrentThreadId();
            var workerThread = new Thread(() => BotLoop(templates, StaticVars.amountOfLoops, threadId));
            workerThread.Start();

            // Main thread : blocking message pump
            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
                Thread.Sleep(50);
            }

            // Cleanup
            UnhookWindowsHookEx(StaticVars.hookId);
            foreach (var mat in templates.Values) mat.Dispose();
        }

        private static void BotLoop(Dictionary<State, Mat> templates, uint amountOfLoops, uint mainThreadId)
        {
            while (StaticVars.keepProgramAlive)
            {
                if (!StaticVars.isRunning)
                {
                    Thread.Sleep(200);
                    continue;
                }

                using var screenMat = MatHandler.CaptureScreenAsBgrMat();

                switch (StaticVars.currentState)
                {
                    case State.WaitingForStart:
                        Console.WriteLine(" --- [State 1 - WaitingForStart] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForStart], out _, centerOffsetY: 0))
                        {
                            if (StaticVars.isStellaMontisClicked)
                            {
                                Console.WriteLine("Stella Montis already cliecked. Skipping stage...");
                                StaticVars.currentState = State.WaitingForConfirm;
                            }
                            else
                                StaticVars.currentState = State.WaitingForStellaMontisA;
                        }
                        break;
                    case State.WaitingForStellaMontisA:
                        Console.WriteLine(" --- [State 2A - WaitingForStellaMontis] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForStellaMontisA], out _, centerOffsetY: -0.3f))
                        {
                            StaticVars.isStellaMontisClicked = true;
                            StaticVars.currentState = State.WaitingForConfirm;
                        }
                        else
                            StaticVars.currentState = State.WaitingForStellaMontisB;
                        break;
                    case State.WaitingForStellaMontisB:
                        Console.WriteLine(" --- [State 2B - WaitingForStellaMontis] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForStellaMontisB], out _, centerOffsetY: -0.3f))
                        {
                            StaticVars.isStellaMontisClicked = true;
                            StaticVars.currentState = State.WaitingForConfirm;
                        }
                        else if (!StaticVars.isStellaMontisClicked)
                            StaticVars.currentState = State.WaitingForStellaMontisA;
                        break;
                    case State.WaitingForConfirm:
                        Console.WriteLine(" --- [State 3 - WaitingForConfirm] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForConfirm], out _, centerOffsetY: 0))
                            StaticVars.currentState = State.WaitingForFreeLoadoutA;
                        break;
                    case State.WaitingForFreeLoadoutA:
                        Console.WriteLine(" --- [State 4A - WaitingForFreeLoadout] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForFreeLoadoutA], out _, centerOffsetY: 0))
                        {
                            StaticVars.isFreeLoadoutClicked = true;
                            StaticVars.currentState = State.WaitingForReadyUp;
                        }
                        else
                            StaticVars.currentState = State.WaitingForFreeLoadoutB;
                        break;
                    case State.WaitingForFreeLoadoutB:
                        Console.WriteLine(" --- [State 4B - WaitingForFreeLoadout] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForFreeLoadoutB], out _, centerOffsetY: 0))
                        {
                            StaticVars.isFreeLoadoutClicked = true;
                            StaticVars.currentState = State.WaitingForReadyUp;
                        }
                        else if (!StaticVars.isFreeLoadoutClicked)
                            StaticVars.currentState = State.WaitingForFreeLoadoutA;
                        break;
                    case State.WaitingForReadyUp:
                        StaticVars.isFreeLoadoutClicked = false;
                        Console.WriteLine(" --- [State 5 - WaitingForReadyUp] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForReadyUp], out _, centerOffsetY: 0))
                            StaticVars.currentState = State.WaitingForBlackScreen;
                        break;
                    case State.WaitingForBlackScreen:
                        Console.WriteLine(" --- [State 6 - WaitingForBlackScreen] ---");
                        if (MatHandler.IsScreenAlmostBlack(screenMat))
                        {
                            Console.WriteLine("-> Screen is black – moving to wait section...");
                            StaticVars.currentState = State.WaitingForNonBlackAfterBlackA;
                        }
                        break;
                    case State.WaitingForNonBlackAfterBlackA:
                        Console.WriteLine(" --- [State 7A - WaitingForNonBlackAndSurrender] ---");
                        Console.WriteLine("-> Pressing [ESC]...");

                        Hooks.PressKey(ConstVars.VK_F); // Sometimes it's not too bright and not too dark so pressing F for flashlight seems to fix that
                        Hooks.PressKey(ConstVars.VK_ESCAPE);

                        Thread.Sleep(200);

                        var postEscMatA = MatHandler.CaptureScreenAsBgrMat();
                        if (MatHandler.TryFindAndClick(postEscMatA, templates[State.WaitingForNonBlackAfterBlackA], out _, centerOffsetY: 0))
                        {
                            StaticVars.currentState = State.WaitingForYes;
                        }
                        else if (StaticVars.amountOfAttemptToFindSurrender >= 20)
                        {
                            Console.WriteLine("-> Cannot find surrender for over [20] attempts.");
                            Console.WriteLine("-> Seems like spawn point is not dark/bright enough.");
                            Console.WriteLine("-> Attempting to move in order to fix that.");
                            Hooks.HoldKeyFor(ConstVars.VK_W, 3000); // Walk for 3 seconds forward in hope that it will trigger some difference in brightness that will make surrender easier to find
                            StaticVars.amountOfAttemptToFindSurrender = 0;
                        }
                        else
                        {
                            ++StaticVars.amountOfAttemptToFindSurrender;
                            Console.WriteLine("Surrender NOT found – will retry on next loop");
                            StaticVars.currentState = State.WaitingForNonBlackAfterBlackB;
                        }
                        break;
                    case State.WaitingForNonBlackAfterBlackB:
                        Console.WriteLine(" --- [State 7B - WaitingForNonBlackAndSurrender] ---");
                        Console.WriteLine("-> Pressing [ESC]...");

                        Hooks.PressKey(ConstVars.VK_ESCAPE);

                        Thread.Sleep(200);

                        var postEscMatB = MatHandler.CaptureScreenAsBgrMat();
                        if (MatHandler.TryFindAndClick(postEscMatB, templates[State.WaitingForNonBlackAfterBlackB], out _, centerOffsetY: 0))
                        {
                            StaticVars.currentState = State.WaitingForYes;
                        }
                        else if (StaticVars.amountOfAttemptToFindSurrender > 20)
                        {
                            Console.WriteLine("-> Cannot find surrender for over 20 attempts.");
                            Console.WriteLine("-> Seems like spawn point is not dark/bright enough.");
                            Console.WriteLine("-> Attempting to move in order to fix that.");
                            Hooks.HoldKeyFor(ConstVars.VK_W, 3000); // Walk for 3 seconds forward in hope that it will trigger some difference in brightness that will make surrender easier to find
                            StaticVars.amountOfAttemptToFindSurrender = 0;
                        }
                        else
                        {
                            ++StaticVars.amountOfAttemptToFindSurrender;
                            Console.WriteLine("Surrender NOT found – will retry on next loop");
                            StaticVars.currentState = State.WaitingForNonBlackAfterBlackA;
                        }
                        break;
                    case State.WaitingForYes:
                        Console.WriteLine(" --- [State 8 - WaitingForYes] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForYes], out _, centerOffsetY: 0))
                            StaticVars.currentState = State.WaitingForContinue;
                        break;
                    case State.WaitingForContinue:
                        Console.WriteLine(" --- [State 9 - WaitingForContinue] ---");
                        if (MatHandler.TryFindAndMultiClick(screenMat, templates[State.WaitingForContinue], 6, 50))
                        {
                            Console.WriteLine("Loop ended – Going back to beginning");
                            if (amountOfLoops != 0)
                            {
                                ++StaticVars.amountOfCompletedLoops;
                                Decorator.PrintConfirm(amountOfLoops);
                                if (StaticVars.amountOfCompletedLoops == amountOfLoops)
                                {
                                    Decorator.PrintExit();
                                    StaticVars.keepProgramAlive = false;
                                }
                            }
                            else
                            {
                                ++StaticVars.amountOfCompletedLoops;
                                Decorator.PrintConfirm();
                            }
                            StaticVars.currentState = State.WaitingForStart;
                        }
                        break;
                }
                Thread.Sleep(250);
            }
            // Post WM_QUIT to main thread to exit message loop
            PostThreadMessage(mainThreadId, ConstVars.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
        }

        private static Dictionary<State, Mat> LoadTemplates()
        {
            return new Dictionary<State, Mat>
            {
                { State.WaitingForStart,                MatHandler.LoadAsBgr(ConstVars.START_TEMPLATE) },
                { State.WaitingForStellaMontisA,        MatHandler.LoadAsBgr(ConstVars.STELLA_MONTIS_NC_TEMPLATE) },
                { State.WaitingForStellaMontisB,        MatHandler.LoadAsBgr(ConstVars.STELLA_MONTIS_TEMPLATE) },
                { State.WaitingForConfirm,              MatHandler.LoadAsBgr(ConstVars.CONFIRM_TEMPLATE) },
                { State.WaitingForFreeLoadoutA,         MatHandler.LoadAsBgr(ConstVars.FREE_LOADOUT_NC_TEMPLATE) },
                { State.WaitingForFreeLoadoutB,         MatHandler.LoadAsBgr(ConstVars.FREE_LOADOUT_TEMPLATE) },
                { State.WaitingForReadyUp,              MatHandler.LoadAsBgr(ConstVars.READY_UP_TEMPLATE) },
                { State.WaitingForNonBlackAfterBlackA,  MatHandler.LoadAsBgr(ConstVars.SURRENDER_TEMPLATE) },
                { State.WaitingForNonBlackAfterBlackB,  MatHandler.LoadAsBgr(ConstVars.SURRENDER_BLACK_TEMPLATE) },
                { State.WaitingForYes,                  MatHandler.LoadAsBgr(ConstVars.YES_TEMPLATE) },
                { State.WaitingForContinue,             MatHandler.LoadAsBgr(ConstVars.CONTINUE_TEMPLATE) }
            };
        }
    }
}