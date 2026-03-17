namespace MacroArcRaiders
{
    class Validator
    {
        // Param is optional so void is enough only console output needed
        internal static void CheckInputArgs(ref string[] args)
        {
            if (args.Length == 0)
                return;
            else if (args.Length == 1 && args[0].All(x => char.IsDigit(x)))
            {
                StaticVars.amountOfLoops = uint.Parse(args[0]);
                Console.WriteLine($"Bot will run for [{StaticVars.amountOfLoops}] loops");
            }
            else if (args.Length > 1)
                Console.WriteLine("Provided more than [1] argument! Ignoring...");
            else if (!args[0].All(x => char.IsDigit(x)))
                Console.WriteLine("Provided argument is NOT a number! Ignoring...");
        }
    }
}
