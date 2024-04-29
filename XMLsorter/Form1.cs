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


namespace XMLsorter
{
    public partial class Form1 : Form
    {
        string folderPath = ""; // Путь к папке с XML файлами 
        string destinationPath = ""; // Путь для перемещения файла 
        string folderForDB = "";
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
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
                string destinationPath = textBox2.Text; // Путь для перемещения файла 
                string databasePath = textBox2.Text+ "/cadastral.db"; // Путь к базе данных SQLite 

                // Создаем базу данных, если она не существует 
                CreateDatabase(databasePath);

                // Получаем список XML файлов в папке 
                string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");

                // Перебираем каждый XML файл 
                foreach (string xmlFile in xmlFiles)
                {
                    // Загружаем содержимое XML файла 
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlFile);

                    // Ищем теги <cadastral_number> и <date_formation> и извлекаем их значения 
                    string cadastralNumber = GetNodeValue(doc, "//cadastral_number");
                    string dateFormation = GetNodeValue(doc, "//date_formation");

                    // Выводим значения 
                    Console.WriteLine("Файл: " + xmlFile);
                    Console.WriteLine("Кадастровый номер: " + cadastralNumber);
                    Console.WriteLine("Дата формирования: " + dateFormation);

                    // Разделяем кадастровый номер на части 
                    string[] parts = cadastralNumber.Split(':');
                    string region = parts[0];

                    // Добавляем данные в базу данных 
                    InsertData(databasePath, region, dateFormation);

                    // Разделяем кадастровый номер на части 
                    destinationPath = Path.Combine(parts[0], parts[1]);
                    Directory.CreateDirectory(textBox2.Text+"\\"+destinationPath);
                    destinationPath = textBox2.Text + "\\"+ Path.Combine(destinationPath, Path.GetFileName(xmlFile));

                    // Перемещаем файл 
                    File.Move(xmlFile, destinationPath);

                    Console.WriteLine("Файл перемещен в: " + destinationPath);
                    Console.WriteLine();
                }
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
                    string sql = "CREATE TABLE cadastral (id INTEGER PRIMARY KEY AUTOINCREMENT, region TEXT, dataCreat TEXT)";
                    SQLiteCommand command = new SQLiteCommand(sql, connection);
                    command.ExecuteNonQuery();
                }
            }
        }
        static void InsertData(string databasePath, string region, string dateCreat)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                string sql = "INSERT INTO cadastral (region, dataCreat) VALUES (@region, @dataCreat)";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@region", region);
                command.Parameters.AddWithValue("@dataCreat", dateCreat);
                command.ExecuteNonQuery();
            }
        }

      
    }
    

}
