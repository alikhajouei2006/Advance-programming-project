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
using System.Net;
using System.Text.Encodings;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Spectre.Console;
using System.Data;
using System.Xml.Linq;
using System.Drawing;

namespace Dormitory
{
    public class DatabaseManager
    {
        private string _connectionString;

        public DatabaseManager()
        { _connectionString = "Data Source=MyDatabase.sqlite;"; }
        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                CreateTables(connection);
            }
        }

        public bool DoesUserNameExist(string userName)
        {
            return GetRecordsByField("Director", "UserName", userName).Count() > 0;
        }
        public bool DoesSocialNumberExist(string tableName, string socialNumber)
        {
            return GetRecordsByField(tableName, "SocialNumber", socialNumber).Count() > 0;
        }
        public void InsertRecord(string tableName, Dictionary<string, object> values)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string columns = string.Join(", ", values.Keys);
                string paramNames = string.Join(", ", values.Keys.Select(k => "@" + k));
                string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({paramNames});";

                using (var command = new SqliteCommand(sql, connection))
                {
                    foreach (var kvp in values)
                    {
                        if (kvp.Value == null || kvp.Value == DBNull.Value)
                        {
                            command.Parameters.Add(new SqliteParameter("@" + kvp.Key, System.Data.DbType.Object) { Value = DBNull.Value });
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                        }
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Dictionary<string, object>> GetRecordsByField(string tableName, string fieldName, object value)
        {
            List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = $"SELECT * FROM {tableName} WHERE {fieldName} = @value";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                record[reader.GetName(i)] = reader.GetValue(i);
                            }

                            records.Add(record);
                        }
                    }
                }
            }

            return records;
        }
        public void DeleteRecord(string tableName, string keyColumn, object keyValue)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string sql = $"DELETE FROM {tableName} WHERE {keyColumn} = @value;";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@value", keyValue);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateRecord(string tableName, Dictionary<string, object> updatedValues, string conditionColumn, object conditionValue)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var setClause = string.Join(", ", updatedValues.Keys.Select(k => $"{k} = @{k}"));
                string sql = $"UPDATE {tableName} SET {setClause} WHERE {conditionColumn} = @ConditionValue";

                using (var command = new SqliteCommand(sql, connection))
                {
                    foreach (var kvp in updatedValues)
                        command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);

                    command.Parameters.AddWithValue("@ConditionValue", conditionValue);
                    command.ExecuteNonQuery();
                }
            }
        }
        public List<Dictionary<string, object>> GetAllRecords(string tableName)
        {
            var result = new List<Dictionary<string, object>>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT * FROM {tableName}";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }
            }

            return result;
        }
        private void CreateTables(SqliteConnection connection)
        {
            string dormitorySql = @"
            CREATE TABLE IF NOT EXISTS Dormitories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Address TEXT NOT NULL,
                TotalCapacity INTEGER NOT NULL,
                Responsible TEXT,
                RemainingCapacity INTEGER NOT NULL
            );";

            string blockSql = @"
            CREATE TABLE IF NOT EXISTS Blocks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                DormitoryId INTEGER,
                Responsible TEXT,
                NumberOfFloors INTEGER,
                NumberOfRooms INTEGER,
                TotalCapacity INTEGER NOT NULL,
                RemainingCapacity INTEGER NOT NULL,
                FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id) ON DELETE SET NULL
            );";

            string roomSql = @"
            CREATE TABLE IF NOT EXISTS Rooms (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BlockId INTEGER NOT NULL,
                FloorNumber INTEGER NOT NULL,
                TotalCapacity INTEGER NOT NULL CHECK(TotalCapacity >= 0 AND TotalCapacity <= 6),
                RemainingCapacity INTEGER NOT NULL CHECK(RemainingCapacity >= 0 AND RemainingCapacity <= 6),
                FOREIGN KEY (BlockId) REFERENCES Blocks(Id) ON DELETE CASCADE
            );";

            string dormitoryBlockSupervisorSql = @"
            CREATE TABLE IF NOT EXISTS DormitoryBlockSupervisors (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                SocialNumber TEXT UNIQUE NOT NULL,
                PhoneNumber TEXT NOT NULL,
                Address TEXT NOT NULL,
                StudentId INTEGER,
                BlockId INTEGER,
                Role TEXT NOT NULL,
                FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE SET NULL,
                FOREIGN KEY (BlockId) REFERENCES Blocks(Id) ON DELETE SET NULL
            );";

            string dormitorySupervisorSql = @"
            CREATE TABLE IF NOT EXISTS DormitorySupervisors (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                SocialNumber TEXT UNIQUE NOT NULL,
                PhoneNumber TEXT NOT NULL,
                Address TEXT NOT NULL,
                Position TEXT NOT NULL,
                DormitoryId INTEGER,
                FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id) ON DELETE SET NULL
            );";

            string personSql = @"
            CREATE TABLE IF NOT EXISTS Persons (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                SocialNumber TEXT,
                PhoneNumber TEXT,
                Address TEXT
            );";

            string studentSql = @"
            CREATE TABLE IF NOT EXISTS Students (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                SocialNumber TEXT UNIQUE NOT NULL,
                PhoneNumber TEXT UNIQUE NOT NULL,
                Address TEXT NOT NULL,
                StudentID TEXT,
                RoomId INTEGER,
                BlockId INTEGER,
                DormitoryId INTEGER,
                FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE SET NULL,
                FOREIGN KEY (BlockId) REFERENCES Blocks(Id) ON DELETE SET NULL,
                FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id) ON DELETE SET NULL
            );";

            string directorSql = @"
            CREATE TABLE IF NOT EXISTS Director (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                SocialNumber TEXT UNIQUE NOT NULL,
                PhoneNumber TEXT,
                UserName TEXT UNIQUE NOT NULL,
                Password TEXT UNIQUE NOT NULL
            );";


            string itemSql = @"
            CREATE TABLE IF NOT EXISTS PersonalItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId INTEGER NOT NULL,
                Item TEXT,
                FOREIGN KEY (StudentId) REFERENCES Students(Id)
            );";

            string equipmentSql = @"
            CREATE TABLE IF NOT EXISTS Equipment (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Type TEXT,
                PartNumber TEXT UNIQUE NOT NULL,
                PropertyNumber TEXT,
                Condition TEXT,
                RoomId INTEGER,
                OwnerId INTEGER,
                FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE SET NULL,
                FOREIGN KEY (OwnerId) REFERENCES Students(Id) ON DELETE SET NULL
            );";

            string repairrequestSql = @"
		    CREATE TABLE IF NOT EXISTS RepairRequests (
		        Id INTEGER PRIMARY KEY AUTOINCREMENT,
		        PropertyNumber TEXT,
		        Status TEXT
		    );";

            string studentaccommodationSql = @"
            CREATE TABLE IF NOT EXISTS StudentAccommodationHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId INTEGER NOT NULL,
                DormitoryId INTEGER NOT NULL,
                BlockId INTEGER NOT NULL,
                RoomId INTEGER NOT NULL,
                StartDate TEXT NOT NULL,
                EndDate TEXT,
                FOREIGN KEY(StudentId) REFERENCES Students(Id),
                FOREIGN KEY(DormitoryId) REFERENCES Dormitories(Id) ON DELETE SET NULL,
                FOREIGN KEY(BlockId) REFERENCES Blocks(Id) ON DELETE SET NULL,
                FOREIGN KEY(RoomId) REFERENCES Rooms(Id) ON DELETE SET NULL
            );";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;"; cmd.ExecuteNonQuery();
                cmd.CommandText = dormitorySql; cmd.ExecuteNonQuery();
                cmd.CommandText = blockSql; cmd.ExecuteNonQuery();
                cmd.CommandText = roomSql; cmd.ExecuteNonQuery();
                cmd.CommandText = personSql; cmd.ExecuteNonQuery();
                cmd.CommandText = studentSql; cmd.ExecuteNonQuery();
                cmd.CommandText = itemSql; cmd.ExecuteNonQuery();
                cmd.CommandText = equipmentSql; cmd.ExecuteNonQuery();
                cmd.CommandText = dormitoryBlockSupervisorSql; cmd.ExecuteNonQuery();
                cmd.CommandText = dormitorySupervisorSql; cmd.ExecuteNonQuery();
                cmd.CommandText = directorSql; cmd.ExecuteNonQuery();
                cmd.CommandText = repairrequestSql; cmd.ExecuteNonQuery();
                cmd.CommandText = studentaccommodationSql; cmd.ExecuteNonQuery();
            }

        }
        public void ShowRecordsByField(string tableName, string fieldName, object value)
        {
            var records = GetRecordsByField(tableName, fieldName, value);

            if (records.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]No records found in table [bold]{tableName}[/] for [yellow]{fieldName} = {value}[/].[/]");
                Thread.Sleep(3000);
                ENUserInterFace.mainMenu();
            }

            var table = new Table();

            foreach (var column in records[0].Keys)
            {
                table.AddColumn($"[blue]{column}[/]");
            }

            foreach (var record in records)
            {
                var rowData = record.Values.Select(v => v?.ToString() ?? "").ToArray();
                table.AddRow(rowData);
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("[yellow]press Enter to continue[/]");
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = ReadKey(intercept: true);
            } while (keyInfo.Key != ConsoleKey.Enter);
            
        }
        public void ShowAllrecords(string tableName, bool check = true)
        {
            try
            {
                var records = GetAllRecords(tableName);

                if (records.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]No records found in table [bold]{tableName}[/][/]");
                    return;
                }

                var table = new Table();

                foreach (var column in records[0].Keys)
                {
                    table.AddColumn($"[blue]{column}[/]");
                }


                foreach (var record in records)
                {
                    var rowData = record.Values.Select(v => v?.ToString() ?? "").ToArray();
                    table.AddRow(rowData);
                }

                AnsiConsole.Write(table);


                while (ReadKey(intercept: true).Key != ConsoleKey.Enter) { }
                if (check)
                {
                    AnsiConsole.MarkupLine("[yellow]Press Enter to return to the main menu...[/]");
                    ENUserInterFace.mainMenu();
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Press Enter to Contiue...[/]");
                }
            }
            catch (Exception)
            {
                ENUserInterFace.mainMenu();
            }
        }
        public void ShowRelatedRecords(string tableName, string foreignKeyColumn, object foreignKeyValue)
        {
            var records = GetRecordsByField(tableName, foreignKeyColumn, foreignKeyValue);

            if (records.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]No records found in table '{tableName}' where {foreignKeyColumn} = {foreignKeyValue}.[/]");
                return;
            }

            var table = new Table().Border(TableBorder.Rounded);
            table.Title($"[blue]{tableName} where {foreignKeyColumn} = {foreignKeyValue}[/]");

            foreach (var column in records[0].Keys)
            {
                table.AddColumn(column);
            }

            foreach (var record in records)
            {
                List<string> row = new List<string>();
                foreach (var value in record.Values)
                {
                    row.Add(value?.ToString() ?? "[grey]NULL[/]");
                }
                table.AddRow(row.ToArray());
            }

            AnsiConsole.Write(table);
        }
        public Dictionary<string, object> ShowAccommodationStepByStepWithTable(string studentSocialNumber)
        {
            var studentRecords = Program.db.GetRecordsByField("Students", "SocialNumber", studentSocialNumber);


            var student = studentRecords[0];
            int? currentDormId = student["DormitoryId"] != DBNull.Value ? Convert.ToInt32(student["DormitoryId"]) : null;
            int? currentBlockId = student["BlockId"] != DBNull.Value ? Convert.ToInt32(student["BlockId"]) : null;
            int? currentRoomId = student["RoomId"] != DBNull.Value ? Convert.ToInt32(student["RoomId"]) : null;

            var dormitories = Program.db.GetAllRecords("Dormitories");
            if (dormitories.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No dormitories found![/]");
                return null;
            }

            var dormTable = new Table();
            dormTable.AddColumn("ID");
            dormTable.AddColumn("Name");

            foreach (var d in dormitories)
            {
                int id = Convert.ToInt32(d["Id"]);
                string marker = (currentDormId.HasValue && currentDormId.Value == id) ? "*" : "";
                dormTable.AddRow(id.ToString(), $"{marker}{d["Name"]}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold blue]Available Dormitories[/]");
            AnsiConsole.Write(dormTable);
        getdor:
            int dormId = AnsiConsole.Ask<int>("Enter Dormitory ID:");
            var block = Program.db.GetRecordsByField("Blocks", "DormitoryId", dormId);
            if (block == null || block.Count == 0 )
            {
                AnsiConsole.MarkupLine("[red]The chosen dormitory dosen't contain any blocks . please retry .[/]");
                Thread.Sleep(3000);
                goto getdor;
            }
            if (!dormitories.Any(d => Convert.ToInt32(d["Id"]) == dormId))
            {
                AnsiConsole.MarkupLine("[red]Invalid Dormitory ID![/]");
                return null;
            }


            var blocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", dormId);
            if (blocks.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No blocks found for this dormitory![/]");
                return null;
            }

            var blockTable = new Table();
            blockTable.AddColumn("ID");
            blockTable.AddColumn("Name");

            foreach (var b in blocks)
            {
                int id = Convert.ToInt32(b["Id"]);
                string marker = (currentBlockId.HasValue && currentBlockId.Value == id) ? "* " : "";
                blockTable.AddRow(id.ToString(), $"{marker}{b["Name"]}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]Available Blocks[/]");
            AnsiConsole.Write(blockTable);
        getbl:
            int blockId = AnsiConsole.Ask<int>("Enter Block ID:");
            var room = Program.db.GetRecordsByField("Rooms", "BlockId", blockId);

            if (room == null || room.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]This block has no rooms.[/]");
                Thread.Sleep(3000);
                goto getbl;
            }
            if (!blocks.Any(b => Convert.ToInt32(b["Id"]) == blockId))
            {
                AnsiConsole.MarkupLine("[red]Invalid Block ID![/]");
                return null;
            }


            var rooms = Program.db.GetRecordsByField("Rooms", "BlockId", blockId);
            if (rooms.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No rooms found for this block![/]");
                return null;
            }

            var roomTable = new Table();
            roomTable.AddColumn("ID");
            roomTable.AddColumn("Floor");
            roomTable.AddColumn("Capacity");
            roomTable.AddColumn("Current");

            foreach (var r in rooms)
            {
                int id = Convert.ToInt32(r["Id"]);
                string marker = (currentRoomId.HasValue && currentRoomId.Value == id) ? "*" : "";
                string floor = r["FloorNumber"].ToString();
                string cap = r["RemainingCapacity"].ToString();
                roomTable.AddRow($"{marker}{id}", floor, cap);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]Available Rooms[/]");
            AnsiConsole.Write(roomTable);

            int roomId = AnsiConsole.Ask<int>("Enter Room ID:");
            if (!rooms.Any(r => Convert.ToInt32(r["Id"]) == roomId))
            {
                AnsiConsole.MarkupLine("[red]Invalid Room ID![/]");
                return null;
            }

            return new Dictionary<string, object>
            {
                { "DormitoryId", dormId },
                { "BlockId", blockId },
                { "RoomId", roomId }
            };
        }
    }
    public class Person
    {
        public string _fullName { get; set; }
        public string _socialNumber { get; set; }
        public string _phoneNumber { get; set; }
        public string _address { get; set; }

        public Person(string fullName, string socialNumber, string phoneNumber, string address)
        {
            _fullName = fullName;
            _socialNumber = socialNumber;
            _phoneNumber = phoneNumber;
            _address = address;
        }
    }
    public class Student : Person
    {
        public string _StudentID { get; set; }
        public string _RoomId { get; set; }
        public string _BlockId { get; set; }
        public string _DormitoryId { get; set; }
        protected List<string> _PersonalItems { get; set; }
        public Student(string fullName, string socialNumber, string phoneNumber, string address, string studentID, string roomId=null, string blockId=null, string dormitoryId = null, List<string> personalItems = null)
            : base(fullName, socialNumber, phoneNumber, address)
        {
            _StudentID = studentID;
            _RoomId = null;
            _BlockId = null;
            _DormitoryId = null;
            _PersonalItems = null;
        }

    }
    public class StudentManager
    {
        public static Student ToStudent(Dictionary<string, object> info)
        {
            return new Student(info["FullName"].ToString(), info["SocialNumber"].ToString(), info["PhoneNumber"].ToString(), info["Address"].ToString(), info["StudentId"].ToString(), info["RoomId"].ToString(), info["BlockId"].ToString(), info["DormitoryId"].ToString());
        }
        private static Dictionary<string, object> ToDictionary(Student student)
        {
            var info = new Dictionary<string, object>
            {
                {"FullName", student._fullName},
                {"SocialNumber", student._socialNumber},
                {"StudentId", student._StudentID},
                {"PhoneNumber", student._phoneNumber},
                {"Address", student._address},
                {"RoomId", string.IsNullOrEmpty(student._RoomId) ? DBNull.Value : student._RoomId},
                {"BlockId", string.IsNullOrEmpty(student._BlockId) ? DBNull.Value : student._BlockId},
                {"DormitoryId", DBNull.Value}
            };

            if (!string.IsNullOrEmpty(student._BlockId))
            {
                var block = Program.db.GetRecordsByField("Blocks", "Id", student._BlockId);
                if (block.Count > 0 && block[0]["DormitoryId"] != null && block[0]["DormitoryId"] != DBNull.Value)
                {
                    info["DormitoryId"] = block[0]["DormitoryId"];
                }
            }

            return info;
        }
        public static bool AddStudent(string fullname, string socialnumber, string phonenumber, string address, string studentid)
        {
            try
            {
                Student student = new Student(fullname, socialnumber, phonenumber, address, studentid);
                Program.db.InsertRecord("Students", StudentManager.ToDictionary(student));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }
        public static bool RemoveStudent(string socialNumber)
        {
            try
            {
                var studentRecord = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];

     
                string roomId = studentRecord["RoomId"] != DBNull.Value ? studentRecord["RoomId"].ToString() : null;
                string dormitoryId = studentRecord["DormitoryId"] != DBNull.Value ? studentRecord["DormitoryId"].ToString() : null;
                string blockId = studentRecord["BlockId"] != DBNull.Value ? studentRecord["BlockId"].ToString() : null;

                if (!string.IsNullOrEmpty(roomId))
                {
                    var room = Program.db.GetRecordsByField("Rooms", "Id", roomId)[0];
                    int cap = int.Parse(room["RemainingCapacity"].ToString());
                    cap++;
                    Program.db.UpdateRecord("Rooms", new Dictionary<string, object>
                    {
                        {"RemainingCapacity", cap}
                    }, "Id", roomId);
                        }

                if (!string.IsNullOrEmpty(blockId))
                {
                    var block = Program.db.GetRecordsByField("Blocks", "Id", blockId)[0];
                    int blockRem = int.Parse(block["RemainingCapacity"].ToString());
                    blockRem++;
                    Program.db.UpdateRecord("Blocks", new Dictionary<string, object>
                    {
                        {"RemainingCapacity", blockRem}
                    }, "Id", blockId);
                        }

                if (!string.IsNullOrEmpty(dormitoryId))
                {
                    var dorm = Program.db.GetRecordsByField("Dormitories", "Id", dormitoryId)[0];
                    int dormRem = int.Parse(dorm["RemainingCapacity"].ToString());
                    dormRem++;
                    Program.db.UpdateRecord("Dormitories", new Dictionary<string, object>
                    {
                        {"RemainingCapacity", dormRem}
                    }, "Id", dormitoryId);
                        }

    
                Program.db.DeleteRecord("Students", "SocialNumber", socialNumber);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        public static bool UpdateStudentInfoWithCurrentData(string SocialNumber, string newphone, string newaddress)
        {
            try
            {
                var studentRecord = Program.db.GetRecordsByField("Students", "SocialNumber", SocialNumber);
                var student = studentRecord[0];
                string currentPhoneNumber = student["PhoneNumber"].ToString();
                string currentAddress = student["Address"].ToString();

                var updateFields = new Dictionary<string, object>
                    {
                        { "PhoneNumber", newphone=="" ? currentPhoneNumber : newphone },
                        { "Address", newaddress == "" ? currentAddress : newaddress }
                    };
                Program.db.UpdateRecord("Students", updateFields, "SocialNumber", SocialNumber);
                return true;
            }
            catch (Exception)
            { return false; }
        }
        public static bool SerachStudent(string socialNumber, string phoneNumber = "")
        {
            try
            {
                if(phoneNumber == "")
                {
                    Program.db.ShowRecordsByField("Students", "SocialNumber", socialNumber);
                    return true;
                }
                else
                {
                    Program.db.ShowRecordsByField("Students", "PhoneNumber", phoneNumber);
                    return true;
                }
                    
            }
            catch(Exception) 
            { return false; }
            
        }
        public static bool ChangeDoirmiBlckRoom(string socialNumber, string dormitoryId, string blockId, string roomIdChosen)
        {
            try
            {
                
                var studentRecord = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
                StudentAccommodationHistory.CloseCurrentAccommodation(int.Parse(studentRecord["Id"].ToString()));
                string currentRoomId = studentRecord["RoomId"] != DBNull.Value ? studentRecord["RoomId"].ToString() : null;
                string currentDormitoryId = studentRecord["DormitoryId"] != DBNull.Value ? studentRecord["DormitoryId"].ToString() : null;
                string currentBlockId = studentRecord["BlockId"] != DBNull.Value ? studentRecord["BlockId"].ToString() : null;

                if (!string.IsNullOrEmpty(currentRoomId))
                {
                    var oldRoom = Program.db.GetRecordsByField("Rooms", "Id", currentRoomId)[0];
                    int oldCap = int.Parse(oldRoom["RemainingCapacity"].ToString());
                    oldCap++;
                    Program.db.UpdateRecord("Rooms", new Dictionary<string, object>
                    {
                        { "RemainingCapacity", oldCap }
                    }, "Id", currentRoomId);
                        }

                if (!string.IsNullOrEmpty(currentBlockId))
                {
                    var oldBlock = Program.db.GetRecordsByField("Blocks", "Id", currentBlockId)[0];
                    int oldCap = int.Parse(oldBlock["RemainingCapacity"].ToString());
                    oldCap++;
                    Program.db.UpdateRecord("Blocks", new Dictionary<string, object>
                    {
                        { "RemainingCapacity", oldCap }
                    }, "Id", currentBlockId);
                        }

                if (!string.IsNullOrEmpty(currentDormitoryId))
                {
                    var oldDorm = Program.db.GetRecordsByField("Dormitories", "Id", currentDormitoryId)[0];
                    int oldCap = int.Parse(oldDorm["RemainingCapacity"].ToString());
                    oldCap++;
                    Program.db.UpdateRecord("Dormitories", new Dictionary<string, object>
                    {
                        { "RemainingCapacity", oldCap }
                    }, "Id", currentDormitoryId);
                        }

                var newRoom = Program.db.GetRecordsByField("Rooms", "Id", roomIdChosen)[0];
                int newCap = int.Parse(newRoom["RemainingCapacity"].ToString());
                newCap--;
                Program.db.UpdateRecord("Rooms", new Dictionary<string, object>
                {
                    { "RemainingCapacity", newCap }
                }, "Id", roomIdChosen);

                var newBlock = Program.db.GetRecordsByField("Blocks", "Id", blockId)[0];
                int newBlockCap = int.Parse(newBlock["RemainingCapacity"].ToString());
                newBlockCap--;
                Program.db.UpdateRecord("Blocks", new Dictionary<string, object>
                {
                    { "RemainingCapacity", newBlockCap }
                }, "Id", blockId);

                var newDorm = Program.db.GetRecordsByField("Dormitories", "Id", dormitoryId)[0];
                int newDormCap = int.Parse(newDorm["RemainingCapacity"].ToString());
                newDormCap--;
                Program.db.UpdateRecord("Dormitories", new Dictionary<string, object>
                {
                    { "RemainingCapacity", newDormCap }
                }, "Id", dormitoryId);

                Program.db.UpdateRecord("Students", new Dictionary<string, object>
                {
                    { "DormitoryId", dormitoryId },
                    { "BlockId", blockId },
                    { "RoomId", roomIdChosen }
                }, "SocialNumber", socialNumber);

                
                StudentAccommodationHistory.AddAccommodationHistory(
                    int.Parse(studentRecord["Id"].ToString()),
                    int.Parse(dormitoryId),
                    int.Parse(blockId),
                    int.Parse(roomIdChosen)
                );

                return true;
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                return false;
            }
        }
        public static bool assignStudentToAccommodation(string SocialNumber, int dormitoryId, int blockId, int roomId)
        {
            try 
            {
                var student = Program.db.GetRecordsByField("Students", "SocialNumber", SocialNumber)[0];
                var accommodationinfo = new Dictionary<string, object>
                {
                    {"DormitoryId", dormitoryId},
                    {"BlockId" , blockId},
                    {"RoomId", roomId }
                };
                var room = Program.db.GetRecordsByField("Rooms", "Id", roomId)[0];
                int cap = int.Parse(room["RemainingCapacity"].ToString());
                Program.db.UpdateRecord("Students", accommodationinfo, "SocialNumber", SocialNumber);
                var updatecaproom = new Dictionary<string, object>
                {
                    {"RemainingCapacity", --cap }
                };
                Program.db.UpdateRecord("Rooms", updatecaproom, "Id", roomId);
                var dorm = Program.db.GetRecordsByField("Dormitories", "Id", dormitoryId)[0];
                var dormcap = int.Parse(dorm["RemainingCapacity"].ToString());
                var updatedormitory = new Dictionary<string, object>
                {
                    {"RemainingCapacity", --dormcap}
                };
                Program.db.UpdateRecord("Dormitories", updatedormitory, "Id", dormitoryId);
                var block = Program.db.GetRecordsByField("Blocks", "Id", blockId)[0];
                var blockcap = int.Parse(block["RemainingCapacity"].ToString());
                var updateblock= new Dictionary<string, object>
                {
                    {"RemainingCapacity", --blockcap}
                };
                Program.db.UpdateRecord("Blocks", updateblock, "Id", blockId);
                StudentAccommodationHistory.AddAccommodationHistory(int.Parse(student["Id"].ToString()), dormitoryId, blockId, roomId);
                return true;
            }
            catch (Exception)
            { return false; }

        }
    }
    public class Dormitory
    {
        public int Id { get; set; }

        public string _name;
        public string _address { get; set; }
        public string _responsible { get; set; }
        public int _Totalcapacity { get; set; }
        public int _remainingCapacity { get; set; }
        public Dormitory(string name, string address, int capcity, string responsible, string block = null)
        {
            _name = name;
            _address = address;
            _Totalcapacity = capcity;
            _remainingCapacity = capcity;
            _responsible = responsible;
        }

    }
    class DormitoryManager
    {
        //public static Student ToDormitory(Dictionary<string, object> info)
        //{
        //    return new Dormitory(info["Name"].ToString(), info["Address"].ToString(), info["capcity"], info["Responsible"].ToString());
        //}
        public static Dictionary<string, object> ToDictionary(Dormitory dormitory)
        {
            Dictionary<string, object> info = new Dictionary<string, object>
            {
                { "Name",dormitory._name},
                { "Address",dormitory._address},
                { "TotalCapacity",dormitory._Totalcapacity},
                { "Responsible",dormitory._responsible},
                { "RemainingCapacity",dormitory._remainingCapacity }
            };
            return info;
        }
        public static bool AddDormitory(string FullName, string Address, int capcity, string responsible)
        {
            try
            {

                var dormitory = new Dormitory(FullName, Address, capcity, responsible);

                Program.db.InsertRecord("Dormitories", ToDictionary(dormitory));
                var updatedormid = new Dictionary<string, object>
                {
                    {"DormitoryId", Program.db.GetRecordsByField("Dormitories","Name",FullName)[0]["Id"]}
                };
                Program.db.UpdateRecord("DormitorySupervisors", updatedormid, "SocialNumber", responsible);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool RemoveDormitory(string name)
        {
            try
            {
                Program.db.DeleteRecord("Dormitories", "Name", name);
                return true;
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                return false;
            }
        }
        public static bool UpdateDormitoryInfoWithCurrentData(string name, string newCapacity, string newAddress, string newResponsible)
        {
            try
            {
                var dormitoryRecord = Program.db.GetRecordsByField("Dormitories", "Name", name);
                var dormitory = dormitoryRecord[0];
                string currentCapcity = dormitory["Capacity"].ToString();
                string currentAddress = dormitory["Address"].ToString();
                string currentResponsible = dormitory["Responsible"].ToString();
                var updateFields = new Dictionary<string, object>
                {
                    { "Capacity", newCapacity=="" ? currentCapcity : newCapacity },
                    { "Address", newAddress=="" ? currentAddress : newAddress },
                    { "Responsible", newResponsible=="" ? currentResponsible : newResponsible }
                };
                Program.db.UpdateRecord("Dormitories", updateFields, "Name", name);
                return true;
            }
            catch (Exception)
            { return false; }

        }
        public static void ShowAlldormitories()
        {
            try
            {
                Program.db.ShowAllrecords("Dormitories");
            }
            catch (Exception)
            {
                ENUserInterFace.dormitorymngmnt();
            }
        }
    }
    public class Block
    {
        public int Id { get; set; }
        public string _name { set; get; }
        public string _responsible { get; set; }
        public string _dormitoryId { get; set; }
        public int _NO_floors { get; set; }
        public int _NO_rooms { get; set; }
        public int _totalCapacity {  get; set; }
        public int _remainingCapacity { get; set; }
        public Block(string dormitory, string name, int floor, int room, string responsible, int capacity)
        {
            _dormitoryId = dormitory;
            _name = name;
            _NO_floors = floor;
            _NO_rooms = room;
            _responsible = responsible;
            _totalCapacity = capacity;
            _remainingCapacity = capacity;
            
        }
    }
    class BlocksManager
    {
        //public static Student ToBlock(Dictionary<string, object> info)
        //{
        //    return new Block(info["DormitoryId"].ToString(), info["Name"].ToString(), info["NumberOfFloors"], info["NumberOfRooms"], info["Responsible"].ToString());
        //}
        public static Dictionary<string, object> ToDictionary(Block block)
        {
            Dictionary<string, object> info = new Dictionary<string, object>
            {
                { "DormitoryId",block._dormitoryId},
                { "Name",block._name},
                { "NumberOfFloors",block._NO_floors},
                { "NumberOfRooms",block._NO_rooms },
                { "Responsible",block._responsible},
                { "TotalCapacity", block._totalCapacity},
                { "RemainingCapacity", block._remainingCapacity}
            };
            return info;
        }
        public static bool AddBlock(string dormiId, string name, string floor, string room, string resposible, int capacity)
        {
            try
            {
                Block block = new Block(dormiId, name, int.Parse(floor), int.Parse(room), resposible, capacity);
                Program.db.InsertRecord("Blocks", ToDictionary(block));

                string blockid = Program.db.GetRecordsByField("Blocks", "Name", name)[0]["Id"].ToString();
                int totalFloors = int.Parse(floor);
                int totalRooms = int.Parse(room);

                for (int i = 1; i <= totalRooms; i++)
                {
                    int floorNumber = (i - 1) * totalFloors / totalRooms + 1;
                    Room roomObj = new Room(floorNumber, blockid);
                    Program.db.InsertRecord("Rooms", roomObj.ToDictionary());
                }
                var dormitory = Program.db.GetRecordsByField("Dormitories", "Id", dormiId)[0];
                var update = new Dictionary<string, object>
                {
                    { "RemainingCapacity", int.Parse(dormitory["RemainingCapacity"].ToString())- int.Parse(capacity.ToString())}
                };
                Program.db.UpdateRecord("Dormitories", update, "Id", dormiId);
                return true;
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                return false;
            }
        }
        public static bool RemoveBlock(string name)
        {
            try
            {
                Program.db.DeleteRecord("Blocks", "Name", name);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool UpdateBlockInfoWithCurrentData(string name, string newFloor, string newRoom, string newResponsible)
        {
            try
            {
                var blockRecord = Program.db.GetRecordsByField("Blocks", "Name", name);


                var block = blockRecord[0];
                string currentFloor = block["NumberOfFloors"].ToString();
                string currentRoom = block["NumberOfRooms"].ToString();
                string currentResponsible = block["Responsible"].ToString();

                var updateFields = new Dictionary<string, object>
                {
                    { "NumberOfFloors", newFloor=="" ? currentFloor : newFloor },
                    { "NumberOfRooms", newRoom=="" ? currentRoom : newRoom },
                    { "Responsible", newResponsible=="" ? currentResponsible : newResponsible }
                };
                Program.db.UpdateRecord("Blocks", updateFields, "Name", name);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static void ShowAllblocks()
        {
            try
            {
                Program.db.ShowAllrecords("Blocks");
            }
            catch (Exception)
            {
                ENUserInterFace.blockmngmnt();
            }
        }
    }
    class Room
    {
        string _block;
        int _NO_floors, _capacity;
        public Room(int no_floor, string block)
        {
            _NO_floors = no_floor;
            _capacity = 6;
            _block = block;
        }
        public Dictionary<string, object> ToDictionary()
        {
            var info = new Dictionary<string, object>
            {
                {"BlockId",int.Parse(_block) },
                {"TotalCapacity",_capacity},
                {"FloorNumber",_NO_floors },
                { "RemainingCapacity", _capacity}
            };
            return info;
        }
    }
    class Equipment
    {
        public static int _ItemCounter = 0;

        public string _type;
        public string _partNumber;
        public string _propertyNumber;
        public string _condition;
        public int _RoomId;
        public int _BlockId;
        public int _DormId;
        public Equipment() { }
        public Equipment(string type, string condition, int roomid, int blockid, int dormid)
        {
            _type = type;
            _condition = condition;
            _RoomId = roomid;
            _BlockId = blockid;
            _DormId = dormid;
            this.partNumber = "";
            this.propertyNumber = "";
        }

        public Equipment(string type, string condition, int blockid, int dormid)
        {
            _type = type;
            _condition = condition;
            _BlockId = blockid;
            _DormId = dormid;
            _RoomId = -1;
            partNumber = "";
            propertyNumber = "";
        }

        public string partNumber
        {
            get => _partNumber;
            set
            {
                if (_type.ToLower() == "fridge") _partNumber = "001";
                else if (_type.ToLower() == "desk") _partNumber = "002";
                else if (_type.ToLower() == "chair") _partNumber = "003";
                else if (_type.ToLower() == "bed") _partNumber = "004";
                else if (_type.ToLower() == "locker") _partNumber = "005";
            }
        }

        public string propertyNumber
        {
            get => _propertyNumber;
            set
            {
                string counter = _ItemCounter.ToString();
                _propertyNumber = $"{_DormId}{_BlockId}{_partNumber}{counter.PadLeft(3, '0')}";
                _ItemCounter++;
            }
        }


        public virtual Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object> {
            {"Type", _type},
            {"PartNumber", _partNumber},
            {"PropertyNumber", _propertyNumber},
            {"Condition", _condition},
            {"RoomId", _RoomId == -1 ? DBNull.Value : _RoomId}
        };

        }

        public static Equipment FromDictionary(Dictionary<string, object> equipmentDict)
        {
            Equipment equipment = new Equipment(equipmentDict["Type"].ToString(), (Condition)equipmentDict["Condition"], int.Parse(equipmentDict["PartNumber"].ToString()), int.Parse(equipmentDict["PropertyNumber"].ToString()), (int)equipmentDict["RoomId"]);
            return equipment;
        }


    }
    class PersonalEquipment : Equipment
    {
        private Student _owner;

        public PersonalEquipment(string type, Condition condition, int roomid, int blockid, int dormid, Student owner) : base(type, condition, roomid, blockid, dormid)
        {
            _owner = owner;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> outputDict = base.ToDictionary();
            outputDict.Add("OwnerId", this._owner);
            return outputDict;

        }
    }
    class EquipmentManager
    {

        public static bool addEquipmentToDB(Equipment newEquipment)
        {
            try
            {
                Dictionary<string, object> info = newEquipment.ToDictionary();

                Program.db.InsertRecord("Equipment", info);
                return true;
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                return false;
            }
        }

        public static void checkEquipmentExistence(string propertyNumber)
        {
            List<Dictionary<string, object>> Equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber);
            if (Equipment.Count == 0)
            {
                throw new ArgumentException($"Equipment With Property Number {propertyNumber}, Does Not Exist.");
            }
        }

        public static bool isEquipmentInRoom(string partNumber, int roomId)
        {
            List<Dictionary<string, object>> Equipment = Program.db.GetRecordsByField("Equipment", "PartNumber", partNumber);
            foreach (var equipment in Equipment)
            {
                if (int.Parse(equipment["RoomId"].ToString()) == roomId) return true;
            }
            return false;
        }

        public static bool doesStudentHaveEquipment(string partNumber, string socialNumber)
        {
            int studentId = int.Parse(Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0]["Id"].ToString());
            List<Dictionary<string, object>> Equipment = Program.db.GetRecordsByField("Equipment", "PartNumber", partNumber);
            foreach (var equipment in Equipment)
            {
                if (int.Parse(equipment["OwnerId"].ToString()) == studentId) return true;
            }
            return false;
        }

        public static void assignEquipmentToRoom(string propertyNumber, string roomId)
        {
            int blockid = int.Parse(propertyNumber[1].ToString());
            string partNumber = propertyNumber.Substring(2, 3);

            checkEquipmentExistence(propertyNumber);

            Dictionary<string, object> Equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0];
            string roomid = Equipment["RoomId"].ToString();
            WriteLine($"roomid : {roomid}, {roomId}");
            if (!string.IsNullOrEmpty(roomid))
            {
                if (roomid == roomId.ToString())
                {
                    throw new ArgumentException("Equipment Already Assigned to This Room.");
                }

                if (int.Parse(roomid) != -1 && Equipment["RoomId"] != DBNull.Value)
                {
                    throw new ArgumentException("Invalid Property Number; This Equipment is Already Assigned To a Room.");
                }

                if (partNumber != "001")
                {
                    throw new ArgumentException("Invalid Property Number; Cannot Add a Personal Equipment To a Room Directly.");
                }

                Dictionary<string, object> Room = Program.db.GetRecordsByField("Rooms", "Id", roomId)[0];
                if (blockid != int.Parse(Room["BlockId"].ToString()))
                {
                    throw new ArgumentException("The Room and The Equipment Must Be in The Same Dormitory and Block.");
                }

                if (!isEquipmentInRoom(partNumber, int.Parse(roomId)))
                {
                    throw new ArgumentException("An Equipment of The Same Type Already Exists in This Room.");
                }
            }
            Dictionary<string, object> EquipmentUpdatedValues = new Dictionary<string, object>
            {
            {"RoomId", roomId}
            };
            Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
        }

        public static void assignEquipmentToStudent(string propertyNumber, string socialNumber)
        {
            int blockid = int.Parse(propertyNumber[1].ToString());
            string partNumber = propertyNumber.Substring(2, 3);

            checkEquipmentExistence(propertyNumber);

            Dictionary<string, object> Student = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
            Dictionary<string, object> Equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0];
            if (Equipment["OwnerId"].ToString() != "")
            {
                throw new ArgumentException("Equipment Already Assigned to This Student.");
            }

            if (partNumber == "001")
            {
                throw new ArgumentException("Invalid Property Number; Cannot Assign a Shared Equipment To a Single Person.");
            }
            object ownerid = Equipment["OwnerId"];
            WriteLine($"owner id : {ownerid}");
            if (!string.IsNullOrEmpty(ownerid.ToString()))
            {
                if (int.Parse(ownerid.ToString()) != -1 && ownerid != DBNull.Value)
                {
                    throw new ArgumentException("Invalid Property Number; This Equipment is Already Assigned To a Student.");
                }
                if (blockid != int.Parse(Student["BlockId"].ToString()))
                {
                    throw new ArgumentException("The Equipment Must Be in The Same Block and Dormitory as Student.");
                }

                if (!doesStudentHaveEquipment(partNumber, socialNumber))
                {
                    throw new ArgumentException("An Equipment of The Same Type is Already Assigned To This Student.");
                }
            }
            Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
            var StudentId = studentDict["Id"];
            var RoomId = studentDict["RoomId"];
            Dictionary<string, object> EquipmentUpdatedValues = new Dictionary<string, object> {
                { "RoomId", RoomId },
                { "OwnerId", StudentId }
            };
            Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
        }

        public static void exchangeEquipmentBetweenRooms(string propertyNumber, string roomId)
        {
            int blockid = int.Parse(propertyNumber[1].ToString());
            string partNumber = propertyNumber.Substring(2, 3);

            checkEquipmentExistence(propertyNumber);

            Dictionary<string, object> Room = Program.db.GetRecordsByField("Rooms", "Id", roomId)[0];
            Dictionary<string, object> Equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0];
            if (partNumber != "001")
            {
                throw new ArgumentException("You Can't Exchange a Personal Equipment In Here.");
            }

            if (blockid != int.Parse(Room["BlockId"].ToString()))
            {
                throw new ArgumentException("The Destination Room and Equipment Must Be in The Same Dormitory and Block.");
            }

            if (roomId == Equipment["RoomId"].ToString())
            {
                throw new ArgumentException("This Equipment is Already Assigned to This Room.");
            }

            if (!isEquipmentInRoom(partNumber, int.Parse(roomId)))
            {
                throw new ArgumentException("No Equipment of This Type is in The Room to Be Replaced.");
            }

            // changing room ID for equipment that is being replaced
            List<Dictionary<string, object>> equipmentInRoom = Program.db.GetRecordsByField("Equipment", "RoomId", roomId);
            foreach (var equipment in equipmentInRoom)
            {
                if (equipment["PartNumber"].ToString() == partNumber)
                {
                    Dictionary<string, object> equipmentChangedRoomId = new Dictionary<string, object> {
                    {"RoomId", Equipment["RoomId"]}
                };
                    Program.db.UpdateRecord("Equipment", equipmentChangedRoomId, "PropertyNumber", equipment["PropertyNumber"]);
                }
            }

            // changing room ID for equipment that is replacing the equipment in room
            Dictionary<string, object> changedRoomId = new Dictionary<string, object> {
                { "RoomId", roomId}
            };

            Program.db.UpdateRecord("Equipment", changedRoomId, "PropertyNumber", propertyNumber);
        }

        public static void changeStudentEquipment(string oldPropertyNumber, string newPropertyNumber, string socialNumber)
        {
            int newBlockId = int.Parse(newPropertyNumber[1].ToString());
            string newPartNumber = newPropertyNumber.Substring(2, 3);
            string oldPartNumber = oldPropertyNumber.Substring(2, 3);

            checkEquipmentExistence(oldPropertyNumber);
            checkEquipmentExistence(newPropertyNumber);

            Dictionary<string, object> newEquipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", newPropertyNumber)[0];
            Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
            Dictionary<string, object> oldEquipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", oldPropertyNumber)[0];
            if (studentDict["BlockId"].ToString() != newBlockId.ToString())
            {
                throw new ArgumentException("The New Equipment and Student Must Be in The Same Dormitory and Block.");
            }

            if (newPartNumber == "001")
            {
                throw new ArgumentException("You Cannot Assign a Shared Equipment To a Student.");
            }

            if (newPartNumber != oldPartNumber)
            {
                throw new ArgumentException("The Two Equipment Are Not of The Same Type.");
            }

            if (int.Parse(newEquipment["OwnerId"].ToString()) != -1 && newEquipment["OwnerId"] != DBNull.Value)
            {
                throw new ArgumentException($"The New Equipment Has another Owner with Id: {newEquipment["OwnerId"]}");
            }

            if (oldEquipment["OwnerId"] != studentDict["Id"])
            {
                throw new ArgumentException("The Old Equipment You Specified Does Not Belong To This Student.");
            }

            var ownerId = studentDict["Id"];
            var roomId = studentDict["RoomId"];

            Dictionary<string, object> newEquipmentUpdatedValues = new Dictionary<string, object> {
                { "RoomId", roomId},
                { "OwnerId", ownerId}
            };

            Dictionary<string, object> oldEquipmentUpdatedValues = new Dictionary<string, object> {
                { "RoomId", DBNull.Value},
                { "OwnerId", DBNull.Value}
            };

            Program.db.UpdateRecord("Equipment", newEquipmentUpdatedValues, "PropertyNumber", newPropertyNumber);
            Program.db.UpdateRecord("Equipment", oldEquipmentUpdatedValues, "PropertyNumber", oldPropertyNumber);
        }

        public static void changeEquipmentCondition(string propertyNumber, Condition condition)
        {
            checkEquipmentExistence(propertyNumber);

            Dictionary<string, object> Equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0];
            if (Equipment["Condition"].ToString().ToLower() == condition.ToString().ToLower())
            {
                throw new ArgumentException($"Equipment Condition Already Set To: {condition.ToString()}");
            }

            Dictionary<string, object> UpdatedCondition = new Dictionary<string, object> {
                { "Condition", condition.ToString()}
            };

            Program.db.UpdateRecord("Equipment", UpdatedCondition, "PropertyNumber", propertyNumber);
        }

        public static Condition checkCondition(string propertyNumber)
        {
            Equipment equipment = Equipment.FromDictionary(Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0]);
            return equipment._condition;
        }

        public static void equipmentAssignedToRoom(int RoomId)
        {
            List<Dictionary<string, object>> roomEquipment = Program.db.GetRecordsByField("Equipment", "RoomId", RoomId);
            foreach (Dictionary<string, object> equipment in roomEquipment)
            {
                if (equipment["OwnerId"] != DBNull.Value)
                {
                    Dictionary<string, object> owner = Program.db.GetRecordsByField("Students", "Id", equipment["OwnerId"])[0];
                    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}, Owned by: {owner["FullName"]}");
                }
                else
                {
                    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
                }
            }
        }

        public static void equipmentAssignedToStudent(int StudentId)
        {
            List<Dictionary<string, object>> studentEquipment = Program.db.GetRecordsByField("Equipment", "OwnerId", StudentId);
            foreach (Dictionary<string, object> equipment in studentEquipment)
            {
                WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
            }
        }

        public static void showEquipmentWithCondition(Condition condition)
        {
            List<Dictionary<string, object>> allEquipment = Program.db.GetRecordsByField("Equipment", "Condition", condition.ToString());
            foreach (Dictionary<string, object> equipment in allEquipment)
            {
                WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, in Room: {equipment["RoomId"]}");
            }
        }

        public static void registerRepairRequest(string propertyNumber)
        {
            Dictionary<string, object> Equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0];
            List<Dictionary<string, object>> repairReq = Program.db.GetRecordsByField("RepairRequests", "PropertyNumber", propertyNumber);
            if (Equipment["Condition"].ToString().ToLower() != "broken")
            {
                throw new ArgumentException("The Equipment Must Be First Registered As Broken Before Requesting Repair.");
            }

            if (repairReq.Count != 0 && Equipment["Condition"].ToString().ToLower() == "repairing")
            {
                throw new ArgumentException("A Repair Request For This Equipment is At Hand.");
            }

            RepairRequest req = new RepairRequest(propertyNumber);
            Dictionary<string, object> reqDict = req.ToDictionary();
            changeEquipmentCondition(propertyNumber, Condition.Reparing);
            Program.db.InsertRecord("RepairRequests", reqDict);
        }
    }
    public class DormitorySupervisor : Person
    {
        public string _position { get; set; }
        public string _dormitory { get; set; }
        public DormitorySupervisor(string fullName, string socialNumber, string phoneNumber, string address, string position, string dormitory = null) : base(fullName, socialNumber, phoneNumber, address)
        {
            _position = position;
            _dormitory = null;
        }

    }
    public class DormitorySuperVisorManager
    {
        public static Dictionary<string, object> ToDictionary(DormitorySupervisor dormitorySupervisor)
        {
            var values = new Dictionary<string, object>
             {
                { "FullName", dormitorySupervisor._fullName },
                { "SocialNumber", dormitorySupervisor._socialNumber },
                { "PhoneNumber", dormitorySupervisor._phoneNumber },
                { "Address", dormitorySupervisor._address },
                { "Position", dormitorySupervisor._position },
                { "DormitoryId", dormitorySupervisor._dormitory}
             };
            return values;
        }
        public static bool AddSuperVisor(string FullName, string SocialNumber, string PhoneNumber, string Address, string Position)
        {
            try
            {
                DormitorySupervisor dormiSupervisor = new DormitorySupervisor(FullName, SocialNumber, PhoneNumber, Address, Position, null);
                Program.db.InsertRecord("DormitorySupervisors", ToDictionary(dormiSupervisor));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool DeleteSpuervisor(string socialNumber)
        {
            try
            {
                Program.db.DeleteRecord("dormitorySupervisor", "socialNumber", socialNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool UpdateSupervisor(string SoicalNumber, string newPhone, string newAddress, string newPosition)
        {

            try
            {
                var supervisorRecord = Program.db.GetRecordsByField("DormitorySupervisors", "SocialNumber", SoicalNumber);
                var supervisor = supervisorRecord[0];
                string currentphonenumber = supervisor["PhoneNumber"].ToString();
                string currentAddress = supervisor["Address"].ToString();
                string currentPosition = supervisor["Position"].ToString();
                var updateFields = new Dictionary<string, object>
                {
                    { "PhoneNumber", newPhone=="" ? currentphonenumber : newPhone },
                    { "Address", newAddress=="" ? currentAddress : newAddress },
                    { "Position", newPosition=="" ? currentPosition : newPosition }
                };
                Program.db.UpdateRecord("DormitorySupervisors", updateFields, "SocialNumber", SoicalNumber);
                return true;
            }
            catch (Exception)
            { return false; }
        }
        public static void ShowAllSupervisors()
        {
            try
            {
                Program.db.ShowAllrecords("DormitorySupervisors");
            }
            catch (Exception)
            {
                ENUserInterFace.sueprvisormngmnt();
            }
        }
    }
    public class DormitoryBlockSupervisor : Person
    {


        public string Role { get; set; }
        public string BlockUnderResponsibility { get; set; }
        public string Studentid { get; set; }
        

        public DormitoryBlockSupervisor(string fullName, string socialNumber, string phoneNumber, string address, string role) : base(fullName, socialNumber, phoneNumber, address)
        {
            Studentid = "";
            Role = role;
            BlockUnderResponsibility = "";
        }

        public DormitoryBlockSupervisor(string fullName, string socialNumber, string phoneNumber, string address, string studentid , string role) : base(fullName, socialNumber, phoneNumber, address)
        {
            Studentid = studentid;
            Role = role;
            BlockUnderResponsibility = "";
        }

    }
    public class DormitoryBlockSupervisorManager
    {
        public static Dictionary<string, object> ToDictionary(DormitoryBlockSupervisor blocksupervisor)
        {
            var values = new Dictionary<string, object>
            {
                {"FullName", blocksupervisor._fullName},
                {"SocialNumber", blocksupervisor._socialNumber},
                {"PhoneNumber", blocksupervisor._phoneNumber},
                {"Address", blocksupervisor._address},
                {"StudentId",blocksupervisor.Studentid == "" ? DBNull.Value : int.Parse(blocksupervisor.Studentid)},
                {"BlockId", blocksupervisor.BlockUnderResponsibility == "" ? DBNull.Value : int.Parse(blocksupervisor.BlockUnderResponsibility)},
                {"Role", blocksupervisor.Role}
            };
            return values;
        }
        public static string AddBlockSupervisor(string FullName, string SocialNumber, string PhoneNumber, string Address, string role)
        {
            try
            {
                if (Program.db.DoesSocialNumberExist("Students", SocialNumber))
                {
                    if (role.ToLower() == "student")
                    {
                        string studentid = Program.db.GetRecordsByField("Students", "SocialNumber", SocialNumber)[0]["Id"].ToString();
                        DormitoryBlockSupervisor dormiblcksupervisor = new DormitoryBlockSupervisor(FullName, SocialNumber, PhoneNumber, Address, studentid, role);
                        Program.db.InsertRecord("DormitoryBlockSupervisors", ToDictionary(dormiblcksupervisor));
                        return "success";
                    }
                    else
                    {
                        return "Student Exists But Role Is Not For a Student";
                    }

                }
                else if(role.ToLower() == "student")
                {
                    return "studentdosenotexist";
                }
                else
                {
                    DormitoryBlockSupervisor dormiblcksupervisor = new DormitoryBlockSupervisor(FullName, SocialNumber, PhoneNumber, Address, role);
                    Program.db.InsertRecord("DormitoryBlockSupervisors", ToDictionary(dormiblcksupervisor));
                    return "success";
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                return "unsucess";
            }
        }
        public static bool RemoveDormitoryBlockSupervisor(string socialNumber)
        {
            try
            {
                Program.db.DeleteRecord("DormitoryBlockSupervisors", "SocialNumber", socialNumber);
                return true;
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                return false;
            }
            
        }
        public static bool UpdateDormitoryBlockSupervisor(string socialNumber, string newPhoneNumber, string newAddress, string newrole)
        {
            try
            {
                var supervisorRecord = Program.db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", socialNumber);
                var supervisor = supervisorRecord[0];
                string currentphonenumber = supervisor["PhoneNumber"].ToString();
                string currentAddress = supervisor["Address"].ToString();
                string currentRole = supervisor["Role"].ToString();
                var updateFields = new Dictionary<string, object>
                {
                    { "PhoneNumber", newPhoneNumber=="" ? currentphonenumber : newPhoneNumber },
                    { "Address", newAddress=="" ? currentAddress : newAddress },
                    { "Role", newrole=="" ? currentRole : newrole }
                };
                Program.db.UpdateRecord("DormitoryBlockSupervisors", updateFields, "SocialNumber", socialNumber);
                return true;
            }
            catch (Exception)
            { return false; }
        }
        public static void ShowAllBlockSupervisors()
        {
            try
            {
                Program.db.ShowAllrecords("DormitoryBlockSupervisors");
            }
            catch (Exception)
            {
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
    }
    class Report
    {
        public static void ShowStudentAccommodation()
        {
            try
            {
                Program.showStudentAccommodation();
            }
            catch (Exception)
            {
                ENUserInterFace.reporting();
            }
        }
        public static void showRemainingCapacity()
        {
            try
            {
                Program.showRemainingCapacity();
            }
            catch (Exception)
            {
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipment()
        {
            try
            {
                Program.db.ShowAllrecords("Equipment");
            }
            catch (Exception)
            {
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipmentAssignedToEachRoom()
        {
            try
            {
                Program.showAllEquipmentAssignedToEachRoom();
            }
            catch (Exception)
            {
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipmentAssignedToEachStudent()
        {
            try
            {
                Program.showAllEquipmentAssignedToEachStudent();
            }
            catch (Exception)
            {
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllRepairRequests()
        {
            try
            {
                Program.db.ShowAllrecords("RepairRequests");
            }
            catch (Exception)
            {
                ENUserInterFace.reporting();
            }
        }

    }
    public enum Condition
    {
        Intact,
        Broken,
        Reparing,
    }
    public enum RequestStatus
    {
        Pending,
        Done
    }
    class RepairRequest
    {
        private string propertyNumber;
        private RequestStatus status;

        public RepairRequest(string propertyNumber, RequestStatus status = RequestStatus.Pending)
        {
            this.propertyNumber = propertyNumber;
            this.status = status;
        }

        public void setStatusToDone()
        {
            this.status = RequestStatus.Done;
        }

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> outDict = new Dictionary<string, object>
                {
                { "PropertyNumber", this.propertyNumber},
                { "Status", this.status.ToString()}
            };

            return outDict;
        }

        public static RepairRequest FromDictionary(Dictionary<string, object> requestDict)
        {
            return new RepairRequest(requestDict["PropertyNumber"].ToString(), (RequestStatus)requestDict["Status"]);

        }
    }
    public class Director
    {
        public string _FullName { get; set; }
        public string _SocialNumber { get; set; }
        public string _PhoneNumber { get; set; }
        public string _UserName { get; set; }
        public string _Password { get; set; }
        public Director(string fullname, string socialnumber, string phonenumber, string username, string password)
        {
            _FullName = fullname;
            _SocialNumber = socialnumber;
            _PhoneNumber = phonenumber;
            _UserName = username;
            _Password = password;
        }
    }
    class DirectorManager
    {
        public static Dictionary<String, object> ToDictionary(Director director)
        {
            return new Dictionary<string, object> {
                { "FullName", director._FullName },
                { "SocialNumber", director._SocialNumber },
                { "PhoneNumber", director._PhoneNumber },
                { "UserName", director._UserName },
                { "Password", Security.HashPassword(director._Password) }
            };
        }
        public static bool AddDirector(Director director)
        {
            try
            {
                Program.db.InsertRecord("Director", DirectorManager.ToDictionary(director));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool Login(string username, string password)
        {
            if (Program.db.DoesUserNameExist(username))
            {
                var directrRecord = Program.db.GetRecordsByField("Director", "UserName", username);
                if (directrRecord[0]["Password"].ToString() == Security.HashPassword(password))
                {
                    return true;
                }
                return false;

            }
            return false;
        }
        public static bool ResetPassword(string username, string socialnumber, string phonenumber)
        {
            var directorRecord = Program.db.GetRecordsByField("Director", "UserName", username);
            if (directorRecord[0]["SocialNumber"].ToString() == socialnumber && directorRecord[0]["PhoneNumber"].ToString() == phonenumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool GetNewpassword(string username, string newpass)
        {
            var directorRecord = Program.db.GetRecordsByField("Director", "UserName", username);
            try
            {
                Dictionary<string, object> newinfo = new Dictionary<string, object>
            {
                { "Password", Security.HashPassword( newpass) }
            };
                Program.db.UpdateRecord("Director", newinfo, "UserName", username);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    class Security
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
        public static bool ValidPhoneNumber(string phoneNumber)
        {
            string regex = @"^09(10|11|12|13|14|15|16|17|18|19|00|01|02|03|04|05|06|07|08|09|30|33|35|36|37|38|39)\d{7}$";
            return Regex.IsMatch(phoneNumber, regex);
        }

        public static bool ValidSocialNumber(string socialNumber)
        {
            if (string.IsNullOrEmpty(socialNumber) || string.IsNullOrWhiteSpace(socialNumber)) return false;
            if (socialNumber.Distinct().Count() == 1) return false;
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += (socialNumber[i] - '0') * (10 - i);
            }
            int remainder = sum % 11;
            int checkdigit = socialNumber[9] - '0';
            return (remainder < 2 && checkdigit == remainder) || (remainder >= 2 && checkdigit == (11 - remainder));
        }
        public static string ReadPasswordWithToggleSpectre(bool re = true)
        {
            var password = new StringBuilder();
            bool showPassword = false;
            ConsoleKeyInfo key;

            AnsiConsole.MarkupLine("[yellow]Enter your password (press [green]F2[/] to toggle visibility):[/]");

            void RenderPrompt()
            {
                if (re)
                    AnsiConsole.Markup("[cyan]Password : [/]");
                else
                    AnsiConsole.Markup("[cyan]Confirm Password : [/]");
            }

            RenderPrompt();

            while (true)
            {
                key = ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.F2)
                {
                    showPassword = !showPassword;

                    Write("\r");

                    int promptLength = (re ? "Password : ".Length : "Confirm Password : ".Length) + 10 + password.Length;

                    Write(new string(' ', promptLength));
                    Write("\r");

                    RenderPrompt();

                    if (showPassword)
                        Write(password.ToString());
                    else
                        Write(new string('*', password.Length));
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;

                    Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    if (showPassword)
                        Write(key.KeyChar);
                    else
                        Write("*");
                }
            }

            return password.ToString();
        }
    }
    public class StudentAccommodationHistory
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int DormitoryId { get; set; }
        public int BlockId { get; set; }
        public int RoomId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public StudentAccommodationHistory(int studentId, int dormitoryId, int blockId, int roomId, string startDate, string endDate = null)
        {
            StudentId = studentId;
            DormitoryId = dormitoryId;
            BlockId = blockId;
            RoomId = roomId;
            StartDate = startDate;
            EndDate = endDate;
        }
        public StudentAccommodationHistory() { }

        public static void AddAccommodationHistory(int studentId, int dormitoryId, int blockId, int roomId)
        {
            var record = new Dictionary<string, object>
            {
                {"StudentId", studentId},
                {"DormitoryId", dormitoryId},
                {"BlockId", blockId},
                {"RoomId", roomId},
                {"StartDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                {"EndDate", null}
            };

            Program.db.InsertRecord("StudentAccommodationHistory", record);
        }

        public static void CloseCurrentAccommodation(int studentId)
        {
            var records = Program.db.GetRecordsByField("StudentAccommodationHistory", "StudentId", studentId)
                                    .Where(r => r["EndDate"] == DBNull.Value).ToList();
            if (records.Count == 0)
                return;

            var lastRecordId = Convert.ToInt32(records.Last()["Id"]);

            var update = new Dictionary<string, object>
            {
                {"EndDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
            };

            Program.db.UpdateRecord("StudentAccommodationHistory", update, "Id", lastRecordId);
        }
    }
    public class ENUserInterFace
    {
        public static void Run()
    {
        while (true)
        {
            Clear();

                var panel = new Panel("[bold yellow]🏠 Main Menu[/]")
                    .RoundedBorder()
                    .BorderStyle(Style.Parse("yellow"))
                    .Padding(1, 1, 1, 1);

                AnsiConsole.Write(panel);

                var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select an option[/]")
                    .PageSize(5)
                    .AddChoices(new[] {
                        "🔐 Login Admin",
                        "📝 Register Admin",
                        "🔑 Forgot Password",
                        "📃 Guide",
                        "🚪 Exit"
                    }));

            switch (choice)
            {
                case "🔐 Login Admin":
                    AdminLogin();
                    break;
                case "📝 Register Admin":
                    RegisterAdmin();
                    break;
                case "🔑 Forgot Password":
                    ForgotPassword();
                    break;
                case "📃 Guide":
                    ShowGuide();
                        break;
                case "🚪 Exit":
                    AnsiConsole.MarkupLine("[bold green]Goodbye! 👋[/]");
                    return;
            }

            AnsiConsole.MarkupLine("\n[grey]Press any key to return to the main menu...[/]");
            ReadKey(true);
        }
    }
        private static void AdminLogin()
        {
            Clear();
            AnsiConsole.Write(
                new Panel("[bold underline green]👤 Admin Login[/]")
                .RoundedBorder()
                .BorderColor(Spectre.Console.Color.Green));

            string username = AnsiConsole.Ask<string>("Username : ");
            if (checkback(username)) Run();

            string password = Security.ReadPasswordWithToggleSpectre();
            if (checkback(password)) Run();

            bool success = false;

            AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("grey")) 
                .Start("[grey]Checking credentials...[/]", ctx =>
                {
                    Thread.Sleep(2000); 

                    success = DirectorManager.Login(username, password);

                    if (success)
                    {
                        ctx.Status($"\n[bold green]✅ Welcome back, {username}![/]"); 
                        ctx.SpinnerStyle(Style.Parse("green")); 
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        ctx.Status("[bold red]❌ Invalid username or password.[/]");
                        ctx.SpinnerStyle(Style.Parse("red"));
                        Thread.Sleep(2000);
                    }
                });

            if (success)
            {
                mainMenu();
            }
        }
        private static void RegisterAdmin()
        {
            Clear();
            AnsiConsole.Write(
                new Panel("[bold underline blue]📝 Register New Admin[/]")
                .RoundedBorder()
                .BorderColor( Spectre.Console.Color.Blue));
            string socialNumber = "";
            string phoneNumber = "";
            string password = "";
            string fullname = AnsiConsole.Ask<string>("Full Name : ");
            if (checkback(fullname)) Run();
            while (true)
            {
                socialNumber = AnsiConsole.Ask<string>("Social Number : ");
                if(checkback(socialNumber))
                {
                    Run();
                    break;
                }
                else if (Program.db.GetRecordsByField("Director", "SocialNumber", socialNumber).Count() > 0)
                {
                    AnsiConsole.MarkupLine("[red]👤❌This Direcotr already exists ... [/]");
                    Thread.Sleep(3000);
                    continue;
                }
                if (!Security.ValidSocialNumber(socialNumber))
                {
                    AnsiConsole.MarkupLine("[red]🔢The entered Social number is invalid,please retry .[/]");
                    Thread.Sleep(3000);
                }
                else
                {
                    break;
                }
                
            }

            while (true)
            {
                phoneNumber = AnsiConsole.Ask<string>("Phone Number : ");
                if (checkback(phoneNumber))
                {
                    Run();
                    break;
                }
                if(!Security.ValidPhoneNumber(phoneNumber))
                {
                    AnsiConsole.MarkupLine("[red]The entered Phone number is invalid,please retry .[/]");
                    Thread.Sleep(3000);
                }
                else
                {
                    break;
                }
            }

            string username = AnsiConsole.Ask<string>("Username : ");
            if(checkback(username)) Run();
            while (true)
            {
                password = Security.ReadPasswordWithToggleSpectre();
                if(checkback(password))
                {
                    Run();
                    break;
                }
                if (password.Length < 8)
                {
                    AnsiConsole.MarkupLine("[red]password must be at least 8 characters long.[/]");
                    Thread.Sleep(3000);
                    continue;
                }
                string confirmPassword = Security.ReadPasswordWithToggleSpectre(false);
                if (password != confirmPassword)
                {
                    AnsiConsole.MarkupLine("[bold red]❌ Passwords do not match![/]");
                    Thread.Sleep(1500);
                }
                else { break; }
            }

            Director dir = new Director(fullname, socialNumber, phoneNumber, username, password);

            bool success = DirectorManager.AddDirector(dir);

            if (success)
            {
                AnsiConsole.MarkupLine("[bold green]✅ Admin registered successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold red]❌ Registration failed. Please try again.[/]");
            }
        }
        private static void ForgotPassword()
        {
            Clear();
            AnsiConsole.Write(
                new Panel("[bold underline yellow]🔑 Forgot Password[/]")
                .RoundedBorder()
                .BorderColor(Spectre.Console.Color.Yellow));

            string username = AnsiConsole.Ask<string>("Username : ");
            if(checkback(username)) Run();

            string socialNumber = AnsiConsole.Ask<string>("Social Number : ");
            if (checkback(socialNumber)) Run();

            string phoneNumber = AnsiConsole.Ask<string>("Phone Number : ");
            if( checkback(phoneNumber)) Run();

            bool valid = DirectorManager.ResetPassword(username, socialNumber, phoneNumber);

            if (!valid)
            {
                AnsiConsole.MarkupLine("[bold red]❌ Information does not match our records.[/]");
                Thread.Sleep(300);
                ForgotPassword();
                return;
            }

            SetNewPassword(username);
        }
        private static void SetNewPassword(string username)
        {
            AnsiConsole.MarkupLine("[bold underline]Set New Password[/]");
            string newPassword = "";
            string repeatPassword = "";
            while (true)
            {
               newPassword = Security.ReadPasswordWithToggleSpectre();
                if (checkback(newPassword)) { ForgotPassword(); break; }
                if (newPassword.Length < 8)
                {
                    AnsiConsole.MarkupLine("[bold red]❌ Password must be at least 8 characters long.[/]");
                    Thread.Sleep(3000);
                    SetNewPassword(username);
                    continue;
                }
                else { break; }
                
            }
            while(true)
            {
                repeatPassword = Security.ReadPasswordWithToggleSpectre(false);
                if (checkback(repeatPassword)) { ForgotPassword(); break; }
                if (newPassword != repeatPassword)
                {
                    AnsiConsole.MarkupLine("[bold red]❌ Passwords do not match![/]");
                    Thread.Sleep(1500);
                    SetNewPassword(username);
                    continue ;
                }
                else
                {  break; }
            }
            bool setPass = DirectorManager.GetNewpassword(username, newPassword);

            if (setPass)
            {
                AnsiConsole.MarkupLine("[bold green]✅ Password reset successfully![/]");
                Thread.Sleep(1500);
                Run();
            }
            else
            {
                AnsiConsole.MarkupLine("[bold red]❌ Failed to reset password. Please try again.[/]");
                Thread.Sleep(1500);
                ForgotPassword();
            }
        }
        public static bool checkback(string arg)
        {
            return (arg.ToLower() == "back") ? true : false;
        }
        public static void ShowGuide()
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("green"))
                .Start("Loading user guide...", ctx =>
                {
                    Thread.Sleep(1500);
                    AnsiConsole.Clear();

                    AnsiConsole.Write(
                        new FigletText("Welcome!")
                            .Centered()
                            .Color(Spectre.Console.Color.Yellow));

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold underline green]📘 User Guide[/]");
                    AnsiConsole.WriteLine();

                    var panel = new Panel(@"
                    [bold yellow]🔹 Basic Navigation:[/]
                    - In any form or menu, type [bold]back[/] to return to the previous menu.
                    - Use arrow keys (↑ ↓) or numbered options to navigate menus.

                    [bold yellow]🔹 Entering Data:[/]
                    - Provide accurate and valid information as requested.
                    - For example:
                        - Dormitory Name: [italic]Green Valley[/]
                        - Capacity: [italic]30[/]
                        - Student ID: [italic]9912345678[/]

                    [bold yellow]🔹 Canceling Actions:[/]
                    - If supported, type [bold]cancel[/] or [bold]leave[/] to abort an operation.

                    [bold yellow]🔹 Other Tips:[/]
                    - Always double-check your inputs.
                    - If an error occurs, read the messages carefully to correct it.

                    [italic underline grey]Need help? Contact system administrator.[/]
                    ")
                    {
                        Border = BoxBorder.Double,
                        Padding = new Padding(1, 1),
                        BorderStyle = new Style(foreground: Spectre.Console.Color.Aqua),
                        Header = new PanelHeader("[bold blue]Dormitory System Guide[/]", Justify.Center)
                    };

                    AnsiConsole.Write(panel);
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]Press any key to return to the main menu...[/]");
                    ReadKey();
                });
        }
        public static void mainMenu()
        {
            while (true)
            {
                    Clear();

                var panel = new Panel("[bold yellow]🏠 Main Menu[/]")
                    .RoundedBorder()
                    .BorderStyle(Style.Parse("yellow"))
                    .Padding(1, 1, 1, 1);

                AnsiConsole.Write(panel);

                var choices = new[]
                {
            "[cyan]1.[/] 🏢 Dormitories Management",
            "[cyan]2.[/] 🏗️ Blocks Management",
            "[cyan]3.[/] 🧑 People Management",
            "[cyan]4.[/] 🛠️ Properties Management",
            "[cyan]5.[/] 📊 Reporting",
            "[cyan]6.[/] 🚪Exit"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices));

                switch (choice)
                {
                    case var s when s.Contains("Dormitories"):
                        dormitorymngmnt();
                        break;
                    case var s when s.Contains("Blocks"):
                        blockmngmnt();
                        break;
                    case var s when s.Contains("People"):
                        peoplemngmnt();
                        break;
                    case var s when s.Contains("Properties"):
                        equipmentmngmnt();
                        break;
                    case var s when s.Contains("Reporting"):
                        reporting();
                        break;
                    case var s when s.Contains("Exit"):
                        return;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void dormitorymngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🏠 Dormitory Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. ➕ Add new Dormitory",
            "2. ❌ Remove Dormitory",
            "3. ✏️ Edit Dormitory informations",
            "4. 📋 Show All Dormitories",
            "5. 🔙 Back to main menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Add"):
                        Program.GetDormitoryInfo();
                        break;
                    case var s when s.Contains("Remove"):
                        Program.RemoveDormitory();
                        break;
                    case var s when s.Contains("Edit"):
                        Program.UpdateDormitoryInfo();
                        break;
                    case var s when s.Contains("Show"):
                        Program.ShowAllDormitory();
                        break;
                    case var s when s.Contains("Back"):
                        mainMenu();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void blockmngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🏢 Block Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. ➕ Add new Block",
            "2. ❌ Remove Block",
            "3. ✏️ Edit Block informations",
            "4. 📋 Show All Blocks",
            "5. 🔙 Back to main menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Add"):
                        Program.GetBlockInfo();
                        break;
                    case var s when s.Contains("Remove"):
                        Program.RemoveBlock();
                        break;
                    case var s when s.Contains("Edit"):
                        Program.UpdateBlockInfo();
                        break;
                    case var s when s.Contains("Show"):
                        Program.ShowAllBlocks();
                        break;
                    case var s when s.Contains("Back"):
                        mainMenu();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void peoplemngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🧑 People Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🏠 Manage Dormitory supervisors",
            "2. 🏢 Manage Block supervisors",
            "3. 🎓 Manage Students",
            "4. 🔙 Back to main menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Dormitory"):
                        sueprvisormngmnt();
                        break;
                    case var s when s.Contains("Block"):
                        blocksupervisormngmnt();
                        break;
                    case var s when s.Contains("Students"):
                        studentMngmnt();
                        break;
                    case var s when s.Contains("Back"):
                        mainMenu();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void equipmentmngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🛠️ Equipment Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🆕 Register new equipment",
            "2. 🏠 Assign equipment to Rooms",
            "3. 🎓 Assign equipment to Students",
            "4. 🔄 Exchange equipment between rooms",
            "5. ✏️ Change student's equipment",
            "6. 🛠️ Maintenance management",
            "7. 🔙 Back to main menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Register"):
                        Program.RegisterNewEquipment();
                        break;
                    case var s when s.Contains("Rooms"):
                        Program.AssignEquipmentToRoom();
                        break;
                    case var s when s.Contains("Students"):
                        Program.AssignEquipmentToStudent();
                        break;
                    case var s when s.Contains("Exchange"):
                        Program.ExchangeEquipmentBetweenRooms();
                        break;
                    case var s when s.Contains("Change"):
                        Program.ChangeStudentEquipment();
                        break;
                    case var s when s.Contains("Maintenance"):
                        maintenancemngmnt();
                        break;
                    case var s when s.Contains("Back"):
                        mainMenu();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void sueprvisormngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]👨‍💼 Supervisor Management Menu [/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🆕 Register New Dormitory supervisor",
            "2. ❌ Remove Existing Dormitory supervisor",
            "3. ✏️ Edit Dormitory Supervisor informations",
            "4. 📋 Show All Dormitory Supervisors",
            "5. 🔙 Back to previous menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Register"):
                        Program.GetSuperVisorInfo();
                        break;
                    case var s when s.Contains("Remove"):
                        Program.RemoveSupervisor();
                        break;
                    case var s when s.Contains("Edit"):
                        Program.UpdateSupervisor();
                        break;
                    case var s when s.Contains("Show"):
                        Program.ShowAllSupreVisor();
                        break;
                    case var s when s.Contains("Back"):
                        peoplemngmnt();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void blocksupervisormngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]👔 Block Supervisor Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🆕 Register New Block supervisor",
            "2. ❌ Remove Existing Block supervisor",
            "3. ✏️ Edit Block Supervisor informations",
            "4. 📋 Show All Block Supervisors",
            "5. 🔙 Back to previous menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Register"):
                        Program.AddBlockSupervisor();
                        break;
                    case var s when s.Contains("Remove"):
                        Program.RemoveBlockSupervisor();
                        break;
                    case var s when s.Contains("Edit"):
                        Program.UpdateBlockSupervisor();
                        break;
                    case var s when s.Contains("Show"):
                        Program.ShowBlockSupervisor();
                        break;
                    case var s when s.Contains("Back"):
                        peoplemngmnt();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void studentMngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🎓 Student Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🆕 Register New Student",
            "2. ❌ Remove Existing Student",
            "3. ✏️ Edit Student informations",
            "4. 🔍 Searching options",
            "5. 📋 Show complete student details",
            "6. 🏠 Register student in dormitory",
            "7. 🚚 Move Student to another accommodation",
            "8. 🔙 Back to previous menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Register New Student"):
                        Program.GetStudentInfo();
                        break;
                    case var s when s.Contains("Remove Existing Student"):
                        Program.RemoveStudent();
                        break;
                    case var s when s.Contains("Edit Student"):
                        Program.UpdateStudentInfo();
                        break;
                    case var s when s.Contains("Searching"):
                        searches();
                        break;
                    case var s when s.Contains("Show complete"):
                        Program.ShowStudentWithData();
                        break;
                    case var s when s.Contains("Register student in dormitory"):
                        Program.GetStudentPlace();
                        break;
                    case var s when s.Contains("Move Student"):
                        Program.ChangeStudentPlace();
                        break;
                    case var s when s.Contains("Back"):
                        peoplemngmnt();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void maintenancemngmnt()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🛠️ Maintenance Management Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🛠️ Request Repair of Equipment",
            "2. 🔍 Check Status of Equipment Under Repairing",
            "3. ⚠️ Set Equipment Condition as Broken",
            "4. 🔙 Back to Previous Menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Request Repair"):
                        Program.RequestRepair();
                        break;
                    case var s when s.Contains("Check Status"):
                        Program.CheckRepairStatus();
                        break;
                    case var s when s.Contains("Set Equipment"):
                        Program.SetEquipmentConditionAsBroken();
                        break;
                    case var s when s.Contains("Back"):
                        equipmentmngmnt();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void reporting()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]📊 Reporting Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🏠 Accommodation status report",
            "2. 🏢 Asset report",
            "3. 📈 Advance report",
            "4. 🔙 Back to main menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Accommodation"):
                        AccommodationStatusReport();
                        break;
                    case var s when s.Contains("Asset"):
                        AssetReport();
                        break;
                    case var s when s.Contains("Advance"):
                        SpecializedReports();
                        break;
                    case var s when s.Contains("Back"):
                        mainMenu();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void AccommodationStatusReport()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🏠 Accommodation Status Report Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 📊 Overall student accommodation statistics",
            "2. 🛏️ List of empty and full rooms",
            "3. 📉 The remaining capacity of each dormitory and block",
            "4. 🔙 Back to reporting menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Overall"):
                        Program.showStudentAccommodation();
                        break;
                    case var s when s.Contains("empty and full"):
                        Program.ShowFullAndEmptyRoom();
                        break;
                    case var s when s.Contains("remaining capacity"):
                        Program.showRemainingCapacity();
                        break;
                    case var s when s.Contains("Back"):
                        reporting();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void AssetReport()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🏢 Asset Report Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 📋 The full list of Asset",
            "2. 🏠 The Asset allocated to each room",
            "3. 🎓 The Asset assigned to each student",
            "4. ⚠️ Defective Asset and in repairing",
            "5. 🔙 Back to reporting menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("full list"):
                        Program.ShowAllEquipment();
                        break;
                    case var s when s.Contains("allocated to each room"):
                        Program.showAllEquipmentAssignedToEachRoom();
                        break;
                    case var s when s.Contains("assigned to each student"):
                        Program.showAllEquipmentAssignedToEachStudent();
                        break;
                    case var s when s.Contains("Defective"):
                        Program.ShowAllDefectiveAndInrepairEquipment();
                        break;
                    case var s when s.Contains("Back"):
                        reporting();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void SpecializedReports()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]📑 Specialized Reports Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🛠️ Report repair requests",
            "2. 📜 Report of student history of accommodation",
            "3. 🔙 Back to reporting menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("repair requests"):
                        Program.showAllRepairRequests();
                        break;
                    case var s when s.Contains("student history"):
                        Program.showHistoryOfAccommodation();
                        break;
                    case var s when s.Contains("Back"):
                        reporting();
                        break;
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void searches()
        {
            while (true)
            {
                Clear();

                AnsiConsole.Write(
                    new Panel("[bold yellow]🎓 Student Management Search Menu[/]")
                        .RoundedBorder()
                        .BorderColor(Spectre.Console.Color.Yellow)
                        .Padding(1, 1, 1, 1)
                );

                var choices = new[]
                {
            "1. 🔎 Simple Search for a Student",
            "2. 🔍 Advanced Search for a Student",
            "8. 🔙 Back to Previous Menu"
        };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Please select an option:[/]")
                        .PageSize(10)
                        .AddChoices(choices)
                );

                switch (choice)
                {
                    case var s when s.Contains("Simple Search"):
                        Program.SimpleSerachStudent();
                        break;
                    case var s when s.Contains("Advanced Search"):
                        Program.AdvanceStudentSearch();
                        break;
                    case var s when s.Contains("Back"):
                        peoplemngmnt();
                        return; 
                }

                AnsiConsole.MarkupLine("\n[bold blue]Press [yellow]ENTER[/] to continue...[/]");
                ReadLine();
            }
        }

    }
    internal static class Program
    {
        public static int MAX_ROOM_CAPACITY = 6;
        public static DatabaseManager db;
        //student
        public static void GetStudentInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📋 Register New Student[/]");
                string SocialNumber;
                string PhoneNumber;
                string FullName = AnsiConsole.Ask<string>("Full Name: ");
                if (ENUserInterFace.checkback(FullName))
                    ENUserInterFace.studentMngmnt();

                while (true)
                {
                    SocialNumber = AnsiConsole.Ask<string>("Social Number: ");

                    if (ENUserInterFace.checkback(SocialNumber))
                    {
                        ENUserInterFace.studentMngmnt();
                        return;
                    }

                    if (!Security.ValidSocialNumber(SocialNumber))
                    {
                        AnsiConsole.MarkupLine("[red]❌ The entered Social Number is invalid. Please retry.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    bool exists = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start("Checking if student exists...", ctx =>
                        {
                            Thread.Sleep(2000);
                            exists = db.GetRecordsByField("Students", "SocialNumber", SocialNumber).Any();
                        });

                    if (exists)
                    {
                        AnsiConsole.MarkupLine("[red]⚠️ This student already exists![/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    break;
                }

                while (true)
                {
                    PhoneNumber = AnsiConsole.Ask<string>("Phone Number: ");
                    if (ENUserInterFace.checkback(PhoneNumber))
                    {
                        ENUserInterFace.studentMngmnt();
                        return;
                    }

                    if (!Security.ValidPhoneNumber(PhoneNumber))
                    {
                        AnsiConsole.MarkupLine("[red]❌ The entered Phone Number is invalid. Please retry.[/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    break;
                }
                string Address = AnsiConsole.Ask<string>("Address: ");
                if (ENUserInterFace.checkback(Address))
                    ENUserInterFace.studentMngmnt();

                string studentid = AnsiConsole.Ask<string>("Student ID: ");
                if (ENUserInterFace.checkback(studentid))
                    ENUserInterFace.studentMngmnt();
                bool done = false;
                AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start("Saving student information ...", ctx =>
                        {
                            Thread.Sleep(2000);
                            done = StudentManager.AddStudent(FullName, SocialNumber, PhoneNumber, Address, studentid);
                        });
                

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Student created successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Creating student failed. Please try again.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void GetStudentPlace()
        {
            try
            {

                var socialnumber = AnsiConsole.Ask<string>("Enter student Social Number : ");
                if (ENUserInterFace.checkback(socialnumber)) ENUserInterFace.studentMngmnt();

                var student = db.GetRecordsByField("Students", "SocialNumber", socialnumber);
                if (student.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]❌ No student found with Social Number: {socialnumber}[/]");
                    Thread.Sleep(3000);
                    GetStudentPlace();
                    return;
                }

                if (!string.IsNullOrEmpty(student[0]["DormitoryId"].ToString()) ||
                    !string.IsNullOrEmpty(student[0]["BlockId"].ToString()) ||
                    !string.IsNullOrEmpty(student[0]["RoomId"].ToString()))
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ This student is already accommodated.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                    return;
                }
                string dormitoryId;
                AnsiConsole.MarkupLine("[blue]🏢 Available Dormitories (remember ID):[/]");
                db.ShowAllrecords("Dormitories", false);
                while (true)
                {
                    dormitoryId = AnsiConsole.Ask<string>("Dormitory ID chosen: ");
                    if (ENUserInterFace.checkback(dormitoryId))
                    { 
                        ENUserInterFace.studentMngmnt();
                        return;
                    }
                    if (int.Parse(db.GetRecordsByField("Dormitories", "Id", dormitoryId)[0]["RemainingCapacity"].ToString()) <= 0)
                    {
                        AnsiConsole.MarkupLine("[bold red]❌ The dormitory dosen't have enough capacity. please choose another one.[/]");
                        Thread.Sleep(3000);
                        continue;
                    }
                    break;
                }
                AnsiConsole.MarkupLine("[blue]🏬 Blocks in selected dormitory:[/]");
                db.ShowRelatedRecords("Blocks", "DormitoryId", dormitoryId);
                string blockId;
                while (true)
                {
                    blockId = AnsiConsole.Ask<string>("Block ID chosen: ");
                    if (ENUserInterFace.checkback(blockId))
                    {
                        ENUserInterFace.studentMngmnt();
                    }
                    if (int.Parse(db.GetRecordsByField("Blocks", "Id", blockId)[0]["RemainingCapacity"].ToString()) <= 0)
                    {
                        AnsiConsole.MarkupLine("[bold red]❌ The Block dosen't have enough capacity. please choose another one.[/]");
                        Thread.Sleep(3000);
                        continue;
                    }
                    break;
                }
                string roomId = null;

                while (true)
                {
                    AnsiConsole.MarkupLine("[blue]🚪 Available Rooms in selected block:[/]");
                    db.ShowRelatedRecords("Rooms", "BlockId", int.Parse(blockId));

                    roomId = AnsiConsole.Ask<string>("Room ID chosen: ");
                    if (ENUserInterFace.checkback(roomId)) ENUserInterFace.studentMngmnt();

                    var room = db.GetRecordsByField("Rooms", "Id", roomId).FirstOrDefault();
                    if (room == null)
                    {
                        AnsiConsole.MarkupLine("[red]❌ Invalid Room ID. Try again.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    int capacity = int.Parse(room["RemainingCapacity"].ToString());
                    if (capacity > 0)
                        break;

                    AnsiConsole.MarkupLine("[yellow]⚠️ The selected room is full. Please choose another room.[/]");
                    Thread.Sleep(2000);
                }

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("Assigning student to room...", ctx =>
                    {
                        Thread.Sleep(2000);
                        StudentManager.assignStudentToAccommodation(socialnumber,
                            int.Parse(dormitoryId),
                            int.Parse(blockId),
                            int.Parse(roomId));
                    });

                AnsiConsole.MarkupLine($"[green]✅ Student {socialnumber} has been successfully accommodated.[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]❌ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void RemoveStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold yellow]🗑️ Remove Student[/]");

                while (true)
                {
                    string socialNumber = AnsiConsole.Ask<string>("Enter the [green]Social Number[/] of the student you want to remove: ");
                    if (ENUserInterFace.checkback(socialNumber))
                    {
                        ENUserInterFace.studentMngmnt();
                        return;
                    }

                    var data = db.GetRecordsByField("Students", "SocialNumber", socialNumber);

                    if (data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]❌ No student found with Social Number: {socialNumber}![/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    string fullName = data[0]["FullName"].ToString();
                    var confirm = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"A student with [cyan]{fullName}[/] was found.\nAre you sure you want to remove?")
                            .AddChoices("✅ Yes", "❌ No")
                    );

                    if (confirm == "❌ No")
                    {
                        ENUserInterFace.studentMngmnt();
                        return;
                    }

                    bool success = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .SpinnerStyle(Style.Parse("green"))
                        .Start("Removing student from database...", ctx =>
                        {
                            Thread.Sleep(1500);
                            success = StudentManager.RemoveStudent(socialNumber);
                        });

                    if (success)
                        AnsiConsole.MarkupLine("[green]✔️ Student deleted successfully![/]");
                    else
                        AnsiConsole.MarkupLine("[red]⚠️ Failed to delete student. Try again later.[/]");

                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                    return;
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]❌ An unexpected error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void UpdateStudentInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]✏️ Update Student Information[/]");
                string SocialNumber = AnsiConsole.Ask<string>("Enter Student's Social Number: ");
                if (ENUserInterFace.checkback(SocialNumber))
                {
                    ENUserInterFace.studentMngmnt();
                    return;
                }

                var studentrecord = db.GetRecordsByField("Students", "SocialNumber", SocialNumber);
                if (studentrecord == null || studentrecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]❌ No student found with the Social Number: {SocialNumber}![/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                    return;
                }

                var newPhone = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Phone Number (leave blank to keep current): ").AllowEmpty());
                if (ENUserInterFace.checkback(newPhone))
                {
                    ENUserInterFace.studentMngmnt();
                    return;
                }

                var newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Address (leave blank to keep current): ").AllowEmpty());
                if (ENUserInterFace.checkback(newAddress))
                {
                    ENUserInterFace.studentMngmnt();
                    return;
                }

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Updating student information...", ctx =>
                    {
                        Thread.Sleep(1500);
                        done = StudentManager.UpdateStudentInfoWithCurrentData(SocialNumber, newPhone, newAddress);
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Information updated successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Updating information failed. Please try again later.[/]");
                }
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void SimpleSerachStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔍 Search Student[/]");
                string socialNumberOrPhoneNumber = AnsiConsole.Ask<string>("Enter Social Number or Phone number to search: ");
                if (ENUserInterFace.checkback(socialNumberOrPhoneNumber))
                {
                    ENUserInterFace.searches();
                    return;
                }

                bool exists = false;

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Searching for student...", ctx =>
                    {
                        Thread.Sleep(1500);

                        if (Security.ValidPhoneNumber(socialNumberOrPhoneNumber))
                        {
                            exists = Program.db.GetRecordsByField("Students", "PhoneNumber", socialNumberOrPhoneNumber).Any();
                        }
                        else if (Security.ValidSocialNumber(socialNumberOrPhoneNumber))
                        {
                            exists = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumberOrPhoneNumber).Any();
                        }
                    });

                if (exists)
                {
                    AnsiConsole.MarkupLine("[green]✅ Search completed successfully! Displaying results...[/]");
                    Thread.Sleep(1000);

                    if (Security.ValidPhoneNumber(socialNumberOrPhoneNumber))
                        db.ShowRecordsByField("Students", "PhoneNumber", socialNumberOrPhoneNumber);
                    else
                        db.ShowRecordsByField("Students", "SocialNumber", socialNumberOrPhoneNumber);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]⚠️ No student found with given identifier: {socialNumberOrPhoneNumber}[/]");
                    Thread.Sleep(3000);
                }

                AnsiConsole.MarkupLine("[blue]Press ENTER to return to Student Management menu...[/]");
                Console.ReadLine();
                ENUserInterFace.searches();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.searches();
            }
        }
        public static void ChangeStudentPlace()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🏠 Change Student Accommodation[/]");
                string socialnumber = AnsiConsole.Ask<string>("Student Social Number: ");

                if (ENUserInterFace.checkback(socialnumber))
                {
                    ENUserInterFace.studentMngmnt();
                    return;
                }

                bool studentExists = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Checking student existence...", ctx =>
                    {
                        Thread.Sleep(1500);
                        studentExists = db.GetRecordsByField("Students", "SocialNumber", socialnumber).Any();
                    });

                if (!studentExists)
                {
                    AnsiConsole.MarkupLine("[red]⚠️ No student found with this social number.[/]");
                    Thread.Sleep(3000);
                    ChangeStudentPlace();
                    return;
                }

                var studentRecord = db.GetRecordsByField("Students", "SocialNumber", socialnumber)[0];
                if (string.IsNullOrWhiteSpace(studentRecord["DormitoryId"].ToString()))
                {
                    AnsiConsole.MarkupLine("[red]❌ This student has not been assigned an accommodation yet.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                    return;
                }
                Dictionary<string, object> newdata;
                while (true)
                {
                    newdata = db.ShowAccommodationStepByStepWithTable(socialnumber);
                    if (newdata["RoomId"].ToString() == studentRecord["RoomId"].ToString())
                    {
                        AnsiConsole.MarkupLine("[bold red]❌The origin & destinition room cannot be the same.");
                        Thread.Sleep(3000);
                        continue;
                    }
                    break;
                }
                var roomCapStr = db.GetRecordsByField("Rooms", "Id", newdata["RoomId"])[0]["RemainingCapacity"].ToString();
                int roomCapacity = int.TryParse(roomCapStr, out int cap) ? cap : 0;

                if (roomCapacity > 0)
                {
                    bool done = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start("Updating accommodation...", ctx =>
                        {
                            Thread.Sleep(1500);
                            done = StudentManager.ChangeDoirmiBlckRoom(socialnumber, newdata["DormitoryId"].ToString(), newdata["BlockId"].ToString(), newdata["RoomId"].ToString());
                        });

                    if (done)
                    {
                        AnsiConsole.MarkupLine("[green]✅ Student's accommodation changed successfully.[/]");
                        Thread.Sleep(3000);
                        ENUserInterFace.studentMngmnt();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Failed to change accommodation. Please try again.[/]");
                        Thread.Sleep(3000);
                        ENUserInterFace.studentMngmnt();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ The chosen room doesn't have enough capacity.[/]");
                    Thread.Sleep(3000);
                    ChangeStudentPlace();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void ShowStudentWithData()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]👤 Show Student Information[/]");
                string socialNumber = AnsiConsole.Ask<string>("Social Number: ");

                if (ENUserInterFace.checkback(socialNumber))
                {
                    ENUserInterFace.studentMngmnt();
                    return;
                }

                List<Dictionary<string, object>> student = null;
                bool exists = false;

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("🔍 Searching for student...", ctx =>
                    {
                        Thread.Sleep(1000);
                        student = db.GetRecordsByField("Students", "SocialNumber", socialNumber);
                        exists = student.Any();
                    });

                if (!exists || student == null)
                {
                    AnsiConsole.MarkupLine($"[red]⚠️ No student found with Social Number: {socialNumber}[/]");
                    Thread.Sleep(2000);
                    ShowStudentWithData();
                    return;
                }

                var studentRecord = student.First();
                int studentId = int.Parse(studentRecord["Id"].ToString());

                AnsiConsole.MarkupLine("\n[green]✅ Student found:[/]");
                db.ShowRecordsByField("Students", "SocialNumber", socialNumber);

                bool equipmentExists = false;

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("🔍 Searching for student's equipment...", ctx =>
                    {
                        Thread.Sleep(1000);
                        equipmentExists = db.GetRecordsByField("Equipment", "OwnerId", studentId).Any();
                    });

                if (equipmentExists)
                {
                    AnsiConsole.MarkupLine("\n[green]🎒 Equipment assigned to student:[/]");
                    db.ShowRelatedRecords("Equipment", "OwnerId", studentId);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]⚠️ No equipment found for student with Social Number: {socialNumber}[/]");
                }

                AnsiConsole.MarkupLine("\n[blue]Press ENTER to return to the Student Management menu...[/]");
                Console.ReadLine();
                ENUserInterFace.studentMngmnt();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An error occurred: {ex.Message}[/]");
                Thread.Sleep(2000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void AdvanceStudentSearch()
        {
            List<Dictionary<string, object>> allStudents = null;

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .Start("Loading students from database...", ctx =>
                {
                    Thread.Sleep(1500); 
                    allStudents = db.GetAllRecords("Students");
                });

            string input = "";
            ConsoleKeyInfo key;

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"[bold blue]🔎 Advanced Student Search[/] | Type to search | Press [yellow]Esc[/] to exit\n");
                AnsiConsole.Markup("[green]> [/]");
                AnsiConsole.Markup(input);

                var filtered = allStudents
                    .Where(s => s["FullName"].ToString().Contains(input, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToList();

                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                if (filtered.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]❌ No matching students found.[/]");
                }
                else
                {
                    foreach (var s in filtered)
                    {
                        string name = s["FullName"].ToString();
                        string id = s["SocialNumber"].ToString();

                        if (!string.IsNullOrEmpty(input))
                        {
                            var highlightedName = name.Replace(input, $"[yellow]{input}[/]", StringComparison.OrdinalIgnoreCase);
                            AnsiConsole.MarkupLine($"[green]✔️[/] {highlightedName} [dim]({id})[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]✔️[/] {name} [dim]({id})[/]");
                        }
                    }
                }

                key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                    break;
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                    input = input[..^1];
                else if (!char.IsControl(key.KeyChar))
                    input += key.KeyChar;
            }

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .SpinnerStyle(Style.Parse("cyan"))
                .Start("Closing search, preparing UI...", ctx =>
                {
                    Thread.Sleep(2000);
                });

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold green]✅ Search closed. Have a good day![/]");
        }
        //dormitory supervisor
        public static void GetSuperVisorInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]👨‍💼 Register New Dormitory Supervisor[/]");

                string fullName = AnsiConsole.Ask<string>("Full Name: ");
                if (ENUserInterFace.checkback(fullName))
                {
                    ENUserInterFace.sueprvisormngmnt();
                    return;
                }

                string socialNumber;
                while (true)
                {
                    socialNumber = AnsiConsole.Ask<string>("Social Number: ");
                    if (ENUserInterFace.checkback(socialNumber))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    if (!Security.ValidSocialNumber(socialNumber))
                    {
                        AnsiConsole.MarkupLine("[red]❌ The entered Social Number is invalid. Please retry.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    bool exists = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start("Checking if supervisor exists...", ctx =>
                        {
                            Thread.Sleep(1500);
                            exists = db.GetRecordsByField("DormitorySupervisors", "SocialNumber", socialNumber).Any();
                        });

                    if (exists)
                    {
                        AnsiConsole.MarkupLine("[red]⚠️ This Dormitory supervisor already exists![/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    break;
                }

                string phoneNumber;
                while (true)
                {
                    phoneNumber = AnsiConsole.Ask<string>("Phone Number: ");
                    if (ENUserInterFace.checkback(phoneNumber))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    if (!Security.ValidPhoneNumber(phoneNumber))
                    {
                        AnsiConsole.MarkupLine("[red]❌ The entered Phone Number is invalid. Please retry.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    break;
                }

                string address = AnsiConsole.Ask<string>("Address: ");
                if (ENUserInterFace.checkback(address))
                {
                    ENUserInterFace.sueprvisormngmnt();
                    return;
                }

                string position;
                while (true)
                {
                    position = AnsiConsole.Ask<string>("Position: ");
                    if (ENUserInterFace.checkback(position))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    if (position.Trim().ToLower() == "student")
                    {
                        AnsiConsole.MarkupLine("[red]❌ Dormitory supervisor's role can't be 'student'. Please retry.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }
                    break;
                }

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Saving supervisor information...", ctx =>
                    {
                        Thread.Sleep(1500);
                        done = DormitorySuperVisorManager.AddSuperVisor(fullName, socialNumber, phoneNumber, address, position);
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Dormitory Supervisor created successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.sueprvisormngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Creating Dormitory Supervisor failed. Please try again.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.sueprvisormngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        public static void RemoveSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🗑 Remove Dormitory Supervisor[/]");

                while (true)
                {

                    string socialnumber = AnsiConsole.Ask<string>("Enter the social number of supervisor you want to remove: ");
                    if (ENUserInterFace.checkback(socialnumber))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }


                    var data = db.GetRecordsByField("DormitorySupervisors", "SocialNumber", socialnumber);

                    if (data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]No dormitory supervisor found with SocialNumber: {socialnumber}[/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    var answ = AnsiConsole.Ask<string>($"A dormitory supervisor with SocialNumber: {socialnumber} & Name: {data[0]["FullName"]} found. Are you sure to remove? (Y/N) ");
                    if (answ.Trim().ToLower() == "y")
                    {
                        bool done = false;

                        AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .SpinnerStyle(Style.Parse("yellow"))
                            .Start("Removing supervisor...", ctx =>
                            {
                                Thread.Sleep(1500);
                                done = DormitorySuperVisorManager.DeleteSpuervisor(socialnumber);
                            });

                        if (done)
                        {
                            AnsiConsole.MarkupLine("[green]✅ Dormitory supervisor deleted successfully.[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.sueprvisormngmnt();
                            return;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]❌ Deleting supervisor failed. Please try again.[/]");
                            Thread.Sleep(3000);
                            continue;
                        }
                    }
                    else if (answ.Trim().ToLower() == "n")
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Unknown command, please retry...[/]");
                        Thread.Sleep(3000);
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        public static void UpdateSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]✏️ Update Dormitory Supervisor Information[/]");
                while (true)
                {
                    string socialNumber = AnsiConsole.Ask<string>("Enter Dormitory Supervisor's Social Number: ");
                    if (ENUserInterFace.checkback(socialNumber))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    var supervisorRecord = db.GetRecordsByField("DormitorySupervisors", "SocialNumber", socialNumber);
                    if (supervisorRecord == null || supervisorRecord.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]No dormitory supervisor found with Social Number: {socialNumber}[/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    string newPhone = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Phone Number (leave blank if no change): ").AllowEmpty());
                    if (ENUserInterFace.checkback(newPhone))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    string newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Address (leave blank if no change): ").AllowEmpty());
                    if (ENUserInterFace.checkback(newAddress))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    string newPosition = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Position (leave blank if no change): ").AllowEmpty());
                    if (ENUserInterFace.checkback(newPosition))
                    {
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }

                    bool done = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start("Updating information...", ctx =>
                        {
                            Thread.Sleep(1000);
                            done = DormitorySuperVisorManager.UpdateSupervisor(socialNumber, newPhone, newAddress, newPosition);
                        });

                    if (done)
                    {
                        AnsiConsole.MarkupLine("[green]✅ Information updated successfully.[/]");
                        Thread.Sleep(3000);
                        ENUserInterFace.sueprvisormngmnt();
                        return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Updating information failed. Please try again.[/]");
                        Thread.Sleep(3000);
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        public static void ShowAllSupreVisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📋 Show all Dormitory Supervisors[/]");
                DormitorySuperVisorManager.ShowAllSupervisors();

                AnsiConsole.MarkupLine("[blue]Press ENTER to return to Supervisor Management Menu...[/]");
                ReadLine();

                ENUserInterFace.sueprvisormngmnt();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        //dormitoryBlockSupervisor
        public static void AddBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📋 Register New Block Supervisor[/]");
                string FullName = AnsiConsole.Ask<string>("Full Name: ");
                if (ENUserInterFace.checkback(FullName))
                    ENUserInterFace.blocksupervisormngmnt();

                string SocialNumber;
                while (true)
                {
                    SocialNumber = AnsiConsole.Ask<string>("Social Number: ");
                    if (ENUserInterFace.checkback(SocialNumber))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }
                    if (!Security.ValidSocialNumber(SocialNumber))
                    {
                        AnsiConsole.MarkupLine("[red]❌ The entered Social Number is invalid. Please retry.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }
                    bool exists = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start("Checking if block supervisor exists...", ctx =>
                        {
                            Thread.Sleep(1500);
                            exists = db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", SocialNumber).Any();
                        });
                    if (exists)
                    {
                        AnsiConsole.MarkupLine("[red]⚠️ This Dormitory Block Supervisor already exists![/]");
                        Thread.Sleep(2000);
                        continue;
                    }
                    break;
                }

                string PhoneNumber;
                while (true)
                {
                    PhoneNumber = AnsiConsole.Ask<string>("Phone Number: ");
                    if (ENUserInterFace.checkback(PhoneNumber))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }
                    if (!Security.ValidPhoneNumber(PhoneNumber))
                    {
                        AnsiConsole.MarkupLine("[red]❌ The entered Phone Number is invalid. Please retry.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }
                    break;
                }

                string Address = AnsiConsole.Ask<string>("Address: ");
                if (ENUserInterFace.checkback(Address))
                    ENUserInterFace.blocksupervisormngmnt();

                string role;
                while (true)
                {
                    role = AnsiConsole.Ask<string>("Role (Can be Student): ");
                    if (ENUserInterFace.checkback(role))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }

                    if (role.ToLower() == "student" && db.GetRecordsByField("Students", "SocialNumber", SocialNumber).Count() > 0)
                    {
                        var student = db.GetRecordsByField("Students", "SocialNumber", SocialNumber)[0];
                        if (FullName != student["FullName"].ToString() ||
                            PhoneNumber != student["PhoneNumber"].ToString() ||
                            Address != student["Address"].ToString())
                        {
                            AnsiConsole.MarkupLine("[red]❌ Entered informations do not match student's info. Please retry.[/]");
                            Thread.Sleep(3000);
                            continue;
                        }
                    }
                    break;
                }

                string result = null;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Saving Block Supervisor information...", ctx =>
                    {
                        Thread.Sleep(2000);
                        result = DormitoryBlockSupervisorManager.AddBlockSupervisor(FullName, SocialNumber, PhoneNumber, Address, role);
                    });

                if (result == "success")
                {
                    AnsiConsole.MarkupLine("[green]✅ Dormitory Block Supervisor created successfully.[/]"); Thread.Sleep(3000);
                    ENUserInterFace.blocksupervisormngmnt();
                }
                else if (result == "Student Exists But Role Is Not For a Student")
                {
                    AnsiConsole.MarkupLine("[red]❌ there is a student with entered social number . please enter other info correct .[/]");
                    Thread.Sleep(3000);
                    AddBlockSupervisor();
                }
                else if (result == "studentdosenotexist")
                {
                    AnsiConsole.MarkupLine("[red]❌ Student with this Social Number not found![/]");
                    Thread.Sleep(3000);
                    AddBlockSupervisor();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Creating Dormitory Block Supervisor failed, please try again.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blocksupervisormngmnt();
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        public static void RemoveBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🗑 Remove Dormitory Block Supervisor[/]");

                while (true)
                {
                    string socialNumber = AnsiConsole.Ask<string>("Enter the social number of supervisor you want to remove: ");

                    if (ENUserInterFace.checkback(socialNumber))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }

                    var data = db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", socialNumber);

                    if (data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]No Dormitory Block Supervisor found with SocialNumber: {socialNumber}![/]");
                        Thread.Sleep(2500);
                        continue;
                    }

                    string fullName = data[0].ContainsKey("FullName") ? data[0]["FullName"].ToString() : "Unknown";
                    

                    AnsiConsole.MarkupLine($"\n[bold green]Supervisor found:[/]\n  [blue]Name:[/] {fullName}");

                    string answ = AnsiConsole.Ask<string>("Are you sure to remove this supervisor? (Y/N): ").Trim().ToLower();

                    if (answ == "y")
                    {
                        bool deleted = false;

                        AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .SpinnerStyle(Style.Parse("yellow"))
                            .Start("Removing supervisor...", ctx =>
                            {
                                Thread.Sleep(1500);
                                deleted = DormitoryBlockSupervisorManager.RemoveDormitoryBlockSupervisor(socialNumber);
                            });

                        if (deleted)
                            AnsiConsole.MarkupLine("[green]✅ Dormitory Block Supervisor deleted successfully.[/]");
                        else
                            AnsiConsole.MarkupLine("[red]❌ Failed to delete. Please try again.[/]");

                        Thread.Sleep(2500);
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }
                    else if (answ == "n")
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❗️ Invalid input. Please enter 'Y' or 'N'.[/]");
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        public static void UpdateBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]✏️ Updating Dormitory Block Supervisor Information[/]");
                while (true)
                {
                    string socialNumber = AnsiConsole.Ask<string>("Enter Dormitory Block Supervisor's Social Number: ");
                    if (ENUserInterFace.checkback(socialNumber))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }

                    var supervisorRecord = db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", socialNumber);
                    if (supervisorRecord == null || supervisorRecord.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]❌ No dormitory Block Supervisor found with Social Number: {socialNumber}[/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    string newPhone = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Phone Number (leave blank to keep current): ").AllowEmpty());
                    if (ENUserInterFace.checkback(newPhone))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }

                    string newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Address (leave blank to keep current): ").AllowEmpty());
                    if (ENUserInterFace.checkback(newAddress))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }

                    string newRole = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Role (leave blank to keep current): ").AllowEmpty());
                    if (ENUserInterFace.checkback(newRole))
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                        return;
                    }

                    bool done = AnsiConsole.Status()
                        .Start("Updating supervisor information...", ctx =>
                        {
                            Thread.Sleep(1500);
                            return DormitoryBlockSupervisorManager.UpdateDormitoryBlockSupervisor(socialNumber, newPhone, newAddress, newRole);
                        });

                    if (done)
                        AnsiConsole.MarkupLine("[green]✅ Information updated successfully.[/]");
                    else
                        AnsiConsole.MarkupLine("[red]❌ Failed to update information. Please try again later.[/]");
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
            }
            finally
            {
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        public static void ShowBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📋 Show All Dormitory Block Supervisors[/]");
                DormitoryBlockSupervisorManager.ShowAllBlockSupervisors();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        //asset
        public static void RegisterNewEquipment()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🏨 Dormitories (remember ID)[/]");
                db.ShowAllrecords("Dormitories", false);

                AnsiConsole.MarkupLine("[bold blue]🏢 Blocks (remember ID)[/]");
                db.ShowAllrecords("Blocks", false);

                AnsiConsole.MarkupLine("[bold yellow]🆕 Registering a New Equipment[/]");

                string type = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[blue]🔧 Select the type of equipment to register:[/]")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Use arrow keys to scroll)[/]")
                        .AddChoices(new[] {
                    "🧊 Fridge",
                    "🪑 Desk",
                    "💺 Chair",
                    "🛏️ Bed",
                    "🔒 Locker"
                        }));

                string condition = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[blue]🔍 Select the condition of equipment:[/]")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Use arrow keys to scroll)[/]")
                        .AddChoices(new[] {
                    "✅ Intact",
                    "❌ Broken",
                    "🔧 Repairing"
                        }));

                string blockidStr = AnsiConsole.Ask<string>("🏢 Enter ID of the Block the equipment belongs to: ");
                if (ENUserInterFace.checkback(blockidStr)) ENUserInterFace.equipmentmngmnt();

                if (!int.TryParse(blockidStr, out int blockid))
                {
                    AnsiConsole.MarkupLine("[red]❗️ Invalid Block ID. Please enter a valid number.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                    return;
                }

                string dormidStr = AnsiConsole.Ask<string>("🏨 Enter ID of the Dormitory the equipment belongs to: ");
                if (ENUserInterFace.checkback(dormidStr)) ENUserInterFace.equipmentmngmnt();

                if (!int.TryParse(dormidStr, out int dormid))
                {
                    AnsiConsole.MarkupLine("[red]❗️ Invalid Dormitory ID. Please enter a valid number.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                    return;
                }

                string cond = Condition.Intact.ToString();
                if (condition.Contains("Broken")) cond = Condition.Broken.ToString();
                else if (condition.Contains("Repairing")) cond = Condition.Reparing.ToString();

                string cleanedType = type.Substring(type.IndexOf(' ') + 1);

                Equipment NewEquipment = new Equipment(cleanedType, cond, blockid, dormid);

                AnsiConsole.Status()
                    .Start("📦 [green]Registering equipment...[/]", ctx =>
                    {
                        Thread.Sleep(1000);
                        bool done = EquipmentManager.addEquipmentToDB(NewEquipment);

                        if (done)
                        {
                            ctx.Status("✅ [bold green]Equipment added successfully![/]");
                            ctx.Spinner(Spinner.Known.Star);
                            Thread.Sleep(1000);
                            AnsiConsole.MarkupLine("\n[bold green]🎉 Equipment was successfully registered.[/]");
                        }
                        else
                        {
                            ctx.Status("❌ [bold red]Failed to add equipment.[/]");
                            ctx.Spinner(Spinner.Known.Dots2);
                            Thread.Sleep(1000);
                            AnsiConsole.MarkupLine("\n[red]⚠️ Adding equipment failed, please try again.[/]");
                        }
                    });

                Thread.Sleep(2000);
                ENUserInterFace.equipmentmngmnt();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An unexpected error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.equipmentmngmnt();
            }
        }
        public static void AssignEquipmentToRoom()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🛋️ Add the Desired Equipment to Room[/]");
                AnsiConsole.MarkupLine("[bold blue]🏠 Rooms (remember ID)[/]");
                db.ShowAllrecords("Rooms", false);

                string roomidStr = AnsiConsole.Ask<string>("🏷️ Chosen Room ID: ");
                if (ENUserInterFace.checkback(roomidStr)) ENUserInterFace.equipmentmngmnt();

                if (!int.TryParse(roomidStr, out int roomid))
                {
                    AnsiConsole.MarkupLine("[red]❗️ Invalid Room ID. Please enter a valid number.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                    return;
                }
                db.ShowAllrecords("Equipment",false);
                string propertynumber = AnsiConsole.Ask<string>("📦 Property Number of Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.equipmentmngmnt();

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("🔄 Assigning equipment to room...", ctx =>
                    {
                        Thread.Sleep(1000);
                        EquipmentManager.assignEquipmentToRoom(propertynumber, roomid.ToString());
                    });

                AnsiConsole.MarkupLine("[green]✅ Successfully Assigned Equipment To Room.[/]");
                Thread.Sleep(3000);
            }
            catch (ArgumentException e)
            {
                AnsiConsole.MarkupLine($"[red]❌ Assigning Equipment Failed: {e.Message}[/]");
                Thread.Sleep(3000);
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An unexpected error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
            }
            finally
            {
                ENUserInterFace.equipmentmngmnt();
            }
        }
        public static void AssignEquipmentToStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Add the Desired Equipment to Student's Equipment[/]");
                string socialid = AnsiConsole.Ask<string>("Enter Student's Social ID: ");
                if (ENUserInterFace.checkback(socialid)) ENUserInterFace.equipmentmngmnt();

                if (string.IsNullOrWhiteSpace(socialid))
                {
                    AnsiConsole.MarkupLine("[red]Social ID cannot be empty.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                    return;
                }

                string propertynumber = AnsiConsole.Ask<string>("Property Number of Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.equipmentmngmnt();

                if (string.IsNullOrWhiteSpace(propertynumber))
                {
                    AnsiConsole.MarkupLine("[red]Property Number cannot be empty.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                    return;
                }

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Assigning equipment to student...", ctx =>
                    {
                        Thread.Sleep(1500);
                        EquipmentManager.assignEquipmentToStudent(propertynumber, socialid);
                        done = true;
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Equipment Assigned Successfully.[/]");
                    Thread.Sleep(3000);
                }
            }
            catch (ArgumentException e)
            {
                AnsiConsole.MarkupLine($"[red]Assigning Equipment Failed: {e.Message}[/]");
                Thread.Sleep(3000);
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An unexpected error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
            }
            finally
            {
                ENUserInterFace.equipmentmngmnt();
            }
        }
        public static void ExchangeEquipmentBetweenRooms()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔄 Move the Desired Equipment to Another Room[/]");
                AnsiConsole.MarkupLine("[blue]Rooms (remember ID)[/]");
                db.ShowAllrecords("Rooms", false);

                string roomid = AnsiConsole.Ask<string>("Enter Destination Room's ID: ");
                if (ENUserInterFace.checkback(roomid)) ENUserInterFace.equipmentmngmnt();

                string propertynumber = AnsiConsole.Ask<string>("Property Number of Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.equipmentmngmnt();

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Moving equipment... ⌛️", ctx =>
                    {
                        Thread.Sleep(1500);
                        EquipmentManager.exchangeEquipmentBetweenRooms(propertynumber, roomid);
                        done = true;
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Equipment Moved Successfully.[/]");
                }
            }
            catch (ArgumentException e)
            {
                AnsiConsole.MarkupLine($"[red]🚫 Moving Equipment Failed: {e.Message}[/]");
                Thread.Sleep(3000);
            }
            finally
            {
                ENUserInterFace.equipmentmngmnt();
            }
        }
        public static void ChangeStudentEquipment()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔄 Change Student's Equipment[/]");

                string socialid = AnsiConsole.Ask<string>("Enter Student's Social ID: ");
                if (ENUserInterFace.checkback(socialid)) ENUserInterFace.equipmentmngmnt();

                string oldpropertynumber = AnsiConsole.Ask<string>("Property Number of The Current Equipment: ");
                if (ENUserInterFace.checkback(oldpropertynumber)) ENUserInterFace.equipmentmngmnt();

                string newpropertynumber = AnsiConsole.Ask<string>("Property Number of Equipment to Replace With The Current Equipment: ");
                if (ENUserInterFace.checkback(newpropertynumber)) ENUserInterFace.equipmentmngmnt();

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Processing equipment change... ⏳", ctx =>
                    {
                        Thread.Sleep(1500);
                        EquipmentManager.changeStudentEquipment(oldpropertynumber, newpropertynumber, socialid);
                        done = true;
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Student's Equipment Changed Successfully.[/]");
                }
            }
            catch (ArgumentException e)
            {
                AnsiConsole.MarkupLine($"[red]🚫 Changing Equipment Failed: {e.Message}[/]");
                Thread.Sleep(3000);
            }
            finally
            {
                ENUserInterFace.equipmentmngmnt();
            }
        }
        public static void RequestRepair()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🛠️ Request Repair of an Equipment[/]");
                string propertynumber = AnsiConsole.Ask<string>("Enter Property Number of Broken Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.maintenancemngmnt();

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Registering repair request... 🔧", ctx =>
                    {
                        Thread.Sleep(1500);
                        EquipmentManager.registerRepairRequest(propertynumber);
                        done = true;
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Request for Repair of Equipment Registered Successfully.[/]");
                }
            }
            catch (ArgumentException e)
            {
                AnsiConsole.MarkupLine($"[red]🚫 Registering Repair Request Failed: {e.Message}[/]");
            }
            finally
            {
                Thread.Sleep(3000);
                ENUserInterFace.maintenancemngmnt();
            }
        }
        public static void SetEquipmentConditionAsBroken()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔧 Changing Condition of Equipment to Broken[/]");
                string propertynumber = AnsiConsole.Ask<string>("Enter Property Number of Broken Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.maintenancemngmnt();

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Updating equipment condition... 🛠️", ctx =>
                    {
                        Thread.Sleep(1500);
                        EquipmentManager.changeEquipmentCondition(propertynumber, Condition.Broken);
                        done = true;
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Equipment's Condition Successfully Changed to Broken.[/]");
                }
            }
            catch (ArgumentException e)
            {
                AnsiConsole.MarkupLine($"[red]🚫 Changing Condition Failed: {e.Message}[/]");
            }
            finally
            {
                Thread.Sleep(3000);
                ENUserInterFace.maintenancemngmnt();
            }
        }
        public static void CheckRepairStatus()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔍 Checking Repair Request of an Equipment[/]");
                string propertynumber = AnsiConsole.Ask<string>("Enter Property Number of The Equipment Being Repaired: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.maintenancemngmnt();

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("Fetching repair request status... 🔧", ctx =>
                    {
                        Thread.Sleep(1500);
                        Program.db.ShowRecordsByField("RepairRequests", "PropertyNumber", propertynumber);
                    });

                AnsiConsole.MarkupLine("[green]✅ Repair status displayed successfully.[/]");
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]🚫 Checking status of repairing equipment failed, please try again.[/]");
            }
            finally
            {
                Thread.Sleep(3000);
                ENUserInterFace.maintenancemngmnt();
            }
        }
        //dormitory
        public static void GetDormitoryInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🏠 Register New Dormitory[/]");
                string fullName = AnsiConsole.Ask<string>("Name of Dormitory: ");
                if (ENUserInterFace.checkback(fullName))
                    ENUserInterFace.mainMenu();

                string address = AnsiConsole.Ask<string>("Address of Dormitory: ");
                if (ENUserInterFace.checkback(address))
                    ENUserInterFace.mainMenu();

                string capacityStr;
                int capacity;
                while (true)
                {
                    capacityStr = AnsiConsole.Ask<string>("Capacity of Dormitory (must be divisible by 6): ");
                    if (ENUserInterFace.checkback(capacityStr))
                        ENUserInterFace.mainMenu();

                    if (!int.TryParse(capacityStr, out capacity))
                    {
                        AnsiConsole.MarkupLine("[red]❌ Invalid number. Please enter a valid integer.[/]");
                        Thread.Sleep(2500);
                        continue;
                    }

                    if (capacity % 6 != 0)
                    {
                        AnsiConsole.MarkupLine("[red]❌ Dormitory capacity must be divisible by 6, as each room accommodates 6 students.[/]");
                        Thread.Sleep(3000);
                        continue;
                    }
                    break;
                }

                string responsible;
                while (true)
                {
                    responsible = AnsiConsole.Ask<string>("Social Number of Responsible Supervisor: ");
                    if (ENUserInterFace.checkback(responsible))
                        ENUserInterFace.mainMenu();

                    var supervisors = db.GetRecordsByField("DormitorySupervisors", "SocialNumber", responsible);
                    if (supervisors.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]❌ Supervisor not found. Please enter a valid social number.[/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    if (supervisors[0]["DormitoryId"].ToString() != "")
                    {
                        AnsiConsole.MarkupLine("[red]The entered supervisors is assigned to another dormitory ,please try another one ... [/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    break;
                }

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("Creating dormitory...", ctx =>
                    {
                        Thread.Sleep(1500);
                        done = DormitoryManager.AddDormitory(fullName, address, capacity, responsible);
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Dormitory created successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Creating dormitory failed, please try again.[/]");
                }

                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        public static void RemoveDormitory()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]🗑️ Remove Dormitory[/]");

                while (true)
                {
                    string name = AnsiConsole.Ask<string>("🏢 [green]Enter Dormitory Name[/]: ");
                    if (ENUserInterFace.checkback(name))
                    {
                        ENUserInterFace.dormitorymngmnt();
                        return;
                    }

                    var data = db.GetRecordsByField("Dormitories", "Name", name);

                    if (data.Count > 0)
                    {
                        var dormId = data[0]["Id"];
                        AnsiConsole.MarkupLine($"[yellow]Dormitory Found:[/] Name: [aqua]{name}[/], ID: [aqua]{dormId}[/]");

                        var confirmation = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Are you sure you want to remove this dormitory?")
                                .AddChoices(new[] { "Yes", "No", "Cancel" }));

                        if (confirmation == "Yes")
                        {
                            var result = AnsiConsole.Status()
                                .Start("Removing dormitory...", ctx =>
                                {
                                    Thread.Sleep(1000);
                                    return DormitoryManager.RemoveDormitory(name);
                                });

                            if (result)
                            {
                                AnsiConsole.MarkupLine("[green]✅ Dormitory deleted successfully.[/]");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[red]❌ Failed to delete dormitory. Please try again.[/]");
                            }

                            Thread.Sleep(2500);
                            ENUserInterFace.dormitorymngmnt();
                            return;
                        }
                        else if (confirmation == "No")
                        {
                            continue;
                        }
                        else
                        {
                            ENUserInterFace.dormitorymngmnt();
                            return;
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]❌ No dormitory found with name: {name}[/]");
                        bool retry = AnsiConsole.Confirm("🔁 Try again?");
                        if (!retry)
                        {
                            ENUserInterFace.dormitorymngmnt();
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred. Returning to menu...[/]");
                Thread.Sleep(2500);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        public static void UpdateDormitoryInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]✏️ Update Dormitory Information[/]");

                string name = AnsiConsole.Ask<string>("Enter Dormitory Name: ");
                if (ENUserInterFace.checkback(name))
                {
                    ENUserInterFace.dormitorymngmnt();
                    return;
                }

                var dormitoryRecord = Program.db.GetRecordsByField("Dormitories", "Name", name);
                if (dormitoryRecord == null || dormitoryRecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]❌ No dormitory found with the name: {name}![/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.dormitorymngmnt();
                    return;
                }

                var record = dormitoryRecord[0];
                AnsiConsole.Write(new Panel($"[bold yellow]📄 Current Info for [green]{name}[/][/]\n\n" +
                                            $"🏠 Address: [white]{record["Address"]}[/]\n" +
                                            $"👥 Capacity: [white]{record["Capacity"]}[/]\n" +
                                            $"🧑 Responsible (SSN): [white]{record["Responsible"]}[/]")
                                    .BorderColor(Spectre.Console.Color.Cyan1)
                                    .RoundedBorder()
                                    .Padding(1, 1));

                string newCapacity = AnsiConsole.Prompt(new TextPrompt<string>(
                        "Enter new [blue]capacity[/] ([grey]leave empty to keep current[/]):")
                        .AllowEmpty());

                if (ENUserInterFace.checkback(newCapacity))
                {
                    ENUserInterFace.dormitorymngmnt();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(newCapacity) && !int.TryParse(newCapacity, out int parsedCapacity))
                {
                    AnsiConsole.MarkupLine("[red]❌ Invalid capacity. Please enter a numeric value.[/]");
                    Thread.Sleep(2000);
                    UpdateDormitoryInfo();
                    return;
                }

                string newAddress = AnsiConsole.Prompt(new TextPrompt<string>(
                        "Enter new [blue]address[/] ([grey]leave empty to keep current[/]):")
                        .AllowEmpty());

                if (ENUserInterFace.checkback(newAddress))
                {
                    ENUserInterFace.dormitorymngmnt();
                    return;
                }

                string newResponsible = AnsiConsole.Prompt(new TextPrompt<string>(
                        "Enter new [blue]responsible's social number[/] ([grey]leave empty to keep current[/]):")
                        .AllowEmpty());

                if (ENUserInterFace.checkback(newResponsible))
                {
                    ENUserInterFace.dormitorymngmnt();
                    return;
                }

                bool done = false;
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("Updating dormitory information...", ctx =>
                    {
                        Thread.Sleep(1500);
                        done = DormitoryManager.UpdateDormitoryInfoWithCurrentData(name, newCapacity, newAddress, newResponsible);
                    });

                if (done)
                {
                    AnsiConsole.MarkupLine("[green]✅ Information updated successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Updating failed. Please try again later.[/]");
                }

                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An unexpected error occurred, Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        public static void ShowAllDormitory()
        {
            try
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[bold blue]🏨 Show All Dormitories[/]");
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("Loading Dormitories from database...", ctx =>
                    {

                        Thread.Sleep(1500);
                    });

                DormitoryManager.ShowAlldormitories();

                AnsiConsole.MarkupLine("\n[bold green]✅ Blocks loaded successfully.[/]");
                Thread.Sleep(3000);
                ReadKey();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]❌ An error occurred, Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        //block
        public static void GetBlockInfo()
        {
            try
            {
                AnsiConsole.Status()
                    .Start("Loading Dormitories...", ctx =>
                    {
                        Thread.Sleep(2000);
                    });
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[bold blue]:house: Dormitories (Remember the [bold yellow]ID[/])[/]");
                db.ShowAllrecords("Dormitories", false);

                AnsiConsole.MarkupLine("\n[bold green]Let's register a new block![/]");

                string dormitoryId = AnsiConsole.Ask<string>("[bold]🔑 Enter Dormitory ID[/]: ");
                if (ENUserInterFace.checkback(dormitoryId))
                    ENUserInterFace.blockmngmnt();

                var dormitory = db.GetRecordsByField("Dormitories", "Id", dormitoryId).FirstOrDefault();
                if (dormitory == null)
                {
                    AnsiConsole.MarkupLine("[red]Dormitory not found. Returning to menu...[/]");
                    Thread.Sleep(2000);
                    ENUserInterFace.blockmngmnt();
                    return;
                }

                int dormCapacity = int.Parse(dormitory["RemainingCapacity"].ToString());

                string fullName = AnsiConsole.Ask<string>("[bold]📋 Enter Name of Block[/]: ");
                if (ENUserInterFace.checkback(fullName))
                    ENUserInterFace.blockmngmnt();

                string floor = AnsiConsole.Ask<string>("[bold]🏙️ Enter Number of Floors[/]: ");
                if (ENUserInterFace.checkback(floor))
                    ENUserInterFace.blockmngmnt();

                string room;
                int totalCapacity;

                while (true)
                {
                    room = AnsiConsole.Ask<string>($"[bold]🚪 Enter Number of Rooms[/]: (Maximum = {float.Parse(dormCapacity.ToString()) / 6}) ");
                    if (ENUserInterFace.checkback(room))
                        ENUserInterFace.blockmngmnt();

                    totalCapacity = int.Parse(room) * 6;

                    if (totalCapacity > dormCapacity)
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠️ Block capacity exceeds dormitory capacity. Please try again.[/]");
                        Thread.Sleep(2000);
                        continue;
                    }

                    break;
                }

                string responsible;
                while (true)
                {
                    responsible = AnsiConsole.Ask<string>(":bust_in_silhouette: [bold]Enter Social Number of Responsible[/]: ");
                    if (ENUserInterFace.checkback(responsible))
                        ENUserInterFace.blockmngmnt();

                    if (db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", responsible).Count > 0)
                        break;
                    if(db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", responsible)[0]["BlockId"].ToString() != "")
                    {
                        AnsiConsole.MarkupLine("[red]The entered Block supervisors is assigned to another dormitory ,please try another one ... [/]");
                        Thread.Sleep(3000);
                        continue;
                    }    
                    AnsiConsole.MarkupLine("[red]❌ Responsible not found or assigned elsewhere. Please try again.[/]");
                    Thread.Sleep(2000);
                }

                bool success = false;
                AnsiConsole.Status()
                    .Start("Registering new block...", ctx =>
                    {
                        success = BlocksManager.AddBlock(dormitoryId, fullName, floor, room, responsible, totalCapacity);
                        Thread.Sleep(1000); 
                    });

                AnsiConsole.Clear();  

                if (success)
                {
                    AnsiConsole.MarkupLine("[bold green]✅ Block created successfully![/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blockmngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Failed to create block. Returning to menu...[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.dormitorymngmnt();
                }

            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]❌ An error occurred, Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }
        public static void RemoveBlock()
        {
            try
            {
                while (true)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[bold blue]🗑️ Remove Block[/]");

                    string name = AnsiConsole.Ask<string>("Enter Block Name: ");
                    if (ENUserInterFace.checkback(name))
                    {
                        ENUserInterFace.blockmngmnt();
                        return;
                    }

                    var data = db.GetRecordsByField("Blocks", "Name", name);
                    if (data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]❌ No block found with name: {name}![/]");
                        Thread.Sleep(2500);
                        continue;
                    }

                    var blockId = data[0]["Id"];
                    string promptMsg = $"A block with Name: [green]{name}[/] & ID: [yellow]{blockId}[/] found.\nAre you sure to remove it? ([green]Y[/]/[red]N[/]) : ";
                    var answ = AnsiConsole.Ask<string>(promptMsg);

                    if (answ.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        bool done = false;
                        AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .SpinnerStyle(Style.Parse("green"))
                            .Start("Removing block...", ctx =>
                            {
                                Thread.Sleep(1500);
                                done = BlocksManager.RemoveBlock(name);
                            });

                        if (done)
                        {
                            AnsiConsole.MarkupLine("[bold green]✅ Block deleted successfully.[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.blockmngmnt();
                            return;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]❌ Failed to delete block. Returning to menu...[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.blockmngmnt();
                            return;
                        }
                    }
                    else if (answ.Equals("n", StringComparison.OrdinalIgnoreCase))
                    {
                        ENUserInterFace.blockmngmnt();
                        return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]⚠️ Unknown command, please try again...[/]");
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An error occurred, Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }
        public static void UpdateBlockInfo()
        {
            try
            {
                while (true)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[bold blue]📝 Update Block Information[/]");

                    string name = AnsiConsole.Ask<string>("Enter Block Name: ");
                    if (ENUserInterFace.checkback(name))
                    {
                        ENUserInterFace.blockmngmnt();
                        return;
                    }

                    var blockRecord = Program.db.GetRecordsByField("Blocks", "Name", name);
                    if (blockRecord == null || blockRecord.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]❌ No block found with name: {name}![/]");
                        Thread.Sleep(3000);
                        continue;
                    }

                    var record = blockRecord[0];
                    AnsiConsole.Write(new Panel(
                        $"[bold yellow]📄 Current Info for [green]{name}[/][/]\n\n" +
                        $"🏢 Floors: [white]{record["Floor"]}[/]\n" +
                        $"🚪 Rooms: [white]{record["Rooms"]}[/]\n" +
                        $"🧑 Responsible (SSN): [white]{record["Responsible"]}[/]")
                        .BorderColor(Spectre.Console.Color.Cyan1)
                        .RoundedBorder()
                        .Padding(1, 1));

                    string newFloor = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter new [blue]Number of floor(s)[/] (leave empty to keep current):")
                        .AllowEmpty());

                    if (ENUserInterFace.checkback(newFloor))
                    {
                        ENUserInterFace.blockmngmnt();
                        return;
                    }

                    string newRoom = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter new [blue]Number of room(s)[/] (leave empty to keep current):")
                        .AllowEmpty());

                    if (ENUserInterFace.checkback(newRoom))
                    {
                        ENUserInterFace.blockmngmnt();
                        return;
                    }

                    string newResponsible = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter new [blue]responsible's social number[/] (leave empty to keep current):")
                        .AllowEmpty());

                    if (ENUserInterFace.checkback(newResponsible))
                    {
                        ENUserInterFace.blockmngmnt();
                        return;
                    }

                    bool done = false;
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots2)
                        .SpinnerStyle(Style.Parse("green"))
                        .Start("Updating block information...", ctx =>
                        {
                            Thread.Sleep(1500);
                            done = BlocksManager.UpdateBlockInfoWithCurrentData(name, newFloor, newRoom, newResponsible);
                        });

                    if (done)
                    {
                        AnsiConsole.MarkupLine("[green]✅ Information updated successfully.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Updating failed. Please try again later.[/]");
                    }

                    Thread.Sleep(3000);
                    ENUserInterFace.blockmngmnt();
                    return;
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An error occurred, Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }
        public static void ShowAllBlocks()
        {
            try
            {
                Clear();
                AnsiConsole.MarkupLine("[bold blue]🏢 Show All Blocks[/]");

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("Loading blocks from database...", ctx =>
                    {
 
                        Thread.Sleep(1500);
                    });

                BlocksManager.ShowAllblocks();

                AnsiConsole.MarkupLine("\n[bold green]✅ Blocks loaded successfully.[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An error occurred: {ex.Message}[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }
        //reports
        public static void showAllEquipmentAssignedToEachRoom()
        {
            try
            {
                AnsiConsole.MarkupLine("[underline blue]🔍 Show all equipment assigned to each room[/]\n");

                List<Dictionary<string, object>> allDorms = Program.db.GetAllRecords("Dormitories");
                if (allDorms.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]⚠️ No dormitories found.[/]");
                    return;
                }

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Dormitory");
                table.AddColumn("Block");
                table.AddColumn("Room");
                table.AddColumn("Equipment Type");
                table.AddColumn("Part Number");

                bool hasEquipment = false;

                foreach (var dorm in allDorms)
                {
                    var dormId = Convert.ToInt32(dorm["Id"]);
                    var dormName = dorm["Name"].ToString();

                    var dormBlocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", dormId);
                    foreach (var block in dormBlocks)
                    {
                        var blockId = Convert.ToInt32(block["Id"]);
                        var blockName = block["Name"].ToString();

                        var blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", blockId);
                        foreach (var room in blockRooms)
                        {
                            var roomId = Convert.ToInt32(room["Id"]);
                            var roomName = room["Id" +
                                "" +
                                "" +
                                "" +
                                ""].ToString();

                            var equipmentList = Program.db.GetRecordsByField("Equipment", "RoomId", roomId);
                            if (equipmentList.Count == 0)
                            {
                                table.AddRow(dormName, blockName, roomName, "[grey]No equipment[/]", "-");
                                continue;
                            }

                            hasEquipment = true;

                            foreach (var equip in equipmentList)
                            {
                                table.AddRow(
                                    dormName,
                                    blockName,
                                    roomName,
                                    equip["Type"].ToString(),
                                    equip["PartNumber"].ToString()
                                );
                            }
                        }
                    }
                }

                AnsiConsole.Write(table);

                if (!hasEquipment)
                {
                    AnsiConsole.MarkupLine("\n[bold yellow]⚠️ No equipment found in any room.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[bold red]❌ An error occurred while showing equipment. Returning to reporting menu...[/]");
                AnsiConsole.MarkupLine($"[grey]{ex.Message}[/]");
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void showAllEquipmentAssignedToEachStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Showing all equipment assigned to each student...[/]");

                var allDorms = db.GetAllRecords("Dormitories");
                if (allDorms.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No records found in Dormitories table[/]");
                    return;
                }

                var table = new Table()
                    .Centered()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold blue]Dormitory[/]")
                    .AddColumn("[bold yellow]Block[/]")
                    .AddColumn("[bold green]Room[/]")
                    .AddColumn("[bold cyan]Student Name[/]")
                    .AddColumn("[bold magenta]Equipment Type[/]")
                    .AddColumn("[bold gray]Part Number[/]");

                bool foundAny = false;

                foreach (var dorm in allDorms)
                {
                    var dormBlocks = db.GetRecordsByField("Blocks", "DormitoryId", Convert.ToInt32(dorm["Id"]));
                    foreach (var block in dormBlocks)
                    {
                        var blockRooms = db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
                        foreach (var room in blockRooms)
                        {
                            var students = db.GetRecordsByField("Students", "RoomId", Convert.ToInt32(room["Id"]));
                            foreach (var student in students)
                            {
                                var equipment = db.GetRecordsByField("Equipment", "OwnerId", Convert.ToInt32(student["Id"]));
                                if (equipment.Count == 0)
                                    continue;

                                foreach (var equip in equipment)
                                {
                                    table.AddRow(
                                        dorm["Name"].ToString(),
                                        block["Name"].ToString(),
                                        room["Id"].ToString(),
                                        student["FullName"].ToString(),
                                        equip["Type"].ToString(),
                                        equip["PartNumber"].ToString()
                                    );
                                    foundAny = true;
                                }
                            }
                        }
                    }
                }

                if (foundAny)
                    AnsiConsole.Write(table);
                else
                    AnsiConsole.MarkupLine("[yellow]No equipment assigned to any student.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllDefectiveAndInrepairEquipment()
        {
            AnsiConsole.MarkupLine("[blue]🔧 Checking defective and repairing equipment...[/]");

            var broken = Program.db.GetRecordsByField("Equipment", "Condition", "Broken");
            var repairing = Program.db.GetRecordsByField("Equipment", "Condition", "Reparing");

            if (broken.Count == 0 && repairing.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]✅ No defective or repairing equipment found.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold underline red]Equipment Status[/]")
                .AddColumn("[bold]🔩 Type[/]")
                .AddColumn("[bold]🆔 Part Number[/]")
                .AddColumn("[bold]📦 Property Number[/]")
                .AddColumn("[bold]⚙️ Condition[/]");

            foreach (var item in broken)
            {
                table.AddRow(
                    $"[red]{item["Type"]}[/]",
                    item["PartNumber"].ToString(),
                    item["PropertyNumber"].ToString(),
                    "[red]❌ Broken[/]"
                );
            }

            foreach (var item in repairing)
            {
                table.AddRow(
                    $"[yellow]{item["Type"]}[/]",
                    item["PartNumber"].ToString(),
                    item["PropertyNumber"].ToString(),
                    "[yellow]🛠️ Repairing[/]"
                );
            }

            AnsiConsole.Write(table);
        }
        public static void showStudentAccommodation()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📋 Showing student accommodations[/]");

                List<Dictionary<string, object>> allDorms = db.GetAllRecords("Dormitories");
                if (allDorms.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ No dormitories found.[/]");
                    return;
                }

                Table table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[bold]Student Name[/]");
                table.AddColumn("[bold]Student ID[/]");
                table.AddColumn("[bold]Room[/]");
                table.AddColumn("[bold]Block[/]");
                table.AddColumn("[bold]Dormitory[/]");

                foreach (var dorm in allDorms)
                {
                    string dormName = dorm["Name"].ToString();
                    int dormId = Convert.ToInt32(dorm["Id"]);
                    var dormBlocks = db.GetRecordsByField("Blocks", "DormitoryId", dormId);

                    if (dormBlocks.Count == 0)
                        continue;

                    foreach (var block in dormBlocks)
                    {
                        string blockName = block["Name"].ToString();
                        int blockId = Convert.ToInt32(block["Id"]);
                        var blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", blockId);

                        if (blockRooms.Count == 0)
                            continue;

                        foreach (var room in blockRooms)
                        {
                            int roomId = Convert.ToInt32(room["Id"]);
                            var roomStudents = Program.db.GetRecordsByField("Students", "RoomId", roomId);

                            if (roomStudents.Count == 0)
                                continue;

                            foreach (var student in roomStudents)
                            {
                                string studentName = student["FullName"].ToString();
                                string studentId = student["StudentID"].ToString();

                                table.AddRow(
                                    $"[green]{studentName}[/]",
                                    $"[bold]{studentId}[/]",
                                    $"[cyan]{roomId}[/]",
                                    $"[cyan]{blockName}[/]",
                                    $"[cyan]{dormName}[/]"
                                );
                            }
                        }
                    }
                }

                if (table.Rows.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ No student accommodations found.[/]");
                }
                else
                {
                    AnsiConsole.Write(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ An error occurred: {ex.Message}[/]");
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowFullAndEmptyRoom()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]🔍 Checking room status...[/]");

                List<Dictionary<string, object>> allRooms = Program.db.GetAllRecords("Rooms");
                if (allRooms.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]❌ No rooms found in the database.[/]");
                    return;
                }

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold underline green]Room Occupancy Status[/]")
                    .AddColumn("[bold]🏠 Room Id[/]")
                    .AddColumn("[bold]🏢 Block ID[/]")
                    .AddColumn("[bold]👥 Capacity[/]")
                    .AddColumn("[bold]🧍‍ Occupied[/]")
                    .AddColumn("[bold]📊 Status[/]");

                foreach (var room in allRooms)
                {
                    int roomId = Convert.ToInt32(room["Id"]);
                    int capacity = Convert.ToInt32(room["RemainingCapacity"]);
                    var students = Program.db.GetRecordsByField("Students", "RoomId", roomId);
                    int count = students.Count;

                    string status = count >= capacity
                        ? "[green]✅ Full[/]"
                        : "[yellow]🟡 Empty[/]";

                    table.AddRow(
                        $"[aqua]{roomId}[/]",
                        room["BlockId"].ToString(),
                        capacity.ToString(),
                        count.ToString(),
                        status
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                AnsiConsole.MarkupLine("[red]⚠️ An error occurred while loading room data.[/]");
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void showRemainingCapacity()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📊 Showing remaining capacity[/]");

                List<Dictionary<string, object>> allDormitory = Program.db.GetAllRecords("Dormitories");
                if (allDormitory.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]❌ No records found in table Dormitories[/]");
                }
                else
                {
                    var dormTable = new Table().Centered();
                    dormTable.Border(TableBorder.Rounded);
                    dormTable.AddColumn("[bold yellow]🏢 Dormitory[/]");
                    dormTable.AddColumn("[bold green]📶 Remaining Capacity[/]");

                    foreach (Dictionary<string, object> dormitory in allDormitory)
                    {
                        dormTable.AddRow(dormitory["Name"].ToString(), dormitory["RemainingCapacity"].ToString());
                    }

                    AnsiConsole.Write(dormTable);
                }

                List<Dictionary<string, object>> allBlocks = Program.db.GetAllRecords("Blocks");
                if (allBlocks.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]❌ No records found in table Blocks[/]");
                    return;
                }

                var blockTable = new Table().Centered();
                blockTable.Border(TableBorder.Rounded);
                blockTable.AddColumn("[bold yellow]🏗 Block[/]");
                blockTable.AddColumn("[bold green]📶 Remaining Capacity[/]");

                foreach (Dictionary<string, object> block in allBlocks)
                {
                    int totalCapacity = Convert.ToInt32(block["NumberOfRooms"]) * 6;
                    List<Dictionary<string, object>> Rooms = db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
                    foreach (Dictionary<string, object> room in Rooms)
                    {
                        int filled = 6 - Convert.ToInt32(room["RemainingCapacity"]);
                        totalCapacity -= filled;
                    }

                    blockTable.AddRow(block["Name"].ToString(), totalCapacity.ToString());
                }

                AnsiConsole.Write(blockTable);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ An error occurred: {ex.Message}[/]");
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipment()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]📋Show all equipment[/]");
                db.ShowAllrecords("Equipment");
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void showAllRepairRequests()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔧 Show all repair requests[/]");

                List<Dictionary<string, object>> allRepairRequests = Program.db.GetAllRecords("RepairRequests");
                if (allRepairRequests.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]⚠️ No records found in table RepairRequests[/]");
                    return;
                }

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("[bold yellow]🆔 Request ID[/]");
                table.AddColumn("[bold cyan]🛠️ Type[/]");
                table.AddColumn("[bold magenta]📇 Property Number[/]");
                table.AddColumn("[bold green]🚪 Room[/]");
                table.AddColumn("[bold green]🏢 Block[/]");
                table.AddColumn("[bold blue]👨‍🎓 Student Name[/]");
                table.AddColumn("[bold blue]🎓 Student ID[/]");

                foreach (var request in allRepairRequests)
                {
                    var equipment = db.GetRecordsByField("Equipment", "PropertyNumber", Convert.ToInt32(request["PropertyNumber"]))[0];
                    var room = db.GetRecordsByField("Rooms", "Id", Convert.ToInt32(equipment["RoomId"]))[0];
                    var block = db.GetRecordsByField("Blocks", "Id", Convert.ToInt32(room["BlockId"]))[0];

                    string studentName = "—";
                    string studentID = "—";

                    if (equipment["OwnerId"] != DBNull.Value)
                    {
                        var student = db.GetRecordsByField("Students", "Id", Convert.ToInt32(equipment["OwnerId"]))[0];
                        studentName = student["FullName"].ToString();
                        studentID = student["StudentID"].ToString();
                    }

                    table.AddRow(
                        $"[yellow]{request["Id"]}[/]",
                        $"[cyan]{equipment["Type"]}[/]",
                        $"[magenta]{request["PropertyNumber"]}[/]",
                        $"[green]{room["RoomNumber"]}[/]",
                        $"[green]{block["Name"]}[/]",
                        $"[blue]{studentName}[/]",
                        $"[blue]{studentID}[/]"
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void showHistoryOfAccommodation()
        {
            try
            {
                Clear();
                AnsiConsole.MarkupLine("[bold blue]📋 Show Students accommdation history[/]");

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green"))
                    .Start("Loading inforamtions from database...", ctx =>
                    {

                        Thread.Sleep(1500);
                    });

                db.ShowAllrecords("StudentAccommodationHistory");

                AnsiConsole.MarkupLine("\n[bold green]✅ History loaded successfully.[/]");
                Thread.Sleep(3000);
                ENUserInterFace.SpecializedReports();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ An error occurred. Returning to menu... [/]");
                Thread.Sleep(3000);
                ENUserInterFace.SpecializedReports();
            }
        }

        //public static void resetdatabase()
        //{
        //    using (var connection = new SqliteConnection("Data Source=MyDatabase.sqlite"))
        //    {
        //        connection.Open();

        //        var command = connection.CreateCommand();
        //        command.CommandText = @"
        //            DELETE FROM Director;
        //        ";

        //        command.ExecuteNonQuery();

        //        MessageBox.Show("اطلاعات دیتابیس با موفقیت پاک شد.", "بازنشانی", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //}
        static void Main()
        {
            OutputEncoding = Encoding.UTF8;
            db = new DatabaseManager();
            db.InitializeDatabase();
            //resetdatabase();
            ENUserInterFace.Run();
        }
    }
}