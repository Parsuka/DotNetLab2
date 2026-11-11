using System;
using System.IO;
using System.Text;
using ATMLibrary;

namespace ATMConsole
{
    internal class Program
    {
        private static Bank bank;
        private static AutomatedTellerMachine atm;
        private static readonly ConsoleNotifier notifier = new ConsoleNotifier();
        private static bool journalEnabled;
        private static readonly string journalPath = Path.Combine(AppContext.BaseDirectory, "atm_journal.txt");

        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Банкомат — консольна версія";

            bank = DemoBankFactory.Create();
            atm = bank.Atms[0];

            // Підписка власних обробників (Console.WriteLine)
            notifier.Subscribe(atm);

            // Анонімний метод (лямбда) як обробник події
            atm.Withdrawn += (sender, e) =>
            {
                var machine = (AutomatedTellerMachine)sender;
                if (machine.CashAmount < 5000)
                    Console.WriteLine("   (службове) У банкоматі залишилось мало готівки: " + Money.Format(machine.CashAmount));
            };

            // Електронна пошта — лише якщо задано змінні середовища
            EmailNotifier email = EmailNotifier.FromEnvironment();
            if (email != null)
            {
                email.StatusChanged += message => Console.WriteLine("   [e-mail] " + message);
                email.Subscribe(atm);
            }

            while (true)
            {
                PrintHeader();
                if (!LoginScreen())
                    break;
                MainMenu();
            }

            Console.WriteLine("\nРоботу завершено. До побачення!");
        }

        // ---------- Екран вставлення картки ----------

        private static bool LoginScreen()
        {
            Console.WriteLine("Тестові картки:");
            foreach (Account a in bank.Accounts)
            {
                string pin;
                DemoBankFactory.PinHints.TryGetValue(a.CardNumber, out pin);
                Console.WriteLine("   " + a.FormattedCardNumber + "  PIN " + pin + "  " + a.OwnerName +
                                  (a.IsBlocked ? "  [заблоковано]" : ""));
            }
            Console.WriteLine();

            while (true)
            {
                Console.Write("Введіть номер картки (або 0 — вихід): ");
                string card = Console.ReadLine();
                if (card == null || card.Trim() == "0")
                    return false;

                if (!atm.InsertCard(card))
                    continue;

                // До 3 спроб введення PIN
                while (atm.IsCardInserted && !atm.IsAuthenticated)
                {
                    Console.Write("Введіть PIN-код: ");
                    string pin = ReadHidden();
                    if (pin == null)
                        return false;
                    atm.EnterPin(pin);
                }

                if (atm.IsAuthenticated)
                    return true;
            }
        }

        // ---------- Головне меню ----------

        private static void MainMenu()
        {
            while (atm.IsAuthenticated)
            {
                Console.WriteLine();
                Console.WriteLine("┌──────────── ГОЛОВНЕ МЕНЮ ────────────┐");
                Console.WriteLine("│ 1. Переглянути баланс                │");
                Console.WriteLine("│ 2. Зняти готівку                     │");
                Console.WriteLine("│ 3. Поповнити картку                  │");
                Console.WriteLine("│ 4. Переказати на іншу картку         │");
                Console.WriteLine("│ 5. Журнал операцій у файл: " + (journalEnabled ? "УВІМК " : "ВИМК  ") + "    │");
                Console.WriteLine("│ 0. Забрати картку                    │");
                Console.WriteLine("└──────────────────────────────────────┘");
                Console.Write("Ваш вибір: ");
                string choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice == null ? "0" : choice.Trim())
                {
                    case "1":
                        decimal balance;
                        atm.CheckBalance(out balance);
                        break;
                    case "2":
                        Withdraw();
                        break;
                    case "3":
                        Deposit();
                        break;
                    case "4":
                        Transfer();
                        break;
                    case "5":
                        ToggleJournal();
                        break;
                    case "0":
                        atm.EjectCard();
                        Pause();
                        break;
                    default:
                        Console.WriteLine("Невідомий пункт меню.");
                        break;
                }
            }
        }

        private static void Withdraw()
        {
            Console.WriteLine("Швидкий вибір: 100 | 200 | 500 | 1000 | 2000 (або введіть свою суму, кратну 50)");
            decimal amount;
            if (ReadAmount("Сума для зняття: ", out amount))
                atm.Withdraw(amount);
        }

        private static void Deposit()
        {
            decimal amount;
            if (ReadAmount("Сума для зарахування: ", out amount))
                atm.Deposit(amount);
        }

        private static void Transfer()
        {
            Console.Write("Номер картки отримувача: ");
            string target = Console.ReadLine();
            decimal amount;
            if (ReadAmount("Сума переказу: ", out amount))
                atm.Transfer(target, amount);
        }

        /// <summary>
        /// Демонстрація підписки (+=) та відписки (-=) обробника під час роботи програми.
        /// </summary>
        private static void ToggleJournal()
        {
            journalEnabled = !journalEnabled;
            if (journalEnabled)
            {
                atm.Authenticated += WriteJournal;
                atm.BalanceChecked += WriteJournal;
                atm.Withdrawn += WriteJournal;
                atm.Deposited += WriteJournal;
                atm.Transferred += WriteJournal;
                atm.OperationFailed += WriteJournal;
                Console.WriteLine("Журнал увімкнено: " + journalPath);
            }
            else
            {
                atm.Authenticated -= WriteJournal;
                atm.BalanceChecked -= WriteJournal;
                atm.Withdrawn -= WriteJournal;
                atm.Deposited -= WriteJournal;
                atm.Transferred -= WriteJournal;
                atm.OperationFailed -= WriteJournal;
                Console.WriteLine("Журнал вимкнено.");
            }
        }

        private static void WriteJournal(object sender, AtmEventArgs e)
        {
            File.AppendAllText(journalPath, AtmMessageFormatter.LogLine(e) + Environment.NewLine);
        }

        // ---------- Допоміжні методи ----------

        private static void PrintHeader()
        {
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════════");
            Console.WriteLine("  " + bank.Name + " — банкомат " + atm.Id);
            Console.WriteLine("  " + atm.Address);
            Console.WriteLine("══════════════════════════════════════════════");
        }

        private static bool ReadAmount(string prompt, out decimal amount)
        {
            Console.Write(prompt);
            if (Money.TryParse(Console.ReadLine(), out amount))
                return true;
            Console.WriteLine("Некоректна сума.");
            return false;
        }

        /// <summary>Читає PIN, показуючи * замість цифр.</summary>
        private static string ReadHidden()
        {
            if (Console.IsInputRedirected)
                return Console.ReadLine();

            var sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (char.IsDigit(key.KeyChar) && sb.Length < Account.PinLength)
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
        }

        private static void Pause()
        {
            if (Console.IsInputRedirected) return;
            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey(true);
        }
    }
}
