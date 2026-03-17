namespace MacroArcRaiders
{
    class ConstVars
    {
        // Resolution templates were made with
        internal const ushort BASE_WIDTH = 2560;
        internal const ushort BASE_HEIGHT = 1440;

        // Keys, mouse events, window messages and hook identifiers
        internal const byte WH_KEYBOARD_LL          = 13;       // Low-level keyboard hook
        internal const byte VK_F1                   = 0x70;     // Start / Pause macro
        internal const byte VK_F2                   = 0x71;     // Reset to stage 1
        internal const byte VK_ESCAPE               = 0x1B;     // Open/close menu
        internal const byte VK_F                    = 0x46;     // Flashlight (dark spawn fix)
        internal const byte VK_W                    = 0x57;     // Move forward (surrender recovery)
        internal const ushort WM_KEYDOWN            = 0x0100;   // Key down message
        internal const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;   // Left mouse down
        internal const uint MOUSEEVENTF_LEFTUP      = 0x0004;   // Left mouse up
        internal const uint WM_QUIT                 = 0x0012;   // Quit message loop

        // Template names
        internal const string TEMPLATE_DIRECTORY = "templates";
        internal const string CONFIRM_TEMPLATE= "Confirm.png";
        internal const string CONTINUE_TEMPLATE = "Continue.png";
        internal const string FREE_LOADOUT_TEMPLATE = "Free_Loadout.png";
        internal const string FREE_LOADOUT_NC_TEMPLATE = "Free_Loadout_NotClicked.png";
        internal const string READY_UP_TEMPLATE = "Ready_Up.png";
        internal const string START_TEMPLATE = "Start.png";
        internal const string STELLA_MONTIS_TEMPLATE = "Stella_Montis.png";
        internal const string STELLA_MONTIS_NC_TEMPLATE = "Stella_Montis_NotClicked.png";
        internal const string SURRENDER_TEMPLATE = "Surrender.png";
        internal const string SURRENDER_BLACK_TEMPLATE = "Surrender_black.png";
        internal const string YES_TEMPLATE = "Yes.png";
    }
}
