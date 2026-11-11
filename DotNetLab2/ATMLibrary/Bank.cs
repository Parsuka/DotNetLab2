using System;
using System.Collections.Generic;
using System.Linq;

namespace ATMLibrary
{
    /// <summary>
    /// Банк: зберігає рахунки клієнтів і список банкоматів.
    /// </summary>
    public class Bank
    {
        private readonly Dictionary<string, Account> accounts = new Dictionary<string, Account>();
        private readonly List<AutomatedTellerMachine> atms = new List<AutomatedTellerMachine>();

        public Bank(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Назва банку не може бути порожньою.", nameof(name));
            Name = name;
        }

        /// <summary>Назва банку.</summary>
        public string Name { get; private set; }

        /// <summary>Усі рахунки банку.</summary>
        public IReadOnlyCollection<Account> Accounts
        {
            get { return accounts.Values; }
        }

        /// <summary>Усі банкомати банку.</summary>
        public IReadOnlyList<AutomatedTellerMachine> Atms
        {
            get { return atms; }
        }

        /// <summary>Відкриває новий рахунок.</summary>
        public Account AddAccount(Account account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (accounts.ContainsKey(account.CardNumber))
                throw new InvalidOperationException("Картка з таким номером уже існує.");
            accounts.Add(account.CardNumber, account);
            return account;
        }

        /// <summary>Шукає рахунок за номером картки (пробіли ігноруються). Повертає null, якщо не знайдено.</summary>
        public Account FindAccount(string cardNumber)
        {
            Account account;
            accounts.TryGetValue(Account.NormalizeCardNumber(cardNumber), out account);
            return account;
        }

        /// <summary>Встановлює новий банкомат банку.</summary>
        public AutomatedTellerMachine AddAtm(string id, string address, decimal initialCash)
        {
            if (atms.Any(a => a.Id == id))
                throw new InvalidOperationException("Банкомат з таким ідентифікатором уже існує.");
            var atm = new AutomatedTellerMachine(this, id, address, initialCash);
            atms.Add(atm);
            return atm;
        }

        /// <summary>Шукає банкомат за ідентифікатором.</summary>
        public AutomatedTellerMachine FindAtm(string id)
        {
            return atms.FirstOrDefault(a => a.Id == id);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
