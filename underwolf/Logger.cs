using System;

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
