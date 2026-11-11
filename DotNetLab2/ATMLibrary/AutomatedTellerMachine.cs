using System;

namespace ATMLibrary
{
    /// <summary>
    /// Модель банкомата. Кожна операція генерує подію, на яку
    /// зовнішній код може підписати власні обробники (+=) або відписати їх (-=).
    /// </summary>
    public class AutomatedTellerMachine
    {
        /// <summary>Номінал, кратним якому має бути сума зняття.</summary>
        public const decimal BanknoteStep = 50m;

        private Account pendingAccount;   // картка вставлена, але PIN ще не введено

        internal AutomatedTellerMachine(Bank bank, string id, string address, decimal initialCash)
        {
            if (bank == null) throw new ArgumentNullException(nameof(bank));
            if (initialCash < 0) throw new ArgumentOutOfRangeException(nameof(initialCash));
            Bank = bank;
            Id = id;
            Address = address;
            CashAmount = initialCash;
            MaxWithdrawal = 20000m;
            MaxDeposit = 50000m;
        }

        // ---------- Властивості ----------

        /// <summary>Банк, якому належить банкомат.</summary>
        public Bank Bank { get; private set; }

        /// <summary>Ідентифікатор банкомата.</summary>
        public string Id { get; private set; }

        /// <summary>Адреса встановлення.</summary>
        public string Address { get; set; }

        /// <summary>Кількість готівки в банкоматі.</summary>
        public decimal CashAmount { get; private set; }

        /// <summary>Максимальна сума одного зняття.</summary>
        public decimal MaxWithdrawal { get; set; }

        /// <summary>Максимальна сума одного поповнення.</summary>
        public decimal MaxDeposit { get; set; }

        /// <summary>Рахунок клієнта, який пройшов аутентифікацію (або null).</summary>
        public Account CurrentAccount { get; private set; }

        /// <summary>Чи є активна сесія.</summary>
        public bool IsAuthenticated
        {
            get { return CurrentAccount != null; }
        }

        /// <summary>Чи вставлена картка, для якої очікується PIN.</summary>
        public bool IsCardInserted
        {
            get { return pendingAccount != null || CurrentAccount != null; }
        }

        // ---------- Події ----------

        /// <summary>Картку прийнято, очікується PIN.</summary>
        public event AtmEventHandler CardInserted;

        /// <summary>Успішна аутентифікація.</summary>
        public event AtmEventHandler Authenticated;

        /// <summary>Перегляд балансу.</summary>
        public event AtmEventHandler BalanceChecked;

        /// <summary>Зняття готівки.</summary>
        public event AtmEventHandler Withdrawn;

        /// <summary>Зарахування коштів на картку.</summary>
        public event AtmEventHandler Deposited;

        /// <summary>Переказ на іншу картку.</summary>
        public event AtmEventHandler Transferred;

        /// <summary>Будь-яка операція завершилась помилкою (невірний PIN, нестача коштів тощо).</summary>
        public event AtmEventHandler OperationFailed;

        /// <summary>Картку повернуто, сесію завершено.</summary>
        public event AtmEventHandler SessionEnded;

        // Методи, що безпечно викликають події (можуть бути перевизначені нащадками)
        protected virtual void OnCardInserted(AtmEventArgs e) { Raise(CardInserted, e); }
        protected virtual void OnAuthenticated(AtmEventArgs e) { Raise(Authenticated, e); }
        protected virtual void OnBalanceChecked(AtmEventArgs e) { Raise(BalanceChecked, e); }
        protected virtual void OnWithdrawn(AtmEventArgs e) { Raise(Withdrawn, e); }
        protected virtual void OnDeposited(AtmEventArgs e) { Raise(Deposited, e); }
        protected virtual void OnTransferred(AtmEventArgs e) { Raise(Transferred, e); }
        protected virtual void OnOperationFailed(AtmEventArgs e) { Raise(OperationFailed, e); }
        protected virtual void OnSessionEnded(AtmEventArgs e) { Raise(SessionEnded, e); }

        private void Raise(AtmEventHandler handler, AtmEventArgs e)
        {
            e.AtmId = Id;
            // Копія делегата в локальній змінній захищає від відписки під час виклику
            AtmEventHandler copy = handler;
            if (copy != null)
                copy(this, e);
        }

        // ---------- Аутентифікація ----------

        /// <summary>Крок 1: клієнт вставляє картку (вводить її номер).</summary>
        public bool InsertCard(string cardNumber)
        {
            if (IsCardInserted)
                return Fail(OperationType.CardInserted, "У банкоматі вже є картка. Спершу заберіть її.", null);

            if (!Account.IsValidCardNumber(cardNumber))
                return Fail(OperationType.CardInserted, "Номер картки має містити 16 цифр.", null);

            Account account = Bank.FindAccount(cardNumber);
            if (account == null)
                return Fail(OperationType.CardInserted, "Картку не знайдено. Перевірте номер.", null);

            if (account.IsBlocked)
                return Fail(OperationType.CardInserted, "Картку заблоковано. Зверніться до відділення банку.", account);

            pendingAccount = account;
            OnCardInserted(new AtmEventArgs(OperationType.CardInserted, true,
                "Картку " + account.MaskedCardNumber + " прийнято. Введіть PIN-код.")
            {
                Account = account
            });
            return true;
        }

        /// <summary>Крок 2: клієнт вводить PIN-код.</summary>
        public bool EnterPin(string pin)
        {
            if (pendingAccount == null)
                return Fail(OperationType.Authentication, "Спершу вставте картку.", null);

            Account account = pendingAccount;
            if (account.VerifyPin(pin))
            {
                pendingAccount = null;
                CurrentAccount = account;
                OnAuthenticated(new AtmEventArgs(OperationType.Authentication, true,
                    "Вітаємо, " + account.FirstName + "! Аутентифікацію пройдено успішно.")
                {
                    Account = account,
                    BalanceAfter = account.Balance
                });
                return true;
            }

            if (account.IsBlocked)
            {
                pendingAccount = null;
                return Fail(OperationType.Authentication,
                    "PIN-код введено невірно " + Account.MaxPinAttempts + " рази. Картку заблоковано.", account);
            }

            return Fail(OperationType.Authentication,
                "Невірний PIN-код. Залишилось спроб: " + account.RemainingPinAttempts + ".", account);
        }

        /// <summary>Аутентифікація одним викликом: номер картки + PIN.</summary>
        public bool Authenticate(string cardNumber, string pin)
        {
            if (!InsertCard(cardNumber))
                return false;
            bool ok = EnterPin(pin);
            if (!ok && pendingAccount != null)
                pendingAccount = null; // повертаємо картку, щоб можна було спробувати ще раз
            return ok;
        }

        // ---------- Операції ----------

        /// <summary>Перегляд балансу.</summary>
        public bool CheckBalance(out decimal balance)
        {
            balance = 0;
            if (!RequireSession(OperationType.BalanceCheck))
                return false;

            balance = CurrentAccount.Balance;
            OnBalanceChecked(new AtmEventArgs(OperationType.BalanceCheck, true,
                "Баланс картки " + CurrentAccount.MaskedCardNumber + ": " + Money.Format(balance))
            {
                Account = CurrentAccount,
                BalanceAfter = balance
            });
            return true;
        }

        /// <summary>Зняття готівки.</summary>
        public bool Withdraw(decimal amount)
        {
            const OperationType op = OperationType.Withdrawal;
            if (!RequireSession(op)) return false;

            if (amount <= 0)
                return Fail(op, "Сума має бути більшою за нуль.", CurrentAccount, amount);
            if (amount % BanknoteStep != 0)
                return Fail(op, "Банкомат видає суми, кратні " + BanknoteStep + " грн.", CurrentAccount, amount);
            if (amount > MaxWithdrawal)
                return Fail(op, "Максимальна сума одного зняття — " + Money.Format(MaxWithdrawal) + ".", CurrentAccount, amount);
            if (amount > CurrentAccount.Balance)
                return Fail(op, "Недостатньо коштів на рахунку.", CurrentAccount, amount);
            if (amount > CashAmount)
                return Fail(op, "У банкоматі недостатньо готівки. Доступно: " + Money.Format(CashAmount) + ".", CurrentAccount, amount);

            CurrentAccount.Debit(amount);
            CashAmount -= amount;

            OnWithdrawn(new AtmEventArgs(op, true,
                "Видано " + Money.Format(amount) + ". Залишок: " + Money.Format(CurrentAccount.Balance))
            {
                Account = CurrentAccount,
                Amount = amount,
                BalanceAfter = CurrentAccount.Balance
            });
            return true;
        }

        /// <summary>Зарахування коштів (готівкою через банкомат) на картку.</summary>
        public bool Deposit(decimal amount)
        {
            const OperationType op = OperationType.Deposit;
            if (!RequireSession(op)) return false;

            if (amount <= 0)
                return Fail(op, "Сума має бути більшою за нуль.", CurrentAccount, amount);
            if (amount > MaxDeposit)
                return Fail(op, "Максимальна сума одного поповнення — " + Money.Format(MaxDeposit) + ".", CurrentAccount, amount);

            CurrentAccount.Credit(amount);
            CashAmount += amount;

            OnDeposited(new AtmEventArgs(op, true,
                "Зараховано " + Money.Format(amount) + ". Баланс: " + Money.Format(CurrentAccount.Balance))
            {
                Account = CurrentAccount,
                Amount = amount,
                BalanceAfter = CurrentAccount.Balance
            });
            return true;
        }

        /// <summary>Переказ коштів на картку із заданим номером.</summary>
        public bool Transfer(string targetCardNumber, decimal amount)
        {
            const OperationType op = OperationType.Transfer;
            if (!RequireSession(op)) return false;

            if (!Account.IsValidCardNumber(targetCardNumber))
                return Fail(op, "Номер картки отримувача має містити 16 цифр.", CurrentAccount, amount);

            Account target = Bank.FindAccount(targetCardNumber);
            if (target == null)
                return Fail(op, "Картку отримувача не знайдено.", CurrentAccount, amount);
            if (target == CurrentAccount)
                return Fail(op, "Не можна переказати кошти на ту саму картку.", CurrentAccount, amount);
            if (amount <= 0)
                return Fail(op, "Сума має бути більшою за нуль.", CurrentAccount, amount);
            if (decimal.Round(amount, 2) != amount)
                return Fail(op, "Сума може мати не більше двох знаків після коми.", CurrentAccount, amount);
            if (amount > CurrentAccount.Balance)
                return Fail(op, "Недостатньо коштів на рахунку.", CurrentAccount, amount);

            CurrentAccount.Debit(amount);
            target.Credit(amount);

            OnTransferred(new AtmEventArgs(op, true,
                "Переказано " + Money.Format(amount) + " на картку " + target.MaskedCardNumber +
                " (" + target.OwnerName + "). Залишок: " + Money.Format(CurrentAccount.Balance))
            {
                Account = CurrentAccount,
                TargetAccount = target,
                Amount = amount,
                BalanceAfter = CurrentAccount.Balance
            });
            return true;
        }

        /// <summary>Повернення картки та завершення сесії.</summary>
        public void EjectCard()
        {
            Account account = CurrentAccount ?? pendingAccount;
            CurrentAccount = null;
            pendingAccount = null;
            if (account != null)
            {
                OnSessionEnded(new AtmEventArgs(OperationType.SessionEnd, true,
                    "Заберіть, будь ласка, картку. Дякуємо, що скористалися послугами " + Bank.Name + "!")
                {
                    Account = account
                });
            }
        }

        /// <summary>Інкасація: додавання готівки в банкомат.</summary>
        public void LoadCash(decimal amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            CashAmount += amount;
        }

        public override string ToString()
        {
            return Id + " (" + Address + ")";
        }

        // ---------- Допоміжні методи ----------

        private bool RequireSession(OperationType op)
        {
            if (IsAuthenticated) return true;
            return Fail(op, "Спершу пройдіть аутентифікацію (картка + PIN).", null);
        }

        private bool Fail(OperationType op, string message, Account account, decimal amount = 0)
        {
            OnOperationFailed(new AtmEventArgs(op, false, message)
            {
                Account = account,
                Amount = amount,
                BalanceAfter = account != null ? account.Balance : 0
            });
            return false;
        }
    }
}
