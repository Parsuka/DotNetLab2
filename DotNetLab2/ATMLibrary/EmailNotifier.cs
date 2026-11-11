using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ATMLibrary
{
    /// <summary>
    /// Обробник подій, що надсилає повідомлення на електронну пошту власника картки.
    ///
    /// Логін і пароль НЕ зберігаються в коді: вони читаються зі змінних середовища
    /// ATM_SMTP_USER та ATM_SMTP_PASSWORD. Для Gmail потрібен "пароль застосунку"
    /// (App password), бо доступ ненадійних застосунків Google вимкнув.
    /// </summary>
    public class EmailNotifier
    {
        public const string UserVariable = "ATM_SMTP_USER";
        public const string PasswordVariable = "ATM_SMTP_PASSWORD";

        public EmailNotifier(string senderEmail, string password, string smtpHost = "smtp.gmail.com", int port = 587)
        {
            SenderEmail = senderEmail;
            Password = password;
            SmtpHost = smtpHost;
            Port = port;
        }

        public string SmtpHost { get; private set; }
        public int Port { get; private set; }
        public string SenderEmail { get; private set; }
        private string Password { get; set; }

        /// <summary>
        /// Подія для повідомлення про результат відправлення.
        /// Використовує вбудований делегат Action&lt;string&gt;.
        /// </summary>
        public event Action<string> StatusChanged;

        /// <summary>
        /// Створює EmailNotifier зі змінних середовища.
        /// Повертає null, якщо змінні не задано (тоді пошта просто не використовується).
        /// </summary>
        public static EmailNotifier FromEnvironment()
        {
            string user = Environment.GetEnvironmentVariable(UserVariable);
            string password = Environment.GetEnvironmentVariable(PasswordVariable);
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
                return null;
            return new EmailNotifier(user, password);
        }

        /// <summary>Підписує цей обробник на всі події операцій банкомата.</summary>
        public void Subscribe(AutomatedTellerMachine atm)
        {
            atm.Authenticated += OnAtmOperation;
            atm.BalanceChecked += OnAtmOperation;
            atm.Withdrawn += OnAtmOperation;
            atm.Deposited += OnAtmOperation;
            atm.Transferred += OnAtmOperation;
        }

        /// <summary>Відписує обробник від подій банкомата.</summary>
        public void Unsubscribe(AutomatedTellerMachine atm)
        {
            atm.Authenticated -= OnAtmOperation;
            atm.BalanceChecked -= OnAtmOperation;
            atm.Withdrawn -= OnAtmOperation;
            atm.Deposited -= OnAtmOperation;
            atm.Transferred -= OnAtmOperation;
        }

        /// <summary>Обробник події (сигнатура відповідає делегату AtmEventHandler).</summary>
        public void OnAtmOperation(object sender, AtmEventArgs e)
        {
            if (e.Account == null || string.IsNullOrWhiteSpace(e.Account.Email))
                return;

            string to = e.Account.Email;
            string subject = "Банкомат " + e.AtmId + ": " + AtmMessageFormatter.Title(e);
            string body = AtmMessageFormatter.EmailBody(e);

            // Відправлення у фоновому потоці, щоб не "підвисав" інтерфейс
            Task.Run(() => Send(to, subject, body));
        }

        public void Send(string to, string subject, string body)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpHost)
                {
                    Port = Port,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(SenderEmail, Password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                })
                {
                    smtpClient.Send(SenderEmail, to, subject, body);
                }
                ReportStatus("Лист надіслано на " + to);
            }
            catch (Exception ex)
            {
                ReportStatus("Не вдалося надіслати лист на " + to + ": " + ex.Message);
            }
        }

        private void ReportStatus(string message)
        {
            Action<string> handler = StatusChanged;
            if (handler != null) handler(message);
        }
    }
}
