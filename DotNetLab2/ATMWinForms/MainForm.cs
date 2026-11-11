using System;
using System.Drawing;
using System.Windows.Forms;
using ATMLibrary;

namespace ATMWinForms
{
    /// <summary>Стани екрана банкомата.</summary>
    internal enum ScreenState
    {
        InsertCard,
        EnterPin,
        Menu,
        Balance,
        Withdraw,
        Deposit,
        TransferCard,
        TransferAmount
    }

    public partial class MainForm : Form
    {
        private readonly Bank bank;
        private AutomatedTellerMachine atm;
        private readonly EmailNotifier emailNotifier;

        private ScreenState state;
        private string input = string.Empty;
        private int inputMaxLength;
        private string transferTarget;

        // Дії бічних кнопок — масиви вбудованого делегата Action
        private readonly Action[] leftActions = new Action[4];
        private readonly Action[] rightActions = new Action[4];

        // Обробник, збережений у змінній, щоб його можна було відписати (-=)
        private readonly AtmEventHandler cashOutHandler;

        public MainForm()
        {
            BuildLayout();

            // Лямбда-вираз, присвоєний змінній типу делегата
            cashOutHandler = (sender, e) => lblCashOut.Text = "Заберіть\n" + Money.Format(e.Amount);

            bank = DemoBankFactory.Create();
            lblBankName.Text = bank.Name.ToUpper();

            emailNotifier = EmailNotifier.FromEnvironment();
            if (emailNotifier != null)
            {
                // Подія EmailNotifier приходить з фонового потоку → BeginInvoke
                emailNotifier.StatusChanged += message => BeginInvoke(new Action(() => AddJournal("[e-mail] " + message)));
            }
            else
            {
                chkEmail.Enabled = false;
                chkEmail.Text = "Сповіщення на e-mail (не налаштовано)";
            }

            foreach (AutomatedTellerMachine machine in bank.Atms)
                cmbAtm.Items.Add(machine.Id);

            atm = bank.Atms[0];
            SubscribeAll(atm);
            cmbAtm.SelectedIndex = 0;

            cmbAtm.SelectedIndexChanged += OnAtmChanged;
            chkMessageBox.CheckedChanged += (s, e) => ToggleMessageBoxHandlers(atm, chkMessageBox.Checked);
            chkJournal.CheckedChanged += (s, e) => ToggleJournalHandlers(atm, chkJournal.Checked);
            chkEmail.CheckedChanged += (s, e) => ToggleEmailHandlers(atm, chkEmail.Checked);

            RefreshServicePanel();
            ShowState(ScreenState.InsertCard);
        }

        // =====================================================================
        //  Підписка / відписка обробників подій
        // =====================================================================

        private void SubscribeAll(AutomatedTellerMachine machine)
        {
            // Обробник, що оновлює екран, підписаний завжди
            machine.CardInserted += OnScreenUpdate;
            machine.Authenticated += OnScreenUpdate;
            machine.BalanceChecked += OnScreenUpdate;
            machine.Withdrawn += OnScreenUpdate;
            machine.Deposited += OnScreenUpdate;
            machine.Transferred += OnScreenUpdate;
            machine.OperationFailed += OnScreenUpdate;
            machine.SessionEnded += OnScreenUpdate;

            machine.Withdrawn += cashOutHandler;

            ToggleMessageBoxHandlers(machine, chkMessageBox.Checked);
            ToggleJournalHandlers(machine, chkJournal.Checked);
            ToggleEmailHandlers(machine, chkEmail.Checked);
        }

        private void UnsubscribeAll(AutomatedTellerMachine machine)
        {
            machine.CardInserted -= OnScreenUpdate;
            machine.Authenticated -= OnScreenUpdate;
            machine.BalanceChecked -= OnScreenUpdate;
            machine.Withdrawn -= OnScreenUpdate;
            machine.Deposited -= OnScreenUpdate;
            machine.Transferred -= OnScreenUpdate;
            machine.OperationFailed -= OnScreenUpdate;
            machine.SessionEnded -= OnScreenUpdate;
            machine.Withdrawn -= cashOutHandler;

            ToggleMessageBoxHandlers(machine, false);
            ToggleJournalHandlers(machine, false);
            ToggleEmailHandlers(machine, false);
        }

        private void ToggleMessageBoxHandlers(AutomatedTellerMachine machine, bool enable)
        {
            // Спершу відписуємо, щоб обробник не додався двічі
            machine.Authenticated -= OnAuthenticatedMessage;
            machine.BalanceChecked -= OnBalanceMessage;
            machine.Withdrawn -= OnWithdrawnMessage;
            machine.Deposited -= OnDepositedMessage;
            machine.Transferred -= OnTransferredMessage;
            machine.OperationFailed -= OnFailedMessage;

            if (!enable) return;

            machine.Authenticated += OnAuthenticatedMessage;
            machine.BalanceChecked += OnBalanceMessage;
            machine.Withdrawn += OnWithdrawnMessage;
            machine.Deposited += OnDepositedMessage;
            machine.Transferred += OnTransferredMessage;
            machine.OperationFailed += OnFailedMessage;
        }

        private void ToggleJournalHandlers(AutomatedTellerMachine machine, bool enable)
        {
            machine.CardInserted -= OnJournal;
            machine.Authenticated -= OnJournal;
            machine.BalanceChecked -= OnJournal;
            machine.Withdrawn -= OnJournal;
            machine.Deposited -= OnJournal;
            machine.Transferred -= OnJournal;
            machine.OperationFailed -= OnJournal;
            machine.SessionEnded -= OnJournal;

            if (!enable) return;

            machine.CardInserted += OnJournal;
            machine.Authenticated += OnJournal;
            machine.BalanceChecked += OnJournal;
            machine.Withdrawn += OnJournal;
            machine.Deposited += OnJournal;
            machine.Transferred += OnJournal;
            machine.OperationFailed += OnJournal;
            machine.SessionEnded += OnJournal;
        }

        private void ToggleEmailHandlers(AutomatedTellerMachine machine, bool enable)
        {
            if (emailNotifier == null) return;
            emailNotifier.Unsubscribe(machine);
            if (enable) emailNotifier.Subscribe(machine);
        }

        // =====================================================================
        //  Власні обробники подій
        // =====================================================================

        private void OnAuthenticatedMessage(object sender, AtmEventArgs e)
        {
            MessageBox.Show(e.Message, "Аутентифікація", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnBalanceMessage(object sender, AtmEventArgs e)
        {
            MessageBox.Show(e.Message, "Баланс", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnWithdrawnMessage(object sender, AtmEventArgs e)
        {
            MessageBox.Show(e.Message + "\n\nЗаберіть готівку.", "Зняття готівки", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnDepositedMessage(object sender, AtmEventArgs e)
        {
            MessageBox.Show(e.Message, "Поповнення картки", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnTransferredMessage(object sender, AtmEventArgs e)
        {
            MessageBox.Show(e.Message, "Переказ коштів", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnFailedMessage(object sender, AtmEventArgs e)
        {
            MessageBox.Show(e.Message, AtmMessageFormatter.Title(e), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OnJournal(object sender, AtmEventArgs e)
        {
            AddJournal(AtmMessageFormatter.LogLine(e));
        }

        /// <summary>Оновлює рядок стану на екрані та сервісну панель.</summary>
        private void OnScreenUpdate(object sender, AtmEventArgs e)
        {
            lblStatus.ForeColor = e.Success ? Color.LightGreen : Color.FromArgb(255, 120, 120);
            lblStatus.Text = e.Message;
            RefreshServicePanel();
        }

        // =====================================================================
        //  Екрани
        // =====================================================================

        private void ShowState(ScreenState newState)
        {
            state = newState;
            input = string.Empty;
            ClearSideActions();
            lblInput.Visible = false;

            switch (state)
            {
                case ScreenState.InsertCard:
                    lblTitle.Text = "Ласкаво просимо!";
                    lblBody.Text = "Вставте картку: введіть 16 цифр номера і натисніть ВВІД\nабо оберіть картку на сервісній панелі";
                    ShowInput(Account.CardNumberLength);
                    lblCashOut.Text = string.Empty;
                    break;

                case ScreenState.EnterPin:
                    lblTitle.Text = "Введіть PIN-код";
                    lblBody.Text = "ВВІД — підтвердити, СКАСУВАТИ — повернути картку";
                    ShowInput(Account.PinLength);
                    SetSide(false, 3, "Повернути картку", EjectCard);
                    break;

                case ScreenState.Menu:
                    lblTitle.Text = "Оберіть операцію";
                    lblBody.Text = atm.CurrentAccount.OwnerName + "\n" + atm.CurrentAccount.MaskedCardNumber;
                    SetSide(true, 0, "Баланс", CheckBalance);
                    SetSide(true, 1, "Зняти готівку", () => ShowState(ScreenState.Withdraw));
                    SetSide(true, 2, "Поповнити картку", () => ShowState(ScreenState.Deposit));
                    SetSide(true, 3, "Переказ на картку", () => ShowState(ScreenState.TransferCard));
                    SetSide(false, 3, "Забрати картку", EjectCard);
                    break;

                case ScreenState.Balance:
                    lblTitle.Text = "Баланс картки";
                    lblBody.Text = "Доступно:";
                    lblInput.Visible = true;
                    lblInput.Text = Money.Format(atm.CurrentAccount.Balance);
                    SetSide(true, 1, "Зняти готівку", () => ShowState(ScreenState.Withdraw));
                    SetSide(false, 2, "Інша операція", () => ShowState(ScreenState.Menu));
                    SetSide(false, 3, "Забрати картку", EjectCard);
                    break;

                case ScreenState.Withdraw:
                    lblTitle.Text = "Зняття готівки";
                    lblBody.Text = "Оберіть суму або введіть свою (кратну " + AutomatedTellerMachine.BanknoteStep + " грн)";
                    ShowInput(6);
                    AddQuickAmounts(Withdraw);
                    break;

                case ScreenState.Deposit:
                    lblTitle.Text = "Поповнення картки";
                    lblBody.Text = "Внесіть готівку: оберіть або введіть суму";
                    ShowInput(6);
                    AddQuickAmounts(Deposit);
                    break;

                case ScreenState.TransferCard:
                    lblTitle.Text = "Переказ на картку";
                    lblBody.Text = "Введіть номер картки отримувача (16 цифр)";
                    ShowInput(Account.CardNumberLength);
                    SetSide(false, 3, "Назад", () => ShowState(ScreenState.Menu));
                    break;

                case ScreenState.TransferAmount:
                    lblTitle.Text = "Переказ на картку";
                    lblBody.Text = "Отримувач: " + Account.FormatCardNumber(transferTarget) + "\nВведіть суму переказу";
                    ShowInput(7);
                    SetSide(false, 3, "Назад", () => ShowState(ScreenState.TransferCard));
                    break;
            }

            UpdateInputLabel();
            RefreshServicePanel();
        }

        private void AddQuickAmounts(Action<decimal> operation)
        {
            decimal[] left = { 100, 200, 500, 1000 };
            decimal[] right = { 2000, 5000 };
            for (int i = 0; i < left.Length; i++)
            {
                decimal amount = left[i];
                SetSide(true, i, amount + " грн", () => operation(amount));
            }
            for (int i = 0; i < right.Length; i++)
            {
                decimal amount = right[i];
                SetSide(false, i, amount + " грн", () => operation(amount));
            }
            SetSide(false, 3, "Назад", () => ShowState(ScreenState.Menu));
        }

        private void ShowInput(int maxLength)
        {
            inputMaxLength = maxLength;
            lblInput.Visible = true;
        }

        private void UpdateInputLabel()
        {
            if (!lblInput.Visible || state == ScreenState.Balance)
                return;

            switch (state)
            {
                case ScreenState.InsertCard:
                case ScreenState.TransferCard:
                    lblInput.Text = input.Length == 0 ? "____ ____ ____ ____" : Account.FormatCardNumber(input);
                    break;
                case ScreenState.EnterPin:
                    lblInput.Text = new string('●', input.Length) + new string('_', Account.PinLength - input.Length);
                    break;
                default:
                    lblInput.Text = (input.Length == 0 ? "0" : input) + " грн";
                    break;
            }
        }

        private void ClearSideActions()
        {
            for (int i = 0; i < 4; i++)
            {
                leftActions[i] = null;
                rightActions[i] = null;
                leftLabels[i].Text = string.Empty;
                rightLabels[i].Text = string.Empty;
            }
        }

        private void SetSide(bool left, int row, string text, Action action)
        {
            if (left)
            {
                leftLabels[row].Text = "◄ " + text;
                leftActions[row] = action;
            }
            else
            {
                rightLabels[row].Text = text + " ►";
                rightActions[row] = action;
            }
        }

        // =====================================================================
        //  Обробка натискань кнопок
        // =====================================================================

        private void PressSideButton(bool left, int row)
        {
            Action action = left ? leftActions[row] : rightActions[row];
            if (action != null)
                action();
        }

        private void PressDigit(char digit)
        {
            if (!lblInput.Visible || state == ScreenState.Balance)
                return;
            if (input.Length >= inputMaxLength)
                return;
            if (input.Length == 0 && digit == '0' && IsAmountState())
                return; // сума не починається з нуля
            input += digit;
            UpdateInputLabel();
        }

        private void PressClear()
        {
            input = string.Empty;
            UpdateInputLabel();
        }

        private void PressBackspace()
        {
            if (input.Length > 0)
                input = input.Substring(0, input.Length - 1);
            UpdateInputLabel();
        }

        private void PressCancel()
        {
            switch (state)
            {
                case ScreenState.InsertCard:
                    PressClear();
                    break;
                case ScreenState.EnterPin:
                case ScreenState.Menu:
                    EjectCard();
                    break;
                case ScreenState.TransferAmount:
                    ShowState(ScreenState.TransferCard);
                    break;
                default:
                    ShowState(ScreenState.Menu);
                    break;
            }
        }

        private void PressEnter()
        {
            switch (state)
            {
                case ScreenState.InsertCard:
                    InsertCard(input);
                    break;

                case ScreenState.EnterPin:
                    atm.EnterPin(input);
                    if (atm.IsAuthenticated)
                        ShowState(ScreenState.Menu);
                    else if (!atm.IsCardInserted)
                        ShowState(ScreenState.InsertCard); // картку заблоковано
                    else
                        PressClear();
                    break;

                case ScreenState.Withdraw:
                    if (input.Length > 0) Withdraw(decimal.Parse(input));
                    break;

                case ScreenState.Deposit:
                    if (input.Length > 0) Deposit(decimal.Parse(input));
                    break;

                case ScreenState.TransferCard:
                    if (!Account.IsValidCardNumber(input))
                    {
                        ShowLocalError("Номер картки має містити 16 цифр.");
                        break;
                    }
                    transferTarget = input;
                    ShowState(ScreenState.TransferAmount);
                    break;

                case ScreenState.TransferAmount:
                    if (input.Length == 0) break;
                    if (atm.Transfer(transferTarget, decimal.Parse(input)))
                        ShowState(ScreenState.Menu);
                    else
                        PressClear();
                    break;

                case ScreenState.Menu:
                case ScreenState.Balance:
                    break;
            }
        }

        private bool IsAmountState()
        {
            return state == ScreenState.Withdraw || state == ScreenState.Deposit || state == ScreenState.TransferAmount;
        }

        // =====================================================================
        //  Операції банкомата
        // =====================================================================

        private void InsertCard(string cardNumber)
        {
            if (atm.InsertCard(cardNumber))
                ShowState(ScreenState.EnterPin);
            else
                PressClear();
        }

        private void InsertSelectedCard()
        {
            if (state != ScreenState.InsertCard)
            {
                MessageBox.Show("У банкоматі вже є картка. Спершу заберіть її.", "Картка",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Account account = lstCards.SelectedItem is CardItem ? ((CardItem)lstCards.SelectedItem).Account : null;
            if (account == null)
            {
                MessageBox.Show("Оберіть картку зі списку.", "Картка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            InsertCard(account.CardNumber);
        }

        private void CheckBalance()
        {
            decimal balance;
            if (atm.CheckBalance(out balance))
                ShowState(ScreenState.Balance);
        }

        private void Withdraw(decimal amount)
        {
            if (atm.Withdraw(amount))
                ShowState(ScreenState.Menu);
            else
                PressClear();
        }

        private void Deposit(decimal amount)
        {
            if (atm.Deposit(amount))
                ShowState(ScreenState.Menu);
            else
                PressClear();
        }

        private void EjectCard()
        {
            atm.EjectCard();
            ShowState(ScreenState.InsertCard);
        }

        private void ShowLocalError(string message)
        {
            lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
            lblStatus.Text = message;
        }

        // =====================================================================
        //  Сервісна панель
        // =====================================================================

        private void OnAtmChanged(object sender, EventArgs e)
        {
            AutomatedTellerMachine selected = bank.Atms[cmbAtm.SelectedIndex];
            if (selected == atm) return;

            if (atm.IsCardInserted)
            {
                MessageBox.Show("Спершу заберіть картку з поточного банкомата.", "Зміна банкомата",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbAtm.SelectedIndex = cmbAtm.Items.IndexOf(atm.Id);
                return;
            }

            // Відписуємо обробники від старого банкомата і підписуємо на новий
            UnsubscribeAll(atm);
            atm = selected;
            SubscribeAll(atm);
            AddJournal("--- Обрано банкомат " + atm.Id + " ---");
            lblStatus.Text = string.Empty;
            ShowState(ScreenState.InsertCard);
        }

        private void RefreshServicePanel()
        {
            lblAtmInfo.Text = "ID: " + atm.Id + "\nАдреса: " + atm.Address + "\nГотівка в банкоматі: " + Money.Format(atm.CashAmount);

            object selected = lstCards.SelectedItem;
            string selectedCard = selected is CardItem ? ((CardItem)selected).Account.CardNumber : null;
            lstCards.BeginUpdate();
            lstCards.Items.Clear();
            foreach (Account account in bank.Accounts)
            {
                var item = new CardItem(account);
                lstCards.Items.Add(item);
                if (account.CardNumber == selectedCard)
                    lstCards.SelectedItem = item;
            }
            lstCards.EndUpdate();

            Account inSlot = atm.CurrentAccount;
            lblCardInSlot.Text = atm.IsCardInserted
                ? "▬ картка ▬\n" + (inSlot != null ? inSlot.MaskedCardNumber.Substring(10) : "очікує PIN")
                : string.Empty;
            btnInsertCard.Enabled = !atm.IsCardInserted;
            cmbAtm.Enabled = !atm.IsCardInserted;
        }

        private void AddJournal(string line)
        {
            lstJournal.Items.Add(line);
            lstJournal.TopIndex = lstJournal.Items.Count - 1;
        }

        // =====================================================================
        //  Клавіатура ПК
        // =====================================================================

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData >= Keys.D0 && keyData <= Keys.D9)
            {
                PressDigit((char)('0' + (keyData - Keys.D0)));
                return true;
            }
            if (keyData >= Keys.NumPad0 && keyData <= Keys.NumPad9)
            {
                PressDigit((char)('0' + (keyData - Keys.NumPad0)));
                return true;
            }
            if (keyData >= Keys.F1 && keyData <= Keys.F4)
            {
                PressSideButton(true, keyData - Keys.F1);
                return true;
            }
            if (keyData >= Keys.F5 && keyData <= Keys.F8)
            {
                PressSideButton(false, keyData - Keys.F5);
                return true;
            }
            switch (keyData)
            {
                case Keys.Enter:
                    PressEnter();
                    return true;
                case Keys.Escape:
                    PressCancel();
                    return true;
                case Keys.Back:
                    PressBackspace();
                    return true;
                case Keys.Delete:
                    PressClear();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>Елемент списку тестових карток.</summary>
        private sealed class CardItem
        {
            public CardItem(Account account)
            {
                Account = account;
            }

            public Account Account { get; private set; }

            public override string ToString()
            {
                string pin;
                DemoBankFactory.PinHints.TryGetValue(Account.CardNumber, out pin);
                return Account.FormattedCardNumber + " PIN " + pin + " " + Account.OwnerName +
                       (Account.IsBlocked ? " [блок]" : " " + Money.Format(Account.Balance));
            }
        }
    }
}
