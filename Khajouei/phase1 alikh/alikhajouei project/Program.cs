using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

public class DatabaseManager
{
    private string connectionString = "Data Source=MyDatabase.sqlite;Version=3;";

    public void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            CreateBlockTable(connection);
        }
    }

    private void CreateBlockTable(SqliteConnection connection)
    {
        string sql = @"
            CREATE TABLE IF NOT EXISTS Blocks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Type TEXT NOT NULL,
                Description TEXT
            )";
        using (var command = new SqliteCommand(sql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void InsertRecord(string tableName, Dictionary<string, object> values)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            string columns = string.Join(", ", values.Keys);
            string paramNames = string.Join(", ", values.Keys.Select(k => "@" + k));
            string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({paramNames})";
            using (var command = new SqliteCommand(sql, connection))
            {
                foreach (var kvp in values)
                {
                    command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                }
                command.ExecuteNonQuery();
            }
        }
    }
}
class DormitoryBlockSupervisor
{
    private string Role;
    private string BlckUnRespons;
    public DormitoryBlockSupervisor(string Role, string BlckUnRespons)
    {
        this.Role = Role;   
        this.BlckUnRespons = BlckUnRespons;
    }
}
class Student
{
    private string StudentID;
    private string Room;
    private string Block;
    private List<string> PersonalItems;
    public Student(string studentID, string room, string block, List<string> personalItems)
    {
        StudentID = studentID;
        Room = room;
        Block = block;
        PersonalItems = personalItems;
    }
}