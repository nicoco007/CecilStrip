﻿using System;

namespace DllStrip
{
    static class Logger
    {
        public static int Verbosity = 0;

        public static void Trace(object obj, int verbosity)
        {
            if (Verbosity >= verbosity)
            {
                WriteLine(obj, "  TRACE", ConsoleColor.DarkGray);
            }
        }

        public static void Info(object obj)    => WriteLine(obj, "   INFO", ConsoleColor.DarkGreen);
        public static void Warning(object obj) => WriteLine(obj, "WARNING", ConsoleColor.Yellow);
        public static void Error(object obj)   => WriteLine(obj, "  ERROR", ConsoleColor.Red);

        public static void WriteLine(object obj, string prefix, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            
            foreach (string line in obj?.ToString().Split("\n"))
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {prefix} | {line}");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
