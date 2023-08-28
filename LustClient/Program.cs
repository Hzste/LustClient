using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Memory;
using System.Globalization;

namespace LustClient
{
    public class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private static readonly IntPtr handle = GetConsoleWindow();
        private static readonly Mem memory = new Mem();
        private static bool hidden = false;
        private static string version = "1.0";
        private static string prefix = ">> ";

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public static bool KeyPressed(int vk)
        {
            return (GetAsyncKeyState(vk) & 32768) != 0;
        }

        public static void Main()
        {
            /*Console.Write("Enter Password: ");
            string password = Console.ReadLine();
            while (password != "lust")
            {
                Console.Write("Enter Password: ");
                password = Console.ReadLine();
            }
            Console.Clear();*/
            Init();
            Thread WThread = new Thread(WindowThread);
            WThread.Start();
            Thread EThread = new Thread(EjectThread);
            EThread.Start();

            int count = 3;
            while (count >= 0 && !memory.OpenProcess("Minecraft.Windows"))
            {
                if (count == 0)
                {
                    Thread.Sleep(1000);
                    if (memory.OpenProcess("Minecraft.Windows"))
                    {
                        break;
                    }
                    else
                    {
                        count = 3;
                    }
                }
                Console.WriteLine($">> Injection Failed - Minecraft Not open. Trying again in {count--} seconds...");
                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
                Thread.Sleep(1000);
            }

            if (!memory.OpenProcess("Minecraft.Windows"))
            {
                Console.WriteLine(prefix + "Injection failed.");
                return;
            }

            Console.WriteLine(prefix + "Injection Success - Minecraft found.");
            Console.WriteLine(prefix + "Type 'help' for a list of commands");
            Console.WriteLine(prefix + "Use CTRL + L to eject the client.\n");
            Commands();
        }

        private static bool wasEscapePressed = false;
        private static bool wasCtrlLPressed = false;

        private static void WindowThread()
        {
            while (true)
            {
                bool isEscapePressed = KeyPressed(0x71);

                if (isEscapePressed && !wasEscapePressed)
                {
                    if (!hidden)
                    {
                        hidden = true;
                        ShowWindow(handle, SW_HIDE);
                    }
                    else
                    {
                        hidden = false;
                        ShowWindow(handle, SW_SHOW);
                    }
                }

                wasEscapePressed = isEscapePressed;
            }
        }

        private static void EjectThread()
        {
            while (true)
            {
                bool isCtrlPressed = KeyPressed(0x11);
                bool isLPressed = KeyPressed(0x4C);

                if (isCtrlPressed && isLPressed && !wasCtrlLPressed)
                {
                    reset();
                    Environment.Exit(0);
                }

                wasCtrlLPressed = isCtrlPressed && isLPressed;
            }
        }

        private static void Init()
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Title = "Lust Client";
            Console.Write("           __               __     _________            __ \r\n          / /   __  _______/ /_   / ____/ (_)__  ____  / /_\r\n         / /   / / / / ___/ __/  / /   / / / _ \\/ __ \\/ __/\r\n        / /___/ /_/ (__  ) /_   / /___/ / /  __/ / / / /_  \r\n       /_____/\\__,_/____/\\__/   \\____/_/_/\\___/_/ /_/\\__/  \r\n                                                           ");
            Console.WriteLine();
            Console.WriteLine(prefix + "Injecting Lust v" + version);
            Console.WriteLine();
            Thread.Sleep(1000);
        }
        private static void Commands()
        {
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();

                // Split input into arguments
                string[] args = input.Split(' ');

                switch (args[0].ToLower())
                {
                    case "help":
                        Console.WriteLine(prefix + "Available commands:");
                        Console.WriteLine(prefix + "help - Shows this help message");
                        Console.WriteLine(prefix + "reach [distance] - Sets your reach distance (3.0 - 7.0)");
                        Console.WriteLine(prefix + "timer [value] - Sets the ticks per second (0.5 - 3.0)");
                        Console.WriteLine(prefix + "reset - Resets all modules to default");
                        Console.WriteLine(prefix + "eject - Ejects the client");
                        break;
                    case "reach":
                        if (args.Length < 2)
                        {
                            Console.WriteLine(prefix + "Usage: reach [distance]");
                        }
                        else if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float distance) || distance < 3.0f || distance > 7.0f)
                        {
                            Console.WriteLine(prefix + "Invalid distance value. Must be between 3.0 and 7.0");
                        }
                        else
                        {
                            distance = (float)Math.Round(distance, 1);
                            setReach(distance);
                            Console.WriteLine(prefix + $"Reach set to {distance}");
                        }
                        break;
                    case "timer":
                        if (args.Length < 2)
                        {
                            Console.WriteLine(prefix + "Usage: timer [value]");
                        }
                        else if (!float.TryParse(args[1], out float timer) || timer < 0.5f || timer > 30000.0f)
                        {
                            Console.WriteLine(prefix + "Invalid timer value. Must be between 0.5 and 3.0");
                        }
                        else
                        {
                            timer = (float)Math.Round(timer, 2);
                            setTimer(timer);
                            Console.WriteLine(prefix + $"Timer set to {timer}");
                        }
                        break;
                    case "reset":
                        reset();
                        Console.WriteLine(prefix + "All modules have been reset");
                        break;
                    case "eject":
                        reset();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine(prefix + "Invalid command. Type 'help' for a list of commands");
                        break;
                }
            }
        }


        private static void setReach(float reachValue)
        {
            memory.WriteMemory("Minecraft.Windows.exe+0x4174AC8", "float", reachValue.ToString());
        }

        private static void setTimer(float timerValue)
        {
            memory.WriteMemory("Minecraft.Windows.exe+0x4174B98", "double", timerValue.ToString());
        }

        private static void reset()
        {
            memory.WriteMemory("Minecraft.Windows.exe+0x4174AC8", "float", "3");
            memory.WriteMemory("Minecraft.Windows.exe+0x4174B98", "double", "1000");
        }

    }
}

