using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

using System.Data.SQLite;
using static System.Windows.Forms.LinkLabel;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Timer = System.Threading.Timer;
using XMLsorter.Properties;



namespace XMLsorter
{
    public partial class Form1 : Form
    {
        string folderPath = ""; // Путь к папке с XML файлами 
        string destinationPath = ""; // Путь для перемещения файла 
        string folderForDB = "";
        static Timer timer;
        bool timerStatus = false;
        long interval = 60 * 1000;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var musorka = Properties.Settings.Default.Musorka;
            var sorted = Properties.Settings.Default.Sorted;
            
            if (musorka != null && sorted != null)
            {
                folderPath = (string)musorka;
                destinationPath = (string)sorted;
                textBox1.Text = folderPath;
                textBox2.Text = destinationPath;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //string folderPath = "C:\\Users\\admin\\Downloads\\Object\\"; // Путь к папке с XML файлами 
           


            if (textBox1.Text == "" || textBox2.Text == "") return;
                folderPath = textBox1.Text;  // Путь к папке с XML файлами 
                destinationPath = textBox2.Text; // Путь для перемещения файла 

            Properties.Settings.Default.Musorka = folderPath;
            Properties.Settings.Default.Sorted = destinationPath;
            Properties.Settings.Default.Save();

            textBox1.Enabled = timerStatus; textBox2.Enabled = timerStatus;
            timerStatus = !timerStatus;
            if (timerStatus)
            {
                button3.Text = "Остановить";
                long interv = interval;
                try
                {
                    interv = interval * Convert.ToInt64(textBox3.Text);
                }
                catch
                {
                    interv = interval * 60;
                }
                timer = new Timer(LookFiles, null, 0, interv);
            }
            else
            {
                timer.Dispose();
                button3.Text = "Запуск";
            }

        }

        void LookFiles(object state)
        {


            string databasePath = destinationPath + "/cadastral.db"; // Путь к базе данных SQLite 

            // Создаем базу данных, если она не существует 
            CreateDatabase(databasePath);

            // Получаем список XML файлов в папке 
            string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");
            string cadastralNumber = "";
            string dateFormation = "";
            // Перебираем каждый XML файл 
            foreach (string xmlFile in xmlFiles)
            {
                // Загружаем содержимое XML файла 
                StreamReader f = new StreamReader(xmlFile);
                while (!f.EndOfStream)
                {
                    string s = f.ReadLine();
                    // Поиск значений в формате "CadastralNumber="MM:NN:KKKKKK""
                    Match match1 = Regex.Match(s, @"DateCreated=""(\d+-\d+-\d+)""");
                    if (match1.Success)
                    {
                        Console.WriteLine("Найдено значение: " + match1.Groups[1].Value);
                        dateFormation = match1.Groups[1].Value;
                        break;
                    }

                    // Поиск значений в формате "<cadastral_number>MM:NN:KKKKKKK</cadastral_number>"
                    Match match2 = Regex.Match(s, @"<date_received_request>(\d+-\d+-\d+)</date_received_request>");
                    if (match2.Success)
                    {
                        Console.WriteLine("Найдено значение: " + match2.Groups[1].Value);
                        dateFormation = match2.Groups[1].Value;
                        break;
                    }
                }
                f.Close();

                f = new StreamReader(xmlFile);
                while (!f.EndOfStream)
                {
                    string s = f.ReadLine();
                    // Поиск значений в формате "CadastralNumber="MM:NN:KKKKKK""
                    Match match1 = Regex.Match(s, @"CadastralNumber=""(\d+:\d+:\d+)""");
                    if (match1.Success)
                    {
                        Console.WriteLine("Найдено значение: " + match1.Groups[1].Value);
                        cadastralNumber = match1.Groups[1].Value;
                        break;
                    }

                    // Поиск значений в формате "<cadastral_number>MM:NN:KKKKKKK</cadastral_number>"
                    Match match2 = Regex.Match(s, @"<cadastral_number>(\d+:\d+:\d+)</cadastral_number>");
                    if (match2.Success)
                    {
                        Console.WriteLine("Найдено значение: " + match2.Groups[1].Value);
                        cadastralNumber = match2.Groups[1].Value;
                        break;
                    }
                }
                f.Close();


                // Разделяем кадастровый номер на части 
                string[] parts = cadastralNumber.Split(':');

                string destinationPathTemp = Path.Combine(parts[0], parts[1]);
                Directory.CreateDirectory(destinationPath + "\\" + destinationPathTemp);
                destinationPathTemp = destinationPath + "\\" + Path.Combine(destinationPathTemp, Path.GetFileName(xmlFile));

                if (CheckZapis(databasePath, parts))
                {
                    string oldDate = DateFromDB(databasePath, parts);
                    DateTime dateTimeOld;
                    DateTime dateTimeNew;
                    DateTime.TryParse(dateFormation, out dateTimeNew);
                    DateTime.TryParse(oldDate, out dateTimeOld);

                    if (dateTimeNew >= dateTimeOld)
                    {
                        File.Delete(destinationPathTemp);
                        UpdateZapis(databasePath, parts, dateFormation);
                    }
                    else
                    {
                        File.Delete(destinationPathTemp);
                        return;
                    }
                }
                else
                {
                    // Добавляем данные в базу данных 
                    InsertData(databasePath, parts[0], parts[1], parts[2], dateFormation);

                    // Разделяем кадастровый номер на части 


                }

                File.Move(xmlFile, destinationPathTemp);
            }
        }
        void UpdateZapis(string databasePath, string[] cadastralNum, string newDat)
        {           
            string connectionString = "Data Source=" + databasePath;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE cadastral SET dataCreat = '"+newDat+"' WHERE region = '" + cadastralNum[0] +
                    "' AND region1 = '" + cadastralNum[1] +"' AND region2 = '" + cadastralNum[2] +"'";
               
                SQLiteCommand command = new SQLiteCommand(query, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        
        }
        bool CheckZapis(string databasePath, string[] cadastralNum)
        {
            int count = -1;
            string connectionString = "Data Source=" + databasePath;
         
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM cadastral WHERE region = @Region AND region1 = @Region1 AND region2 = @Region2";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Region", cadastralNum[0]);
                    command.Parameters.AddWithValue("@Region1", cadastralNum[1]);
                    command.Parameters.AddWithValue("@Region2", cadastralNum[2]);
                    count = Convert.ToInt32(command.ExecuteScalar());

                }

                connection.Close();
            }
            return _ = count>0?true:false;
        }

        string DateFromDB(string databasePath, string[] cadastralNum)
        {
            string dataCreat="";
            string connectionString = "Data Source=" + databasePath;
        
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT dataCreat FROM cadastral WHERE region = @Region AND region1 = @Region1 AND region2 = @Region2";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Region", cadastralNum[0]);
                    command.Parameters.AddWithValue("@Region1", cadastralNum[1]);
                    command.Parameters.AddWithValue("@Region2", cadastralNum[2]);
                    dataCreat = (string)command.ExecuteScalar();
                                       
                }

                connection.Close();
            }
            return dataCreat;
        }

            static string GetNodeValue(XmlDocument doc, string xpath)
            {
                XmlNode node = doc.SelectSingleNode(xpath);
                return node?.InnerText;
            }


        static void CreateDatabase(string databasePath)
        {
            if (!File.Exists(databasePath))
            {
                SQLiteConnection.CreateFile(databasePath);
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
                {
                    connection.Open();
                    string sql = "CREATE TABLE cadastral (id INTEGER PRIMARY KEY AUTOINCREMENT, region TEXT, region1 TEXT, region2 TEXT, dataCreat TEXT)";
                    SQLiteCommand command = new SQLiteCommand(sql, connection);
                    command.ExecuteNonQuery();
                }
            }
        }
        static void InsertData(string databasePath, string region, string region1, string region2,  string dateCreat)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                string sql = "INSERT INTO cadastral (region, region1, region2, dataCreat) VALUES (@region, @region1, @region2, @dataCreat)";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@region", region);
                command.Parameters.AddWithValue("@region1", region1);
                command.Parameters.AddWithValue("@region2", region2);
                command.Parameters.AddWithValue("@dataCreat", dateCreat);
                command.ExecuteNonQuery();
            }
        }

      
    }
    

}
