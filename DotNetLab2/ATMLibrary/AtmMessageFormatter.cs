namespace ATMLibrary
{
    /// <summary>
    /// Формує текст повідомлення для обробників подій
    /// (однаковий для консольного та віконного застосунку).
    /// </summary>
    public static class AtmMessageFormatter
    {
        public static string Title(AtmEventArgs e)
        {
            if (!e.Success) return "Помилка: " + OperationName(e.Operation);
            return OperationName(e.Operation);
        }

        public static string OperationName(OperationType op)
        {
            switch (op)
            {
                case OperationType.CardInserted: return "Прийом картки";
                case OperationType.Authentication: return "Аутентифікація";
                case OperationType.BalanceCheck: return "Перегляд балансу";
                case OperationType.Withdrawal: return "Зняття готівки";
                case OperationType.Deposit: return "Поповнення картки";
                case OperationType.Transfer: return "Переказ на картку";
                case OperationType.SessionEnd: return "Завершення сеансу";
                default: return op.ToString();
            }
        }

        /// <summary>Рядок для журналу: [12:30:15] ATM-001 | Зняття готівки | OK | ...</summary>
        public static string LogLine(AtmEventArgs e)
        {
            return string.Format("[{0:HH:mm:ss}] {1} | {2} | {3} | {4}",
                e.Time, e.AtmId, OperationName(e.Operation), e.Success ? "OK" : "ПОМИЛКА", e.Message);
        }

        /// <summary>Текст електронного листа.</summary>
        public static string EmailBody(AtmEventArgs e)
        {
            string text = "Шановний(а) " + (e.Account != null ? e.Account.OwnerName : "клієнте") + "!\n\n" +
                          "Операція: " + OperationName(e.Operation) + "\n" +
                          "Статус: " + (e.Success ? "успішно" : "відхилено") + "\n" +
                          "Банкомат: " + e.AtmId + "\n" +
                          "Час: " + e.Time.ToString("dd.MM.yyyy HH:mm:ss") + "\n";
            if (e.Account != null)
                text += "Картка: " + e.Account.MaskedCardNumber + "\n";
            if (e.Amount > 0)
                text += "Сума: " + Money.Format(e.Amount) + "\n";
            if (e.TargetAccount != null)
                text += "Отримувач: " + e.TargetAccount.MaskedCardNumber + "\n";
            if (e.Account != null && e.Success)
                text += "Баланс: " + Money.Format(e.BalanceAfter) + "\n";
            text += "\n" + e.Message + "\n";
            return text;
        }
    }
}
