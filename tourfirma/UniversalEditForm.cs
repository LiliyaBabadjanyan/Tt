using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace tourfirma
{
    public partial class UniversalEditForm : Form
    {
        private readonly string _activeTab; // Активная вкладка (определяет тип сущности)
        private readonly DataGridViewRow _selectedRow; // Выбранная строка (null при добавлении)
        private readonly NpgsqlConnection _connection; // Подключение к БД
        private int _currentTop = 20; // Текущая позиция Y для размещения элементов
        private const int ControlWidth = 250; // Ширина элементов управления
        private const int LabelWidth = 120; // Ширина меток

        // Конструктор формы
        public UniversalEditForm(string activeTab, DataGridViewRow selectedRow, NpgsqlConnection connection)
        {
            InitializeComponent();
            _activeTab = activeTab;
            _selectedRow = selectedRow;
            _connection = connection;
            ConfigureForm(); // Настройка формы
        }

        // Основная настройка формы
        private void ConfigureForm()
        {
            // Установка заголовка в зависимости от действия
            this.Text = _selectedRow == null ? "Добавить запись" : "Редактировать запись";
            this.ClientSize = new Size(400, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Выбор конфигурации в зависимости от типа сущности
            switch (_activeTab)
            {
                case "tabPage2": // Туристы
                    ConfigureTouristTab();
                    break;

                case "tabPage3": // Сезоны
                    ConfigureSeasonsTab();
                    break;

                case "tabPage4": // Путевки
                    ConfigurePutevkiTab();
                    break;

                case "tabPage5": // Оплата
                    ConfigurePaymentTab();
                    break;
                case "tabPage1": // Оплата
                    ConfigureToursTab();
                    break;
            }

            // Добавление кнопок сохранения/отмены
            AddSaveButton();

            // Если редактируем существующую запись - загружаем данные
            if (_selectedRow != null)
            {
                LoadDataFromRow();
            }
        }

        // Конфигурация формы для работы с туристами
        private void ConfigureTouristTab()
        {
            AddTextField("Фамилия*", "tourist_surname");
            AddTextField("Имя*", "tourist_name");
            AddTextField("Отчество", "tourist_otch");
            AddTextField("Паспорт*", "passport", 10);
            AddTextField("Город*", "city");
            AddTextField("Страна*", "country");
            AddTextField("Телефон*", "phone", 13);
            AddRequiredFieldsHint();
        }

        // Конфигурация формы для работы с сезонами
        private void ConfigureSeasonsTab()
        {
            AddComboBoxField("Тур*", "tour_id", "SELECT tour_id, tour_name FROM tours");
            AddDateField("Дата начала*", "start_date");
            AddDateField("Дата окончания*", "end_date");
            AddCheckBoxField("Закрыт", "closed");
            AddNumericField("Количество*", "amount");
            AddRequiredFieldsHint();
        }

        // Конфигурация формы для работы с путевками
        private void ConfigurePutevkiTab()
        {
            // Комбобокс для выбора туриста с форматированным отображением
            AddComboBoxField("Турист*", "tourist_id",
                "SELECT t.tourist_id, t.tourist_surname || ' ' || t.tourist_name || ' (паспорт: ' || t.passport || ')' AS display_info " +
                "FROM tourists t " +
                "ORDER BY t.tourist_surname");

            // Комбобокс для выбора сезона с форматированным отображением
            AddComboBoxField("Сезон*", "season_id",
                "SELECT s.season_id, t.tour_name || ' (' || s.start_date || ' - ' || s.end_date || ')' AS display_info " +
                "FROM seasons s " +
                "JOIN tours t ON s.tour_id = t.tour_id");

            AddRequiredFieldsHint();
            AddAddNewTouristButton(); // Кнопка быстрого добавления туриста
        }

        // Конфигурация формы для работы с платежами
        private void ConfigurePaymentTab()
        {
            // Комбобокс для выбора путевки с форматированным отображением
            AddComboBoxField("Путевка*", "putevki_id",
                "SELECT p.putevki_id, t.tourist_surname || ' ' || t.tourist_name || ' (' || s.start_date || ')' AS display_info " +
                "FROM putevki p " +
                "JOIN tourists t ON p.tourist_id = t.tourist_id " +
                "JOIN seasons s ON p.season_id = s.season_id");

            AddDateField("Дата оплаты*", "payment_date", DateTime.Today);
            AddNumericField("Сумма*", "summa", decimal.MaxValue);
            AddRequiredFieldsHint();
        }

        private void ConfigureToursTab()
        {
            // Поле для ввода названия тура
            AddTextField("Название тура*", "tour_name");

            // Поле для ввода цены тура с ограничением на максимально возможную цену
            AddNumericField("Цена тура*", "price", decimal.MaxValue);

            // Поле для ввода описания тура (по желанию, оно может быть необязательным)
            AddTextField("Описание тура", "tour_info");

            // Добавление подсказки для обязательных полей
            AddRequiredFieldsHint();
        }


        // Добавление текстового поля
        private void AddTextField(string labelText, string fieldName, int maxLength = 0)
        {
            var label = new Label
            {
                Text = labelText,
                Top = _currentTop,
                Left = 10,
                Width = LabelWidth,
                TextAlign = ContentAlignment.MiddleRight
            };

            var textBox = new TextBox
            {
                Name = fieldName,
                Tag = fieldName, // Tag используется для связи с именем поля в БД
                Top = _currentTop,
                Left = LabelWidth + 20,
                Width = ControlWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            if (maxLength > 0) textBox.MaxLength = maxLength;

            this.Controls.Add(label);
            this.Controls.Add(textBox);
            _currentTop += 30; // Увеличиваем позицию для следующего элемента
        }

        // Добавление выпадающего списка с данными из БД
        private void AddComboBoxField(string labelText, string fieldName, string query)
        {
            var label = new Label
            {
                Text = labelText,
                Top = _currentTop,
                Left = 10,
                Width = LabelWidth,
                TextAlign = ContentAlignment.MiddleRight
            };

            var comboBox = new ComboBox
            {
                Name = fieldName,
                Tag = fieldName,
                Top = _currentTop,
                Left = LabelWidth + 20,
                Width = ControlWidth,
                DropDownStyle = ComboBoxStyle.DropDownList, // Запрет ввода текста
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            try
            {
                // Загрузка данных из БД в комбобокс
                var dt = new DataTable();
                new NpgsqlDataAdapter(query, _connection).Fill(dt);
                // Второй столбец используется для отображения (если есть)
                comboBox.DisplayMember = dt.Columns.Count > 1 ? dt.Columns[1].ColumnName : dt.Columns[0].ColumnName;
                comboBox.ValueMember = dt.Columns[0].ColumnName; // Первый столбец - значение
                comboBox.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }

            this.Controls.Add(label);
            this.Controls.Add(comboBox);
            _currentTop += 30;
        }

        // Добавление поля выбора даты
        private void AddDateField(string labelText, string fieldName, DateTime? defaultDate = null)
        {
            var label = new Label
            {
                Text = labelText,
                Top = _currentTop,
                Left = 10,
                Width = LabelWidth,
                TextAlign = ContentAlignment.MiddleRight
            };

            var datePicker = new DateTimePicker
            {
                Name = fieldName,
                Tag = fieldName,
                Top = _currentTop,
                Left = LabelWidth + 20,
                Width = ControlWidth,
                Format = DateTimePickerFormat.Short, // Короткий формат даты
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            if (defaultDate.HasValue) datePicker.Value = defaultDate.Value;

            this.Controls.Add(label);
            this.Controls.Add(datePicker);
            _currentTop += 30;
        }

        // Добавление чекбокса
        private void AddCheckBoxField(string labelText, string fieldName)
        {
            var checkBox = new CheckBox
            {
                Text = labelText,
                Name = fieldName,
                Tag = fieldName,
                Top = _currentTop,
                Left = LabelWidth + 20,
                Width = ControlWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.Add(checkBox);
            _currentTop += 30;
        }

        // Добавление числового поля
        private void AddNumericField(string labelText, string fieldName, decimal maxValue = 1000000)
        {
            var label = new Label
            {
                Text = labelText,
                Top = _currentTop,
                Left = 10,
                Width = LabelWidth,
                TextAlign = ContentAlignment.MiddleRight
            };

            var numericUpDown = new NumericUpDown
            {
                Name = fieldName,
                Tag = fieldName,
                Top = _currentTop,
                Left = LabelWidth + 20,
                Width = ControlWidth,
                Maximum = maxValue,
                DecimalPlaces = 2, // 2 знака после запятой
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.Add(label);
            this.Controls.Add(numericUpDown);
            _currentTop += 30;
        }

        // Добавление подсказки об обязательных полях
        private void AddRequiredFieldsHint()
        {
            var hint = new Label
            {
                Text = "* - обязательные поля",
                Top = _currentTop,
                Left = 10,
                ForeColor = Color.Gray,
                Font = new Font(this.Font, FontStyle.Italic)
            };
            this.Controls.Add(hint);
            _currentTop += 25;
        }

        // Добавление кнопки для быстрого создания нового туриста
        private void AddAddNewTouristButton()
        {
            var btn = new Button
            {
                Text = "Добавить нового туриста",
                Top = _currentTop,
                Left = LabelWidth + 20,
                Width = ControlWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btn.Click += (s, e) => {
                // Открываем форму добавления туриста
                var touristForm = new UniversalEditForm("tabPage2", null, _connection);
                if (touristForm.ShowDialog() == DialogResult.OK)
                {
                    // После добавления обновляем список туристов
                    var combo = this.Controls.Find("tourist_id", true).FirstOrDefault() as ComboBox;
                    if (combo != null)
                    {
                        var dt = new DataTable();
                        new NpgsqlDataAdapter(
                            "SELECT tourist_id, tourist_surname || ' ' || tourist_name || ' (паспорт: ' || passport || ')' AS display_info " +
                            "FROM tourists ORDER BY tourist_id DESC LIMIT 1",
                            _connection).Fill(dt);
                        combo.DataSource = dt;
                        combo.SelectedValue = dt.Rows[0]["tourist_id"]; // Выбираем только что добавленного туриста
                    }
                }
            };

            this.Controls.Add(btn);
            _currentTop += 35;
        }

        // Добавление кнопок сохранения и отмены
        private void AddSaveButton()
        {
            var btnSave = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Top = _currentTop + 10,
                Left = LabelWidth + 20,
                Width = ControlWidth / 2 - 5,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnSave.Click += btnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Top = _currentTop + 10,
                Left = LabelWidth + 20 + ControlWidth / 2 + 5,
                Width = ControlWidth / 2 - 5,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            this.Controls.Add(btnCancel);
        }

        // Загрузка данных из выбранной строки в элементы управления
        private void LoadDataFromRow()
        {
            foreach (Control control in this.Controls)
            {
                // Проверяем, что элемент связан с полем в БД и в строке есть значение
                if (control.Tag != null && _selectedRow.Cells[control.Tag.ToString()].Value != null)
                {
                    var value = _selectedRow.Cells[control.Tag.ToString()].Value;

                    // Установка значения в зависимости от типа элемента
                    if (control is TextBox textBox)
                    {
                        textBox.Text = value.ToString();
                    }
                    else if (control is ComboBox comboBox)
                    {
                        comboBox.SelectedValue = value;
                    }
                    else if (control is DateTimePicker datePicker && value is DateTime)
                    {
                        datePicker.Value = (DateTime)value;
                    }
                    else if (control is CheckBox checkBox)
                    {
                        checkBox.Checked = Convert.ToBoolean(value);
                    }
                    else if (control is NumericUpDown numericUpDown)
                    {
                        numericUpDown.Value = Convert.ToDecimal(value);
                    }
                }
            }
        }

        // Обработчик сохранения данных
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateBeforeSave()) return; // Проверка валидации

            try
            {
                // Сохранение в транзакции
                using (var transaction = _connection.BeginTransaction())
                {
                    if (_selectedRow == null)
                    {
                        InsertData(transaction); // Вставка новой записи
                    }
                    else
                    {
                        UpdateData(transaction); // Обновление существующей
                    }
                    transaction.Commit();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503")
            {
                // Обработка ошибки внешнего ключа
                ShowForeignKeyError(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Валидация перед сохранением
        private bool ValidateBeforeSave()
        {
            foreach (Control control in this.Controls)
            {
                // Проверяем обязательные поля (помеченные *)
                if (control is Label label && label.Text.EndsWith("*"))
                {
                    string fieldName = label.Text.Replace("*", "").Trim();
                    // Находим соответствующий элемент управления
                    var inputControl = this.Controls
                        .OfType<Control>()
                        .FirstOrDefault(c => c.Tag?.ToString() == fieldName);

                    // Проверка текстового поля
                    if (inputControl is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        MessageBox.Show($"Поле '{fieldName}' обязательно для заполнения!", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    // Проверка выпадающего списка
                    else if (inputControl is ComboBox comboBox && comboBox.SelectedValue == null)
                    {
                        MessageBox.Show($"Необходимо выбрать значение для '{fieldName}'!", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }
            return true;
        }

        // Показать понятное сообщение об ошибке внешнего ключа
        private void ShowForeignKeyError(Npgsql.PostgresException ex)
        {
            string errorMessage = "Ошибка целостности данных:\n";

            // Определяем тип ошибки по имени ограничения
            if (ex.Message.Contains("putevki_tourist_id_fkey"))
            {
                errorMessage += "Выбранный турист не найден в системе.\n";
                errorMessage += "Пожалуйста, выберите существующего туриста.";
            }
            else if (ex.Message.Contains("putevki_season_id_fkey"))
            {
                errorMessage += "Выбранный сезон не найден в системе.\n";
                errorMessage += "Пожалуйста, выберите существующий сезон.";
            }
            else
            {
                errorMessage += ex.Message;
                if (ex.Detail != null) errorMessage += "\n\nДетали: " + ex.Detail;
            }

            MessageBox.Show(errorMessage, "Ошибка сохранения",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Вставка новой записи
        private void InsertData(NpgsqlTransaction transaction)
        {
            string tableName = GetTableName();
            // Создаем команду с SQL для вставки
            var cmd = new NpgsqlCommand(GetInsertSql(tableName), _connection, transaction);

            // Добавляем параметры из элементов управления
            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    cmd.Parameters.AddWithValue($"@{control.Tag}", GetControlValue(control, false));
                }
            }

            // Выполняем запрос и получаем ID новой записи
            var result = cmd.ExecuteScalar();
            MessageBox.Show($"Запись успешно добавлена с ID: {result}", "Успех",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Обновление существующей записи
        void UpdateData(NpgsqlTransaction transaction)
        {
            string tableName = GetTableName();
            var cmd = new NpgsqlCommand(GetUpdateSql(tableName), _connection, transaction);

            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    cmd.Parameters.AddWithValue($"@{control.Tag}", GetControlValue(control, false));
                }
            }

            // 👉 Добавляем параметр ID вручную
            cmd.Parameters.AddWithValue($"@{GetIdColumnName()}", _selectedRow.Cells[GetIdColumnName()].Value);

            int affected = cmd.ExecuteNonQuery();
            if (affected > 0)
            {
                MessageBox.Show("Запись успешно обновлена", "Успех",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Генерация SQL для вставки
        private string GetInsertSql(string tableName)
        {
            // Собираем имена столбцов
            string columns = string.Join(", ", this.Controls
                .OfType<Control>()
                .Where(c => c.Tag != null)
                .Select(c => c.Tag.ToString()));

            // Собираем параметры
            string values = string.Join(", ", this.Controls
                .OfType<Control>()
                .Where(c => c.Tag != null)
                .Select(c => $"@{c.Tag}"));

            // Возвращаем SQL с RETURNING для получения ID
            return $"INSERT INTO {tableName} ({columns}) VALUES ({values}) RETURNING {GetIdColumnName()}";
        }

        // Генерация SQL для обновления
        private string GetUpdateSql(string tableName)
        {
            // Собираем пары столбец=значение для SET
            string setClause = string.Join(", ", this.Controls
                .OfType<Control>()
                .Where(c => c.Tag != null && c.Tag.ToString() != GetIdColumnName())
                .Select(c => $"{c.Tag} = @{c.Tag}"));

            // Условие WHERE по ID
            return $"UPDATE {tableName} SET {setClause} WHERE {GetIdColumnName()} = @{GetIdColumnName()}";
        }

        // Получение имени таблицы для текущей вкладки
        private string GetTableName()
        {
            return _activeTab switch
            {
                "tabPage2" => "tourists",
                "tabPage3" => "seasons",
                "tabPage4" => "putevki",
                "tabPage5" => "payment",
                "tabPage1" => "tours",
                _ => throw new Exception("Неизвестная таблица")
            };
        }

        // Получение имени столбца с ID для текущей таблицы
        private string GetIdColumnName()
        {
            return _activeTab switch
            {
                "tabPage2" => "tourist_id",
                "tabPage3" => "season_id",
                "tabPage4" => "putevki_id",
                "tabPage5" => "payment_id",
                "tabPage1" => "tour_id",
                _ => throw new Exception("Неизвестный ID столбец")
            };
        }

        // Получение значения из элемента управления
        private object GetControlValue(Control control, bool forSql)
        {
            if (control is TextBox textBox)
                return textBox.Text;
            if (control is ComboBox comboBox)
                return comboBox.SelectedValue ?? DBNull.Value;
            if (control is DateTimePicker datePicker)
                return datePicker.Value;
            if (control is CheckBox checkBox)
                return checkBox.Checked;
            if (control is NumericUpDown numericUpDown)
                return numericUpDown.Value;

            return DBNull.Value;
        }
    }
}