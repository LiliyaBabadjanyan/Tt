using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using System.IO;
using ClosedXML.Excel;
using System.Windows.Forms.DataVisualization.Charting;

// tourfirmatest is actual database

namespace tourfirma
{
    public partial class Туризм : Form
    {

        private NpgsqlConnection con; // Подключение к PostgreSQL
        private string connString = "Host=127.0.0.1;Username=postgres;Password=Ilovechehov17105;Database=Turfirm";
        private DataGridViewRow selectedRow; // Выбранная строка в DataGridView


        private Chart chartPie;
        private Chart chartBar;

        private void loadDiagrams()
        {
            // Запрос для круговой диаграммы
            string sqlPie = @"
    SELECT t.tour_name,
           COUNT(p.putevki_id)::float / NULLIF(total.total_count, 0) * 100 AS payment_percentage
    FROM tours t
    LEFT JOIN seasons s ON s.tour_id = t.tour_id
    LEFT JOIN putevki p ON p.season_id = s.season_id
    CROSS JOIN (
        SELECT COUNT(*) AS total_count
        FROM putevki
    ) AS total
    GROUP BY t.tour_name, total.total_count
    HAVING COUNT(p.putevki_id) > 0
    ORDER BY payment_percentage DESC;";

            // Круговая диаграмма
            Chart PieChart = new Chart();
            PieChart.Titles.Add("Процент выкупа туров");
            PieChart.Titles[0].Font = new Font("Arial", 12, FontStyle.Bold);
            PieChart.Location = new Point(10, 75);
            PieChart.Size = new Size(400, 400);
            tabPage7.Controls.Add(PieChart);
            PieChart.ChartAreas.Add(new ChartArea());

            Series PieSeries = new Series("PaymentPercentage");
            PieSeries.ChartType = SeriesChartType.Pie;

            // Заполнение данных
            using (NpgsqlCommand cmdPie = new NpgsqlCommand(sqlPie, this.con))
            {
                using (NpgsqlDataReader reader = cmdPie.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tourName = reader["tour_name"].ToString();
                        double percentage = Convert.ToDouble(reader["payment_percentage"]);
                        PieSeries.Points.AddXY(tourName, percentage);
                    }
                }
            }


            PieSeries.Label = "#PERCENT{P0}";
            PieSeries.LegendText = "#VALX";
            PieSeries.Font = new Font("Arial", 8);
            PieChart.Series.Add(PieSeries);

            // Настройка легенды
            Legend pieLegend = new Legend();
            pieLegend.Docking = Docking.Bottom;
            pieLegend.Alignment = StringAlignment.Center;
            pieLegend.Font = new Font("Arial", 8);
            PieChart.Legends.Add(pieLegend);

            //// 3D-эффект
            //PieChart.ChartAreas[0].Area3DStyle.Enable3D = true;
            //PieChart.ChartAreas[0].Area3DStyle.Inclination = 60;

            // Запрос для столбчатой диаграммы
            string sqlBar = @"
    SELECT 
        t.tour_name, 
        COALESCE(SUM(CAST(pay.summa AS numeric)), 0) AS total_payments
    FROM tours t
    JOIN seasons s ON s.tour_id = t.tour_id
    JOIN putevki p ON p.season_id = s.season_id
    JOIN payment pay ON pay.putevki_id = p.putevki_id
    GROUP BY t.tour_name
    ORDER BY total_payments DESC;";

            // Столбчатая диаграмма
            Chart BarChart = new Chart();
            BarChart.Titles.Add("Сумма выкупа туров");
            BarChart.Titles[0].Font = new Font("Arial", 12, FontStyle.Bold);
            BarChart.Location = new Point(400, 75);
            BarChart.Size = new Size(400, 400);
            tabPage7.Controls.Add(BarChart);

            BarChart.ChartAreas.Add(new ChartArea());

            Series BarSeries = new Series("TotalPayments");
            BarSeries.ChartType = SeriesChartType.Column;
            BarSeries.IsValueShownAsLabel = true;
            BarSeries.LabelFormat = "C2"; // Формат валюты

            // Заполнение данных для столбчатой диаграммы
            using (NpgsqlCommand cmdBar = new NpgsqlCommand(sqlBar, this.con))
            {
                using (NpgsqlDataReader reader = cmdBar.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tourName = reader["tour_name"].ToString();
                        decimal total = Convert.ToDecimal(reader["total_payments"]);
                        BarSeries.Points.AddXY(tourName, total);
                    }
                }
            }

            BarChart.Series.Add(BarSeries);
            BarChart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;

            // Настройка внешнего вида столбчатой диаграммы
            BarChart.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
            BarChart.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Arial", 8);
            BarChart.ChartAreas[0].AxisY.LabelStyle.Format = "C0";
            BarChart.ChartAreas[0].AxisY.Title = "Сумма";
            BarChart.ChartAreas[0].AxisX.Title = "Тур";
        }



        public Туризм()
        {
            InitializeComponent();
            // Инициализация подключения к БД
            con = new NpgsqlConnection(connString);
            con.Open();
            // Загрузка данных во все таблицы
            loadTouristsCombined();
            loadSeasons();
            loadTours();
            loadPutevki();
            loadPayment();
            // Построение графиков
            // Новое
            loadDiagrams();
            InitPredefinedQueries();


        }
        private void InitPredefinedQueries()
        {
            comboAggregate.Items.AddRange(new object[]
            {
        "Количество туристов",
        "Общая сумма платежей",
        "Средняя стоимость туров"
            });

            comboParametric.Items.AddRange(new object[]
            {
        "Туры по стране",
        "Платежи по туристу",
        "Путевки по дате"
            });
        }

        // Обработчик изменения выбранной строки в таблице туристов
        private void dataGridViewTourists_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView3.SelectedRows.Count > 0)
            {
                selectedRow = dataGridView3.SelectedRows[0];

                if (selectedRow.Cells["tourist_id"].Value != null)
                {
                    Console.WriteLine($"Выбрана строка с ID: {selectedRow.Cells["tourist_id"].Value}");
                }
            }
        }

        // Обработчик кнопки удаления записи
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string activeTab = tabControl1.SelectedTab?.Name;
                if (activeTab == null)
                {
                    MessageBox.Show("Не удалось определить активную вкладку!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataGridView currentGrid = GetCurrentDataGridView(activeTab);
                if (currentGrid == null || currentGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите строку для удаления!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedRow = currentGrid.SelectedRows[0];
                if (selectedRow == null || selectedRow.IsNewRow)
                {
                    MessageBox.Show("Выберите корректную строку для удаления!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Запрос подтверждения удаления
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение удаления",
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                string tableName = "";
                string idColumnName = "";
                int idValue = 0;

                // Определение таблицы и ID для удаления в зависимости от активной вкладки
                switch (activeTab)
                {
                    case "tabPage2": // Туристы
                        tableName = "tourists";
                        idColumnName = "tourist_id";
                        if (selectedRow.Cells["tourist_id"].Value != null)
                            idValue = Convert.ToInt32(selectedRow.Cells["tourist_id"].Value);
                        break;

                    case "tabPage3": // Сезоны
                        tableName = "seasons";
                        idColumnName = "season_id";
                        if (selectedRow.Cells["season_id"].Value != null)
                            idValue = Convert.ToInt32(selectedRow.Cells["season_id"].Value);
                        break;

                    case "tabPage4": // Путевки
                        tableName = "putevki";
                        idColumnName = "putevki_id";
                        if (selectedRow.Cells["putevki_id"].Value != null)
                            idValue = Convert.ToInt32(selectedRow.Cells["putevki_id"].Value);
                        break;

                    case "tabPage5": // Оплата
                        tableName = "payment";
                        idColumnName = "payment_id";
                        if (selectedRow.Cells["payment_id"].Value != null)
                            idValue = Convert.ToInt32(selectedRow.Cells["payment_id"].Value);
                        break;

                    default:
                        MessageBox.Show("Неизвестная вкладка!", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                }

                if (idValue == 0)
                {
                    MessageBox.Show("Не удалось определить ID для удаления!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // SQL-запрос для удаления
                string sql = $@"DELETE FROM {tableName} WHERE {idColumnName} = @id;";

                // Выполнение в транзакцииф
                using (var transaction = con.BeginTransaction())
                using (var cmd = new NpgsqlCommand(sql, con, transaction))
                {
                    cmd.Parameters.AddWithValue("id", idValue);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        transaction.Commit();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Запись успешно удалена!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RefreshCurrentTab();
                        }
                        else
                        {
                            MessageBox.Show("Запись не была удалена!", "Ошибка",
                                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503")
                    {
                        // Обработка ошибки внешнего ключа
                        transaction.Rollback();
                        string errorMessage = GetForeignKeyErrorMessage(tableName);
                        MessageBox.Show(errorMessage, "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Возвращает сообщение об ошибке для нарушения внешнего ключа
        private string GetForeignKeyErrorMessage(string tableName)
        {
            switch (tableName)
            {
                case "tourists":
                    return "Нельзя удалить туриста, для которого существуют путевки!\nСначала удалите связанные путевки.";
                case "seasons":
                    return "Нельзя удалить сезон, для которого существуют путевки!\nСначала удалите связанные путевки.";
                case "putevki":
                    return "Нельзя удалить путевку, для которой существуют платежи!\nСначала удалите связанные платежи.";
                default:
                    return "Нельзя удалить запись, так как на нее есть ссылки в других таблицах!";
            }
        }

        // Загрузка данных о туристах с локализованными заголовками
        private void loadTouristsCombined()
        {
            try
            {
                DataTable dt = new DataTable();
                string sql = @"SELECT 
                    tourist_id, 
                    tourist_surname, 
                    tourist_name, 
                    tourist_otch,
                    passport,
                    city,
                    country,
                    phone
                    FROM tourists";

                new NpgsqlDataAdapter(sql, con).Fill(dt);

                // Установка отображаемых имен столбцов
                dt.Columns["tourist_surname"].Caption = "Фамилия";
                dt.Columns["tourist_name"].Caption = "Имя";
                dt.Columns["tourist_otch"].Caption = "Отчество";
                dt.Columns["passport"].Caption = "Паспорт";
                dt.Columns["city"].Caption = "Город";
                dt.Columns["country"].Caption = "Страна";
                dt.Columns["phone"].Caption = "Телефон";

                dataGridView3.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка данных о сезонах
        private void loadSeasons()
        {
            DataTable dt = new DataTable();
            NpgsqlDataAdapter adap = new NpgsqlDataAdapter("SELECT * FROM seasons", con);
            adap.Fill(dt);
            dataGridView4.DataSource = dt;
        }

        private void loadTours()
        {
            DataTable dt = new DataTable();
            NpgsqlDataAdapter adap = new NpgsqlDataAdapter("SELECT * FROM tours", con);
            adap.Fill(dt);
            dataGridView7.DataSource = dt;
        }

        // Загрузка данных о путевках
        private void loadPutevki()
        {
            DataTable dt = new DataTable();
            NpgsqlDataAdapter adap = new NpgsqlDataAdapter("SELECT * FROM putevki", con);
            adap.Fill(dt);
            dataGridView5.DataSource = dt;
        }

        // Загрузка данных о платежах
        private void loadPayment()
        {
            DataTable dt = new DataTable();
            NpgsqlDataAdapter adap = new NpgsqlDataAdapter("SELECT * FROM payment", con);
            adap.Fill(dt);
            dataGridView6.DataSource = dt;
        }

        // Обработчик кнопки добавления записи
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string activeTab = tabControl1.SelectedTab?.Name;
                if (activeTab == null) return;

                // Открытие формы добавления
                var form = new UniversalEditForm(activeTab, null, con);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    RefreshCurrentTab();
                    loadTours();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии формы добавления: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик кнопки редактирования записи
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string activeTab = tabControl1.SelectedTab?.Name;
                if (activeTab == null) return;

                DataGridView currentGrid = GetCurrentDataGridView(activeTab);

                if (currentGrid == null || currentGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите строку для редактирования!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedRow = currentGrid.SelectedRows[0];
                if (selectedRow == null || selectedRow.IsNewRow)
                {
                    MessageBox.Show("Выберите корректную строку для редактирования!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Открытие формы редактирования
                var form = new UniversalEditForm(activeTab, selectedRow, con);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    RefreshCurrentTab();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии формы редактирования: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Возвращает DataGridView активной вкладки
        private DataGridView GetCurrentDataGridView(string activeTab)
        {
            switch (activeTab)
            {
                case "tabPage2": return dataGridView3; // Туристы
                case "tabPage3": return dataGridView4; // Сезоны
                case "tabPage4": return dataGridView5; // Путевки
                case "tabPage5": return dataGridView6; // Оплата
                case "tabPage7": return dataGridView7; // Туры
                default: return null;
            }
        }

        // Обновление данных активной вкладки
        private void RefreshCurrentTab()
        {
            string activeTab = tabControl1.SelectedTab?.Name;
            if (activeTab == null) return;

            switch (activeTab)
            {
                case "tabPage2": loadTouristsCombined(); break;
                case "tabPage3": loadSeasons(); break;
                case "tabPage4": loadPutevki(); break;
                case "tabPage5": loadPayment(); break;
                case "tabPage7": loadTours(); break;

            }
        }

        // Обработчик выполнения агрегированного запроса
        private void button4_Click(object sender, EventArgs e)
        {
            string query = txtAggregateQuery.Text.Trim();

            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Введите агрегированный запрос!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
                {
                    // Выполнение скалярного запроса (COUNT, SUM и т.д.)
                    object result = cmd.ExecuteScalar();
                    MessageBox.Show($"Результат: {result}", "Агрегированный запрос", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик выполнения параметрического запроса
        private void button5_Click(object sender, EventArgs e)
        {
            string query = txtParametricQuery.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Введите параметрический запрос!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
                {
                    // Здесь можно добавить параметры, если они есть в запросе
                    // cmd.Parameters.AddWithValue("@param", value);

                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Вывод результата в DataGridView
                        dataGridViewResult.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Неиспользуемый обработчик (заглушка)
        private void button7_Click_1(object sender, EventArgs e)
        {
            // Метод оставлен для будущей реализации
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, есть ли данные в dataGridView
                if (dataGridViewResult.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Создаем диалог выбора файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    Title = "Сохранить отчет",
                    FileName = "Отчет.xlsx"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    // Создаем новую книгу Excel
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Отчет");

                        // Заголовки столбцов
                        for (int i = 0; i < dataGridViewResult.Columns.Count; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = dataGridViewResult.Columns[i].HeaderText;
                            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        }

                        // Данные из DataGridView
                        for (int i = 0; i < dataGridViewResult.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataGridViewResult.Columns.Count; j++)
                            {
                                worksheet.Cell(i + 2, j + 1).Value = dataGridViewResult.Rows[i].Cells[j].Value?.ToString() ?? "";
                            }
                        }

                        // Автоширина колонок
                        worksheet.Columns().AdjustToContents();

                        // Сохранение файла
                        workbook.SaveAs(filePath);
                    }

                    MessageBox.Show($"Файл успешно сохранен: {filePath}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                // Диалог выбора файла
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    Title = "Выберите файл для импорта"
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    // Открываем файл Excel
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        var worksheet = workbook.Worksheet(1); // Берем первый лист
                        var range = worksheet.RangeUsed(); // Получаем используемый диапазон

                        if (range == null)
                        {
                            MessageBox.Show("Файл пуст или не содержит данных!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        DataTable dt = new DataTable();

                        // Читаем заголовки (первую строку)
                        foreach (var cell in range.FirstRow().CellsUsed())
                        {
                            dt.Columns.Add(cell.Value.ToString().Trim());
                        }

                        // Читаем строки (со второй)
                        foreach (var row in range.RowsUsed().Skip(1))
                        {
                            DataRow dataRow = dt.NewRow();
                            int columnIndex = 0;

                            foreach (var cell in row.CellsUsed())
                            {
                                dataRow[columnIndex] = cell.Value.ToString().Trim();
                                columnIndex++;
                            }

                            dt.Rows.Add(dataRow);
                        }

                        // Загружаем данные в DataGridView
                        dataGridViewResult.DataSource = dt;
                    }

                    MessageBox.Show("Данные успешно импортированы!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Кнопка триггера
        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                // Вставка нового туриста вручную (без формы)
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO tourists (tourist_surname, tourist_name, tourist_otch, passport, city, country, phone)
                    VALUES ('Годунов', 'Борис', 'Федорович', '1234123123', 'Московия', 'Россия', '+79991231212')
                    RETURNING tourist_id;", con
                );

                int newTouristId = Convert.ToInt32(cmd.ExecuteScalar());

                MessageBox.Show($"Добавлен новый турист с ID: {newTouristId}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Переключиться на вкладку «Путевки»
                tabControl1.SelectedTab = tabPage4;

                // Обновить dataGridView с туристами и путевками
                loadTouristsCombined(); // Добавьте эту строку
                loadPutevki();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании туриста и срабатывании триггера: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboAggregate_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboAggregate.SelectedItem.ToString())
            {
                case "Количество туристов":
                    txtAggregateQuery.Text = "SELECT COUNT(*) FROM tourists;";
                    break;
                case "Общая сумма платежей":
                    txtAggregateQuery.Text = "SELECT SUM(summa) FROM payment;";
                    break;
                case "Средняя стоимость туров":
                    txtAggregateQuery.Text = "SELECT AVG(price::numeric) FROM tours;";
                    break;
            }
        }

        private void comboParametric_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboParametric.SelectedItem.ToString())
            {
                case "Туры по стране":
                    txtParametricQuery.Text = "SELECT * FROM tourists WHERE country = 'Россия';";
                    break;
                case "Платежи по туристу":
                    txtParametricQuery.Text = "SELECT * FROM payment WHERE putevki_id IN (SELECT putevki_id FROM putevki WHERE tourist_id = 1);";
                    break;
                case "Путевки по дате":
                    txtParametricQuery.Text = "SELECT * FROM putevki WHERE CURRENT_DATE BETWEEN '2024-01-01' AND '2024-12-31';";
                    break;
            }
        }


        private void tabPage7_Click(object sender, EventArgs e)
        {

        }

        private void Туризм_Load(object sender, EventArgs e)
        {

        }
    }
}