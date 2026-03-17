using static MacroArcRaiders.ImportsDLL;

namespace MacroArcRaiders
{
    class StaticVars
    {
        internal static bool isRunning = false;           // main toggle
        internal static bool keepProgramAlive = true;     // exit flag
        internal static LowLevelKeyboardProc hookProc;    // prevent GC
        internal static IntPtr hookId = IntPtr.Zero;
        internal static State currentState = State.WaitingForStart;
        internal static bool isStellaMontisClicked = false;
        internal static bool isFreeLoadoutClicked = false;
        internal static uint amountOfCompletedLoops = 0;
        internal static uint amountOfLoops = 0;
        internal static ushort amountOfAttemptToFindSurrender = 0;
    }
}
