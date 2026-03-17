using System.Runtime.InteropServices;
using static MacroArcRaiders.ImportsDLL;

namespace MacroArcRaiders
{
    class Hooks
    {
        internal static void LeftClick()
        {
            mouse_event(ConstVars.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(40);
            mouse_event(ConstVars.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        internal static void PressKey(byte key)
        {
            keybd_event(key, 0, 0, 0);           // down
            Thread.Sleep(50);
            keybd_event(key, 0, 2, 0);           // up
        }

        internal static void HoldKeyFor(byte key, int milliseconds)
        {
            keybd_event(key, 0, 0, UIntPtr.Zero);          // 0 = down
            Thread.Sleep(milliseconds);
            keybd_event(key, 0, 2, UIntPtr.Zero);          // 2 = KEYEVENTF_KEYUP
        }

        internal static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(ConstVars.WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        internal static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)ConstVars.WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == ConstVars.VK_F1)
                {
                    StaticVars.isRunning = !StaticVars.isRunning;
                    Decorator.PrintF1Pressed();

                    return (IntPtr)1;
                }
                else if (vkCode == ConstVars.VK_F2)
                {
                    StaticVars.isRunning = false;

                    StaticVars.currentState = State.WaitingForStart;
                    StaticVars.isStellaMontisClicked = false;
                    StaticVars.amountOfCompletedLoops = 0;

                    Decorator.PrintF2Pressed();

                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(StaticVars.hookId, nCode, wParam, lParam);
        }
    }
}
