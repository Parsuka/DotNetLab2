using System;
using ATMLibrary;

namespace ATMConsole
{
    /// <summary>
    /// Власні обробники подій банкомата для консолі (Console.WriteLine).
    /// </summary>
    public class ConsoleNotifier
    {
        public void Subscribe(AutomatedTellerMachine atm)
        {
            atm.CardInserted += OnInfo;
            atm.Authenticated += OnAuthenticated;
            atm.BalanceChecked += OnBalanceChecked;
            atm.Withdrawn += OnWithdrawn;
            atm.Deposited += OnDeposited;
            atm.Transferred += OnTransferred;
            atm.OperationFailed += OnOperationFailed;
            atm.SessionEnded += OnInfo;
        }

        public void Unsubscribe(AutomatedTellerMachine atm)
        {
            atm.CardInserted -= OnInfo;
            atm.Authenticated -= OnAuthenticated;
            atm.BalanceChecked -= OnBalanceChecked;
            atm.Withdrawn -= OnWithdrawn;
            atm.Deposited -= OnDeposited;
            atm.Transferred -= OnTransferred;
            atm.OperationFailed -= OnOperationFailed;
            atm.SessionEnded -= OnInfo;
        }

        private void OnAuthenticated(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Green, "[OK] " + e.Message);
        }

        private void OnBalanceChecked(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Cyan, "[БАЛАНС] " + e.Message);
        }

        private void OnWithdrawn(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Green, "[$] " + e.Message + ". Заберіть готівку.");
        }

        private void OnDeposited(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Green, "[+] " + e.Message);
        }

        private void OnTransferred(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Green, "[->] " + e.Message);
        }

        private void OnOperationFailed(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Red, "[ПОМИЛКА] " + AtmMessageFormatter.OperationName(e.Operation) + ": " + e.Message);
        }

        private void OnInfo(object sender, AtmEventArgs e)
        {
            Write(ConsoleColor.Yellow, "[i] " + e.Message);
        }

        private static void Write(ConsoleColor color, string message)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = old;
        }
    }
}
