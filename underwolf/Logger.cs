using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace underwolf {
    internal class Logger {

        public string Title { get; }

        public Logger(string title ) {
            Title = title;
        }

        public void Info(string? message) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine( $"[{Title}]: {message}" );
            Console.ResetColor();
        }

        public void Warn(string? message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine( $"[{Title}]: {message}" );
            Console.ResetColor();
        }

        public void Error(string? message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( $"[{Title}]: {message}" );
            Console.ResetColor();
        }

    }
}
