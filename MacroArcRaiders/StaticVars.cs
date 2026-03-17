using static MacroArcRaiders.ImportsDLL;

namespace MacroArcRaiders
{
    class StaticVars
    {
        internal static volatile bool isRunning                 = false;                 // main toggle
        internal static volatile bool keepProgramAlive          = true;                  // exit flag
        internal static volatile State currentState             = State.WaitingForStart;
        internal static IntPtr hookId                           = IntPtr.Zero;
        internal static bool isStellaMontisClicked              = false;
        internal static bool isFreeLoadoutClicked               = false;
        internal static uint amountOfCompletedLoops             = 0;
        internal static uint amountOfLoops                      = 0;
        internal static ushort amountOfAttemptToFindSurrender   = 0;
        internal static ushort amountOfFailedStartAttempts      = 0;
        internal static LowLevelKeyboardProc hookProc;          // prevent GC
    }
}
