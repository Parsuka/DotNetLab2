using System;
using System.Linq;

namespace ATMLibrary
{
    /// <summary>
    /// Картковий рахунок клієнта.
    /// </summary>
    public class Account
    {
        public const int CardNumberLength = 16;
        public const int PinLength = 4;
        public const int MaxPinAttempts = 3;

        private readonly string pinCode;
        private decimal balance;

        public Account(string cardNumber, string lastName, string firstName,
                       string pinCode, decimal initialBalance, string email = null)
        {
            string normalized = NormalizeCardNumber(cardNumber);
            if (!IsValidCardNumber(normalized))
                throw new ArgumentException("Номер картки має складатися з 16 цифр.", nameof(cardNumber));
            if (string.IsNullOrEmpty(pinCode) || pinCode.Length != PinLength || !pinCode.All(char.IsDigit))
                throw new ArgumentException("PIN-код має складатися з 4 цифр.", nameof(pinCode));
            if (initialBalance < 0)
                throw new ArgumentOutOfRangeException(nameof(initialBalance), "Баланс не може бути від'ємним.");

            CardNumber = normalized;
            LastName = lastName;
            FirstName = firstName;
            this.pinCode = pinCode;
            balance = initialBalance;
            Email = email;
        }

        /// <summary>Номер картки (16 цифр без пробілів).</summary>
        public string CardNumber { get; private set; }

        /// <summary>Прізвище власника.</summary>
        public string LastName { get; set; }

        /// <summary>Ім'я власника.</summary>
        public string FirstName { get; set; }

        /// <summary>Прізвище та ім'я власника.</summary>
        public string OwnerName
        {
            get { return LastName + " " + FirstName; }
        }

        /// <summary>Електронна пошта для сповіщень (необов'язково).</summary>
        public string Email { get; set; }

        /// <summary>Поточний баланс рахунку.</summary>
        public decimal Balance
        {
            get { return balance; }
        }

        /// <summary>Чи заблокована картка (після 3 невдалих спроб введення PIN).</summary>
        public bool IsBlocked { get; private set; }

        /// <summary>Кількість невдалих спроб введення PIN поспіль.</summary>
        public int FailedPinAttempts { get; private set; }

        /// <summary>Скільки спроб введення PIN залишилось.</summary>
        public int RemainingPinAttempts
        {
            get { return Math.Max(0, MaxPinAttempts - FailedPinAttempts); }
        }

        /// <summary>Номер картки у форматі 0000 0000 0000 0000.</summary>
        public string FormattedCardNumber
        {
            get { return FormatCardNumber(CardNumber); }
        }

        /// <summary>Номер картки з прихованими цифрами: **** **** **** 1234.</summary>
        public string MaskedCardNumber
        {
            get { return "**** **** **** " + CardNumber.Substring(CardNumber.Length - 4); }
        }

        /// <summary>
        /// Перевіряє PIN-код. Після 3 невдалих спроб картка блокується.
        /// </summary>
        internal bool VerifyPin(string pin)
        {
            if (IsBlocked)
                return false;

            if (pin == pinCode)
            {
                FailedPinAttempts = 0;
                return true;
            }

            FailedPinAttempts++;
            if (FailedPinAttempts >= MaxPinAttempts)
                IsBlocked = true;
            return false;
        }

        /// <summary>Списує кошти з рахунку.</summary>
        internal void Debit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > balance) throw new InvalidOperationException("Недостатньо коштів на рахунку.");
            balance -= amount;
        }

        /// <summary>Зараховує кошти на рахунок.</summary>
        internal void Credit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            balance += amount;
        }

        /// <summary>Розблоковує картку (виконує банк).</summary>
        public void Unblock()
        {
            IsBlocked = false;
            FailedPinAttempts = 0;
        }

        public override string ToString()
        {
            return FormattedCardNumber + " — " + OwnerName;
        }

        // ---------- Допоміжні статичні методи ----------

        /// <summary>Прибирає пробіли та дефіси з номера картки.</summary>
        public static string NormalizeCardNumber(string cardNumber)
        {
            if (cardNumber == null) return string.Empty;
            return new string(cardNumber.Where(char.IsDigit).ToArray());
        }

        public static bool IsValidCardNumber(string cardNumber)
        {
            string n = NormalizeCardNumber(cardNumber);
            return n.Length == CardNumberLength;
        }

        /// <summary>Розбиває номер на групи по 4 цифри.</summary>
        public static string FormatCardNumber(string cardNumber)
        {
            string n = NormalizeCardNumber(cardNumber);
            var parts = Enumerable.Range(0, (n.Length + 3) / 4)
                                  .Select(i => n.Substring(i * 4, Math.Min(4, n.Length - i * 4)));
            return string.Join(" ", parts);
        }
    }
}
