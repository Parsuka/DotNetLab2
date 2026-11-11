using System;
using System.Drawing;
using System.Windows.Forms;

namespace ATMWinForms
{
    /// <summary>
    /// Частина форми, що відповідає за побудову інтерфейсу «під банкомат».
    /// Елементи створюються в коді, щоб розмітку було легко читати.
    /// </summary>
    public partial class MainForm
    {
        // Кольори
        private static readonly Color BodyColor = Color.FromArgb(52, 58, 68);
        private static readonly Color BodyDark = Color.FromArgb(36, 40, 48);
        private static readonly Color ScreenColor = Color.FromArgb(14, 52, 92);
        private static readonly Color ScreenInputColor = Color.FromArgb(28, 78, 128);
        private static readonly Color KeyColor = Color.FromArgb(214, 217, 222);
        private static readonly Color SideKeyColor = Color.FromArgb(150, 156, 166);

        // Екран банкомата
        private Panel screen;
        private Label lblTitle;
        private Label lblBody;
        private Label lblInput;
        private Label lblStatus;
        private readonly Label[] leftLabels = new Label[4];
        private readonly Label[] rightLabels = new Label[4];
        private readonly Button[] leftButtons = new Button[4];
        private readonly Button[] rightButtons = new Button[4];

        // Корпус
        private Label lblBankName;
        private Panel cardSlot;
        private Label lblCardInSlot;
        private Panel cashSlot;
        private Label lblCashOut;

        // Сервісна панель
        private ComboBox cmbAtm;
        private Label lblAtmInfo;
        private ListBox lstCards;
        private Button btnInsertCard;
        private CheckBox chkMessageBox;
        private CheckBox chkJournal;
        private CheckBox chkEmail;
        private ListBox lstJournal;

        private void BuildLayout()
        {
            SuspendLayout();

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Банкомат — Лабораторна робота №2 (делегати та події)";
            ClientSize = new Size(1000, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(232, 234, 238);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            Panel body = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(640, 660),
                BackColor = BodyColor
            };
            Controls.Add(body);

            lblBankName = new Label
            {
                Location = new Point(0, 8),
                Size = new Size(640, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            body.Controls.Add(lblBankName);

            BuildScreen(body);
            BuildKeypad(body);
            BuildSlots(body);
            BuildServicePanel();

            ResumeLayout(false);
            PerformLayout();
        }

        private void BuildScreen(Panel body)
        {
            // Рамка навколо екрана
            Panel bezel = new Panel
            {
                Location = new Point(84, 44),
                Size = new Size(472, 332),
                BackColor = BodyDark
            };
            body.Controls.Add(bezel);

            screen = new Panel
            {
                Location = new Point(6, 6),
                Size = new Size(460, 320),
                BackColor = ScreenColor
            };
            bezel.Controls.Add(screen);

            lblTitle = new Label
            {
                Location = new Point(0, 8),
                Size = new Size(460, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            screen.Controls.Add(lblTitle);

            lblBody = new Label
            {
                Location = new Point(10, 40),
                Size = new Size(440, 44),
                ForeColor = Color.FromArgb(205, 225, 245),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleCenter
            };
            screen.Controls.Add(lblBody);

            lblInput = new Label
            {
                Location = new Point(80, 90),
                Size = new Size(300, 36),
                BackColor = ScreenInputColor,
                ForeColor = Color.White,
                Font = new Font("Consolas", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            screen.Controls.Add(lblInput);

            // 4 рядки підписів біля бічних кнопок
            for (int i = 0; i < 4; i++)
            {
                int y = RowCenter(i) - 14;

                leftLabels[i] = new Label
                {
                    Location = new Point(8, y),
                    Size = new Size(222, 28),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                screen.Controls.Add(leftLabels[i]);

                rightLabels[i] = new Label
                {
                    Location = new Point(230, y),
                    Size = new Size(222, 28),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleRight
                };
                screen.Controls.Add(rightLabels[i]);

                // Бічні кнопки на корпусі (на рівні рядка екрана)
                int buttonY = 44 + 6 + RowCenter(i) - 17;
                int index = i;

                leftButtons[i] = CreateKey("▶", SideKeyColor, new Point(18, buttonY), new Size(58, 34));
                leftButtons[i].Click += (s, e) => PressSideButton(true, index);
                body.Controls.Add(leftButtons[i]);

                rightButtons[i] = CreateKey("◀", SideKeyColor, new Point(564, buttonY), new Size(58, 34));
                rightButtons[i].Click += (s, e) => PressSideButton(false, index);
                body.Controls.Add(rightButtons[i]);
            }

            lblStatus = new Label
            {
                Location = new Point(8, 286),
                Size = new Size(444, 30),
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            screen.Controls.Add(lblStatus);
        }

        /// <summary>Вертикальний центр i-го рядка меню на екрані.</summary>
        private static int RowCenter(int row)
        {
            return 146 + row * 37;
        }

        private void BuildKeypad(Panel body)
        {
            Panel keypad = new Panel
            {
                Location = new Point(84, 396),
                Size = new Size(380, 248),
                BackColor = BodyDark
            };
            body.Controls.Add(keypad);

            string[] digits = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "" };
            for (int i = 0; i < digits.Length; i++)
            {
                int col = i % 3, row = i / 3;
                Point location = new Point(12 + col * 76, 12 + row * 58);
                if (digits[i].Length == 0)
                {
                    Button blank = CreateKey("", KeyColor, location, new Size(68, 50));
                    blank.Enabled = false;
                    keypad.Controls.Add(blank);
                    continue;
                }
                string digit = digits[i];
                Button key = CreateKey(digit, KeyColor, location, new Size(68, 50));
                key.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
                key.Click += (s, e) => PressDigit(digit[0]);
                keypad.Controls.Add(key);
            }

            Button cancel = CreateKey("СКАСУВАТИ", Color.FromArgb(205, 55, 55), new Point(248, 12), new Size(120, 50));
            cancel.ForeColor = Color.White;
            cancel.Click += (s, e) => PressCancel();
            keypad.Controls.Add(cancel);

            Button clear = CreateKey("ВИПРАВИТИ", Color.FromArgb(235, 190, 40), new Point(248, 70), new Size(120, 50));
            clear.Click += (s, e) => PressClear();
            keypad.Controls.Add(clear);

            Button enter = CreateKey("ВВІД", Color.FromArgb(46, 160, 80), new Point(248, 128), new Size(120, 50));
            enter.ForeColor = Color.White;
            enter.Click += (s, e) => PressEnter();
            keypad.Controls.Add(enter);

            Label hint = new Label
            {
                Location = new Point(248, 186),
                Size = new Size(124, 54),
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 7.5F),
                Text = "Клавіатура ПК:\nцифри, Enter, Esc,\nBackspace, F1–F8"
            };
            keypad.Controls.Add(hint);
        }

        private void BuildSlots(Panel body)
        {
            Label cardCaption = new Label
            {
                Location = new Point(482, 396),
                Size = new Size(140, 18),
                ForeColor = Color.Silver,
                Text = "КАРТКА",
                TextAlign = ContentAlignment.MiddleCenter
            };
            body.Controls.Add(cardCaption);

            cardSlot = new Panel
            {
                Location = new Point(482, 416),
                Size = new Size(140, 70),
                BackColor = BodyDark
            };
            body.Controls.Add(cardSlot);

            Panel slotLine = new Panel
            {
                Location = new Point(15, 8),
                Size = new Size(110, 6),
                BackColor = Color.Black
            };
            cardSlot.Controls.Add(slotLine);

            lblCardInSlot = new Label
            {
                Location = new Point(10, 20),
                Size = new Size(120, 44),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardSlot.Controls.Add(lblCardInSlot);

            Label cashCaption = new Label
            {
                Location = new Point(482, 500),
                Size = new Size(140, 18),
                ForeColor = Color.Silver,
                Text = "ВИДАЧА ГОТІВКИ",
                TextAlign = ContentAlignment.MiddleCenter
            };
            body.Controls.Add(cashCaption);

            cashSlot = new Panel
            {
                Location = new Point(482, 520),
                Size = new Size(140, 124),
                BackColor = BodyDark
            };
            body.Controls.Add(cashSlot);

            Panel cashLine = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(120, 10),
                BackColor = Color.Black
            };
            cashSlot.Controls.Add(cashLine);

            lblCashOut = new Label
            {
                Location = new Point(10, 28),
                Size = new Size(120, 88),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cashSlot.Controls.Add(lblCashOut);
        }

        private void BuildServicePanel()
        {
            GroupBox group = new GroupBox
            {
                Location = new Point(680, 14),
                Size = new Size(300, 666),
                Text = "Сервісна панель (для тестування)"
            };
            Controls.Add(group);

            Label lblAtm = new Label { Location = new Point(12, 26), Size = new Size(80, 22), Text = "Банкомат:", TextAlign = ContentAlignment.MiddleLeft };
            group.Controls.Add(lblAtm);

            cmbAtm = new ComboBox
            {
                Location = new Point(92, 26),
                Size = new Size(196, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            group.Controls.Add(cmbAtm);

            lblAtmInfo = new Label
            {
                Location = new Point(12, 54),
                Size = new Size(276, 54),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            group.Controls.Add(lblAtmInfo);

            Label lblCards = new Label { Location = new Point(12, 110), Size = new Size(276, 18), Text = "Тестові картки (подвійний клік — вставити):" };
            group.Controls.Add(lblCards);

            lstCards = new ListBox
            {
                Location = new Point(12, 130),
                Size = new Size(276, 100),
                Font = new Font("Consolas", 8.5F),
                HorizontalScrollbar = true
            };
            lstCards.DoubleClick += (s, e) => InsertSelectedCard();
            group.Controls.Add(lstCards);

            btnInsertCard = new Button
            {
                Location = new Point(12, 234),
                Size = new Size(276, 28),
                Text = "Вставити вибрану картку"
            };
            btnInsertCard.Click += (s, e) => InsertSelectedCard();
            group.Controls.Add(btnInsertCard);

            Label lblHandlers = new Label
            {
                Location = new Point(12, 272),
                Size = new Size(276, 18),
                Text = "Обробники подій (+= / -=):",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            group.Controls.Add(lblHandlers);

            chkMessageBox = new CheckBox { Location = new Point(14, 292), Size = new Size(274, 22), Text = "Повідомлення MessageBox", Checked = true };
            group.Controls.Add(chkMessageBox);

            chkJournal = new CheckBox { Location = new Point(14, 314), Size = new Size(274, 22), Text = "Журнал операцій", Checked = true };
            group.Controls.Add(chkJournal);

            chkEmail = new CheckBox { Location = new Point(14, 336), Size = new Size(274, 22), Text = "Сповіщення на e-mail" };
            group.Controls.Add(chkEmail);

            Label lblJournal = new Label { Location = new Point(12, 364), Size = new Size(276, 18), Text = "Журнал подій:" };
            group.Controls.Add(lblJournal);

            lstJournal = new ListBox
            {
                Location = new Point(12, 384),
                Size = new Size(276, 270),
                Font = new Font("Consolas", 8F),
                HorizontalScrollbar = true
            };
            group.Controls.Add(lstJournal);
        }

        private static Button CreateKey(string text, Color color, Point location, Size size)
        {
            Button button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TabStop = false,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            return button;
        }
    }
}
