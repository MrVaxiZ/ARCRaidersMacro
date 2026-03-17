namespace MacroArcRaiders
{
    static class Decorator
    {
        internal static void PrintASCII_Art() 
        {
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
        }

        internal static void PrintStartInfo()
        {
            Console.WriteLine("Bot ready. Press [F1] to START / PAUSE the macro");
            Console.WriteLine("You can also reset macro back to stage 1 with [F2]");
            Console.WriteLine("Press [Ctrl+C] to completely exit");
            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("REMEMBER! Start it when you have focus on the game and leave it like that!");
            Console.WriteLine("If you want to [Alt+Tab] to something pause it first.");
            Console.WriteLine("-----------------------------------------------");
        }

        internal static void PrintF1Pressed()
        {
            Console.ForegroundColor = StaticVars.isRunning ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine(StaticVars.isRunning ? "\nMACRO STARTED" : "\nMACRO PAUSED");
            Console.ResetColor();
        }

        internal static void PrintF2Pressed()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\nMACRO RESET -> BACK TO STAGE [1] (WaitingForStart)");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Bot ready. Press [F1] to START / PAUSE the macro");
            Console.ResetColor();
        }

        internal static void PrintExit()
        {
            Console.WriteLine("Everything Done! \\[^_^]/");
            Console.WriteLine("Program will close in [3]");
            Thread.Sleep(1000);
            Console.WriteLine("Program will close in [2]");
            Thread.Sleep(1000);
            Console.WriteLine("Program will close in [1]");
        }

        internal static void PrintConfirm()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n---------------------------------------------------------------");
            Console.WriteLine($"    ---[ COMPLETED [{StaticVars.amountOfCompletedLoops}] LOOPS ]---");
            Console.WriteLine("---------------------------------------------------------------\n");
            Console.ResetColor();
        }
        internal static void PrintConfirm(uint amountOfLoops)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n---------------------------------------------------------------");
            Console.WriteLine($"    ---[ COMPLETED [{StaticVars.amountOfCompletedLoops}/{amountOfLoops}] LOOPS ]---");
            Console.WriteLine("---------------------------------------------------------------\n");
            Console.ResetColor();
        }
    }
}
