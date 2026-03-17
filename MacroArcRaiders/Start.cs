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
                                Console.WriteLine("Stella Montis already clicked. Skipping stage...");
                                StaticVars.currentState = State.WaitingForConfirm;
                            }
                            else
                                StaticVars.currentState = State.WaitingForStellaMontisA;
                        }
                        else if(StaticVars.amountOfFailedStartAttempts > ConstVars.LIMIT_TO_FIND_PLAY) // Probably stuck on round feedback screen
                            StaticVars.currentState = State.SpecialFeedback; // Move to state that will get rid of it
                        else
                            ++StaticVars.amountOfFailedStartAttempts;
                        break;
                    case State.WaitingForStellaMontisA:
                    case State.WaitingForStellaMontisB:
                        HandleStellaMontis(templates, screenMat, StaticVars.currentState);
                        break;
                    case State.WaitingForConfirm:
                        StaticVars.amountOfFailedStartAttempts = 0; // Reset as it found it's way to the next stage
                        Console.WriteLine(" --- [State 3 - WaitingForConfirm] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.WaitingForConfirm], out _, centerOffsetY: 0))
                            StaticVars.currentState = State.WaitingForFreeLoadoutA;
                        break;
                    case State.WaitingForFreeLoadoutA:
                    case State.WaitingForFreeLoadoutB:
                        HandleFreeLoadout(templates, screenMat, StaticVars.currentState);
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
                    case State.WaitingForNonBlackAfterBlackB:
                        HandleSurrenderAttempt(templates, screenMat, StaticVars.currentState);
                        break;
                    case State.WaitingForYes:
                        StaticVars.amountOfAttemptToFindSurrender = 0;
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
                    case State.SpecialFeedback: // Special state that happens from time to time when player is asked to give feedback after round
                        Console.WriteLine($"Detected fail for over [{ConstVars.LIMIT_TO_FIND_PLAY}] moving to special state...");
                        Console.WriteLine(" --- [State SPECIAL - WaitingForSkip] ---");
                        if (MatHandler.TryFindAndClick(screenMat, templates[State.SpecialFeedback], out _, centerOffsetY: 0))
                        {
                            StaticVars.amountOfFailedStartAttempts = 0;
                            StaticVars.currentState = State.WaitingForStart;
                        }
                        break;
                }
                Thread.Sleep(ConstVars.DELAY_BETWEEN_LOOPS_MS);
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
                { State.WaitingForContinue,             MatHandler.LoadAsBgr(ConstVars.CONTINUE_TEMPLATE) },
                { State.SpecialFeedback,                MatHandler.LoadAsBgr(ConstVars.SKIP_TEMPLATE) },    
            };
        }

        private static void HandleStellaMontis(Dictionary<State, Mat> templates, Mat screen, State currentABState)
        {
            Mat template = templates[currentABState];
            State alternate = (currentABState == State.WaitingForStellaMontisA)
                ? State.WaitingForStellaMontisB
                : State.WaitingForStellaMontisA;

            Console.WriteLine($" --- [State 2 - {currentABState}] ---");

            if (MatHandler.TryFindAndClick(screen, template, out _, centerOffsetY: -0.3f))
            {
                StaticVars.isStellaMontisClicked = true;
                StaticVars.currentState = State.WaitingForConfirm;
            }
            else
            {
                StaticVars.currentState = alternate;
            }
        }

        private static void HandleFreeLoadout(Dictionary<State, Mat> templates, Mat screen, State currentABState)
        {
            Mat template = templates[currentABState];
            State alternate = (currentABState == State.WaitingForFreeLoadoutA)
                ? State.WaitingForFreeLoadoutB
                : State.WaitingForFreeLoadoutA;

            Console.WriteLine($" --- [State 4 - {currentABState}] ---");

            if (MatHandler.TryFindAndClick(screen, template, out _, centerOffsetY: 0))
            {
                StaticVars.isFreeLoadoutClicked = true;
                StaticVars.currentState = State.WaitingForReadyUp;
            }
            else
            {
                StaticVars.currentState = alternate;
            }
        }

        private static void HandleSurrenderAttempt(Dictionary<State, Mat> templates, Mat screen, State currentABState)
        {
            bool isA = currentABState == State.WaitingForNonBlackAfterBlackA;

            Console.WriteLine($" --- [State 7 - {currentABState}] ---");
            Console.WriteLine("-> Pressing [ESC]...");

            if (isA)
            {
                Hooks.PressKey(ConstVars.VK_F); // flashlight only in A
            }

            Hooks.PressKey(ConstVars.VK_ESCAPE);
            Thread.Sleep(ConstVars.DELAY_FOR_MENUS_MS);

            using var postEscMat = MatHandler.CaptureScreenAsBgrMat();

            Mat template = templates[currentABState];
            State nextFailState = isA ? State.WaitingForNonBlackAfterBlackB : State.WaitingForNonBlackAfterBlackA;

            if (MatHandler.TryFindAndClick(postEscMat, template, out _, centerOffsetY: 0))
            {
                StaticVars.currentState = State.WaitingForYes;
                return;
            }

            if (StaticVars.amountOfAttemptToFindSurrender >= ConstVars.LIMIT_TO_FIND_SURRENDER)
            {
                Console.WriteLine($"-> Cannot find surrender for over [{ConstVars.LIMIT_TO_FIND_SURRENDER}] attempts.");
                Console.WriteLine("-> Seems like spawn point is not dark/bright enough.");
                Console.WriteLine("-> Attempting to move in order to fix that.");
                Hooks.HoldKeyFor(ConstVars.VK_W, ConstVars.FOR_HOW_LONG_TO_MOVE_MS);
                StaticVars.amountOfAttemptToFindSurrender = 0;
            }
            else
            {
                ++StaticVars.amountOfAttemptToFindSurrender;
                Console.WriteLine("Surrender NOT found – will retry on next loop");
                StaticVars.currentState = nextFailState;
            }
        }
    }
}