# MacroArcRaiders

I know it does not work after the latest patch.
However I am too lazy to fix it so either do it yourself or you will have to wait.

Simple automated surrender macro for Arc Raiders.

## How to Run It?

You can run the macro in two ways:

### 1. Double-click `MacroArcRaiders.exe`
- The macro will run **indefinitely** until you stop it.
- Press **[Ctrl+C]** in the console window to fully exit.

### 2. Use `RunExactAmountOfTimes.cmd`
- This allows you to set a specific number of surrender loops.
- Double-click the `.cmd` file to start.
- You can edit the number (e.g. change `8` to `20`) by opening the file in Notepad.

After reaching the set number of loops, the program will automatically close.

---

## How to Operate It?

1. Launch the game and go to the **main menu** (where the big **PLAY** button is visible).
2. Start the macro by double-clicking `MacroArcRaiders.exe` (or the .cmd file).
3. Once the console says *"Bot ready..."*, press **[F1]** while the game window is focused.
4. The macro will now automatically play → surrender → repeat.

### Keyboard Controls

| Key   | Action |
|-------|--------|
| **F1**    | Start / Pause the macro |
| **F2**    | Reset macro back to stage 1 (WaitingForStart) |
| **Ctrl+C** | Fully exit the program |

---

## Important Notes & Tips

- Your game has to be in **English** language
- Always start the macro **only when you are in the main menu** and the game window has focus.
- If you want to Alt+Tab or do something else, **pause the macro first** by pressing F1.
- The macro now has improved handling for dark spawns:
  - It tries two different surrender templates (`Surrender.png` and `Surrender_black.png`).
  - It presses **F** (flashlight) on the first attempt to improve visibility.
  - If it fails to find the surrender button for more than 20 attempts, it will automatically walk forward for 3 seconds using **W** to change the lighting/position and retry.
- Template matching is **resolution independent** — it automatically scales templates to your current screen resolution. (If macro does not work for you this is probably why please so make an issue for it or write to me about what resolution you are using as I did make dynamic scaling but it's not perfect.)

---

## Common Issues

### I don't see the .exe file / Windows Defender says it's a virus
This is normal for unsigned .exe files.  
**Solution**: Add the file as an exception in Windows Defender (or mark it as "Allow on this device" or press "Run it anyways").

### Macro doesn't click / low confidence
Make sure:
- The game is in **focus**
- You're starting from the correct menu screen
- Your resolution is 16:9 (best compatibility)****

---

**Enjoy the farming!** 🚀
