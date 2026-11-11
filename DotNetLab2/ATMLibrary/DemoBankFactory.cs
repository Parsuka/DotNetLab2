using System.Collections.Generic;

namespace ATMLibrary
{
    /// <summary>Створює банк із тестовими рахунками та банкоматами.</summary>
    public static class DemoBankFactory
    {
        /// <summary>Підказки PIN-кодів тестових карток (лише для демонстрації).</summary>
        public static readonly IReadOnlyDictionary<string, string> PinHints = new Dictionary<string, string>
        {
            { "4149439012345678", "1234" },
            { "5168742000001111", "1111" },
            { "4441114422223333", "0000" }
        };

        public static Bank Create()
        {
            var bank = new Bank("Житомир Банк");

            bank.AddAccount(new Account("4149 4390 1234 5678", "Ярощук", "Даниїл", "1234", 15000m));
            bank.AddAccount(new Account("5168 7420 0000 1111", "Петренко", "Олена", "1111", 8200.50m));
            bank.AddAccount(new Account("4441 1144 2222 3333", "Коваль", "Андрій", "0000", 500m));

            bank.AddAtm("ATM-001", "м. Житомир, вул. Чуднівська, 103", 100000m);
            bank.AddAtm("ATM-002", "м. Житомир, вул. Київська, 77", 3000m);

            return bank;
        }
    }
}
