using System;

namespace ATMLibrary
{
    /// <summary>
    /// Дані, що передаються обробникам подій банкомата.
    /// </summary>
    public class AtmEventArgs : EventArgs
    {
        public AtmEventArgs(OperationType operation, bool success, string message)
        {
            Operation = operation;
            Success = success;
            Message = message;
            Time = DateTime.Now;
        }

        /// <summary>Тип операції.</summary>
        public OperationType Operation { get; private set; }

        /// <summary>Чи успішно виконано операцію.</summary>
        public bool Success { get; private set; }

        /// <summary>Текст повідомлення для користувача.</summary>
        public string Message { get; private set; }

        /// <summary>Час виконання операції.</summary>
        public DateTime Time { get; private set; }

        /// <summary>Рахунок, з яким виконується операція (може бути null).</summary>
        public Account Account { get; set; }

        /// <summary>Сума операції.</summary>
        public decimal Amount { get; set; }

        /// <summary>Баланс рахунку після операції.</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>Рахунок отримувача (для переказу).</summary>
        public Account TargetAccount { get; set; }

        /// <summary>Ідентифікатор банкомата, що згенерував подію.</summary>
        public string AtmId { get; set; }
    }
}
