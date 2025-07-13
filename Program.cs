﻿using System;
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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Spectre.Console;
using System.Data;
using System.Xml.Linq;

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
                Capacity INTEGER NOT NULL,
                Responsible TEXT
            );";

            string blockSql = @"
            CREATE TABLE IF NOT EXISTS Blocks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                DormitoryId INTEGER,
                Responsible TEXT,
                NumberOfFloors INTEGER,
                NumberOfRooms INTEGER,
                FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id) ON DELETE SET NULL
            );";

            string roomSql = @"
            CREATE TABLE IF NOT EXISTS Rooms (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BlockId INTEGER NOT NULL,
                RoomNumber INTEGER,
                FloorNumber INTEGER,
                Capacity INTEGER,
                FOREIGN KEY (BlockId) REFERENCES Blocks(Id) ON DELETE SET NULL
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
                PartNumber TEXT,
                PropertyNumber TEXT,
                Condition TEXT,
                RoomId INTEGER,
                OwnerId INTEGER,
                FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE SET NULL,
                FOREIGN KEY (OwnerId) REFERENCES Students(Id)
            );";

            string repairrequestSql = @"
		CREATE TABLE IF NOT EXISTS RepairRequests (
		    Id INTEGER PRIMARY KEY AUTOINCREMENT,
		    PropertyNumber TEXT,
		    Status TEXT
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
            AnsiConsole.MarkupLine("[yellow]press Enter to return to main menu[/]");
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(intercept: true);
            } while (keyInfo.Key != ConsoleKey.Enter);
            ENUserInterFace.mainMenu();
        }
        public void ShowAllrecords(string tableName,bool check = true)
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
                Program.db.DeleteRecord("Students", "SocialNumber", socialNumber);
                return true;
            }
            catch (Exception)
            {
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
        public static void ChangeDoirmiBlckRoom(string socialNumber)
        {
            var studentRecord = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber);
            if (studentRecord == null || studentRecord.Count == 0)
            {
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
                return;
            }

            WriteLine("لیست خوابگاه‌ها:");
            var dormitories = Program.db.GetAllRecords("Dormitories");
            foreach (var dorm in dormitories)
            {
                if (studentRecord[0]["DormitoryId"].ToString() != dorm["Id"].ToString())
                    WriteLine($"{dorm["Id"]}: {dorm["Name"]} - {dorm["Address"]}");
                else
                    WriteLine($"-{dorm["Id"]}: {dorm["Name"]} - {dorm["Address"]}");
            }

            Write("آیدی خوابگاه انتخابی: ");
            if (!int.TryParse(ReadLine(), out int dormitoryId))
            {
                WriteLine("آیدی نامعتبر است.");
                return;
            }

            var selectedDorm = dormitories.FirstOrDefault(d => Convert.ToInt32(d["Id"]) == dormitoryId);
            if (selectedDorm == null)
            {
                WriteLine("چنین خوابگاهی وجود ندارد.");
                return;
            }

            WriteLine("\nلیست بلوک‌های خوابگاه:");
            var blocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", dormitoryId);
            foreach (var block in blocks)
            {
                if (studentRecord[0]["BlockId"].ToString() != block["Id"].ToString())
                    WriteLine($"{block["Id"]}: {block["Name"]}");
                else
                    WriteLine($"-{block["Id"]}: {block["Name"]}");
            }

            Write("آیدی بلوک انتخابی: ");
            if (!int.TryParse(ReadLine(), out int blockId))
            {
                WriteLine("آیدی نامعتبر است.");
                return;
            }

            if (!blocks.Any(b => Convert.ToInt32(b["Id"]) == blockId))
            {
                WriteLine("چنین بلوکی در این خوابگاه وجود ندارد.");
                return;
            }

            WriteLine("\nاتاق‌های دارای ظرفیت باقی‌مانده:");
            var rooms = Program.db.GetRecordsByField("Rooms", "BlockId", blockId);
            bool hasAvailableRoom = false;
            foreach (var room in rooms)
            {
                int roomId = Convert.ToInt32(room["Id"]);
                int capacity = Convert.ToInt32(room["Capacity"]);
                var studentsInRoom = Program.db.GetRecordsByField("Students", "RoomId", roomId);
                int remaining = capacity - studentsInRoom.Count;
                if (remaining > 0)
                {
                    hasAvailableRoom = true;
                    if (studentRecord[0]["RoomId"].ToString() != room["Id"].ToString())
                        WriteLine($"{room["Id"]}: اتاق {room["RoomNumber"]} - باقی‌مانده {remaining}");
                    else
                        WriteLine($"-{room["Id"]}: اتاق {room["RoomNumber"]} - باقی‌مانده {remaining}");
                }
            }

            if (!hasAvailableRoom)
            {
                WriteLine("هیچ اتاقی با ظرفیت خالی در این بلوک وجود ندارد.");
                return;
            }

            Write("آیدی اتاق انتخابی: ");
            if (!int.TryParse(ReadLine(), out int roomIdChosen))
            {
                WriteLine("آیدی نامعتبر است.");
                return;
            }

            var chosenRoom = rooms.FirstOrDefault(r => Convert.ToInt32(r["Id"]) == roomIdChosen);
            if (chosenRoom == null)
            {
                WriteLine("چنین اتاقی در این بلوک وجود ندارد.");
                return;
            }

            var newDormBlckRoom = new Dictionary<string, object>
            {
                {"FullName", studentRecord[0]["FullName"]},
                {"SocialNumber", studentRecord[0]["SocialNumber"]},
                {"PhoneNumber", studentRecord[0]["PhoneNumber"]},
                {"Address", studentRecord[0]["Address"]},
                {"StudentID", studentRecord[0]["StudentID"]},
                {"RoomId", roomIdChosen},
                {"BlockId", blockId},
                {"DormitoryId", dormitoryId}
            };

            Program.db.UpdateRecord("Students", newDormBlckRoom, "SocialNumber", socialNumber);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
    }
    public class Dormitory
    {
        public int Id { get; set; }

        public string _name;
        public string _address { get; set; }
        public string _responsible { get; set; }

        private List<string> _blocks = new List<string>();
        public int _capacity { get; set; }
        public Dormitory(string name, string address, int capcity, string responsible, string block = null)
        {
            _name = name;
            _address = address;
            _capacity = capcity;
            _responsible = responsible;
            _blocks.Add(block);
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
                { "Capacity",dormitory._capacity},
                { "Responsible",dormitory._responsible},
            };
            return info;
        }
        public static bool AddDormitory(string FullName, string Address, int capcity, string responsible)
        {
            try
            {

                var dormitory = new Dormitory(FullName, Address, capcity, responsible);

                Program.db.InsertRecord("Dormitories", DormitoryManager.ToDictionary(dormitory));
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
        private List<string> _rooms { get; set; }
        public Block(string dormitory, string name, int floor, int room, string responsible, List<string> rooms = null)
        {
            _dormitoryId = dormitory;
            _name = name;
            _NO_floors = floor;
            _NO_rooms = room;
            _responsible = responsible;
            _rooms = rooms;
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
                {"DormitoryId",block._dormitoryId},
                { "Name",block._name},
                { "NumberOfFloors",block._NO_floors},
                {"NumberOfRooms",block._NO_rooms },
                {"Responsible",block._responsible},
            };
            return info;
        }
        public static bool AddBlock(string dormiId, string name, string floor, string room, string resposible)
        {
            try
            {
                Block block = new Block(dormiId, name, int.Parse(floor), int.Parse(room), resposible);
                Program.db.InsertRecord("Blocks", BlocksManager.ToDictionary(block));
                return true;
            }
            catch (Exception)
            {
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
        public int Id { get; set; }
        private List<string> _equipment = new List<string>();
        private List<string> _students = new List<string>();
        string _block;
        int _NO_room, _NO_floors, _capacity;
        public Room(int no_room, int no_floor, int capacity, string block, List<string> equipment, List<string> students)
        {
            _NO_room = no_room;
            _NO_floors = no_floor;
            _capacity = capacity;
            _block = block;
            _equipment = equipment;
            _students = students;
        }
        public Room(int no_room, int no_floor, int capacity, string block)
        {
            // this constructor is for cases when we don't want to specify students and equipment in the room
            _NO_room = no_room;
            _NO_floors = no_floor;
            _capacity = capacity;
            _block = block;
        }

        public static Room FromDictionary(Dictionary<string, object> roomDict)
        {
            Room room = new Room(Convert.ToInt32(roomDict["RoomNumber"]), Convert.ToInt32(roomDict["FloorNumber"]), Convert.ToInt32(roomDict["Capacity"]), roomDict["BlockId"].ToString());
            room.Id = Convert.ToInt32(roomDict["Id"]);
            return room;
        }
    }
    class Equipment
    {
        public static int _ItemCounter = 0;

        public string _type;
        public string _partNumber;
        public string _propertyNumber;
        public Condition _condition;
        public int _RoomId;
        public int _BlockId;
        public int _DormId;
        public Equipment() { }
        public Equipment(string type, Condition condition, int roomid, int blockid, int dormid)
        {
            _type = type;
            _condition = condition;
            _RoomId = roomid;
            _BlockId = blockid;
            _DormId = dormid;
        }

        public Equipment(string type, Condition condition, int blockid, int dormid)
        {
            _type = type;
            _condition = condition;
            _BlockId = blockid;
            _DormId = dormid;
            _RoomId = -1;
        }

        public string partNumber
        {
            get => _partNumber;
            set
            {
                if (_type == "Fridge") _partNumber = "001";
                else if (_type == "Desk") _partNumber = "002";
                else if (_type == "Chair") _partNumber = "003";
                else if (_type == "Bed") _partNumber = "004";
                else if (_type == "Locker") _partNumber = "005";
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
            {"Type", this._type},
            {"PartNumber", this._partNumber},
            {"PropertyNumber", this._propertyNumber},
            {"Condition", this._condition},
            {"RoomId", this._RoomId}
        };

        }

        public static Equipment FromDictionary(Dictionary<string, object> equipmentDict)
        {
            Equipment equipment = new Equipment(equipmentDict["Type"].ToString(), (Condition)equipmentDict["Condition"], (int)equipmentDict["PartNumber"], (int)equipmentDict["PropertyNumber"], (int)equipmentDict["RoomId"]);
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
            catch (Exception)
            {
                return false;
            }
        }

        public static bool assignEquipmentToRoom(string propertyNumber, string RoomId)
        {
            try
            {
                Dictionary<string, object> EquipmentUpdatedValues = new Dictionary<string, object>
                    {
                    {"RoomId", RoomId}
                };
                Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool assignEquipmentToStudent(string propertyNumber, string socialNumber)
        {
            try
            {

                Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
                var StudentId = studentDict["Id"];
                var RoomId = studentDict["RoomId"];
                Dictionary<string, object> EquipmentUpdatedValues = new Dictionary<string, object> {
                    { "RoomId", RoomId },
                    { "OwnerId", StudentId }
                };
                Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool exchangeEquipmentBetweenRooms(string propertyNumber, string roomId)
        {
            try
            {
                Dictionary<string, object> ChangedRoomId = new Dictionary<string, object> {
                    { "RoomId", roomId}
                };

                Program.db.UpdateRecord("Equipment", ChangedRoomId, "PropertyNumber", propertyNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool changeStudentEquipment(string oldPropertyNumber, string newPropertyNumber, string socialNumber)
        {
            try
            {
                Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
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
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool changeEquipmentCondition(string propertyNumber, Condition condition)
        {
            try
            {
                Dictionary<string, object> UpdatedCondition = new Dictionary<string, object> {
                    { "Condition", condition.ToString()}
                };

                Program.db.UpdateRecord("Equipment", UpdatedCondition, "PropertyNumber", propertyNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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

        public static bool registerRepairRequest(string propertyNumber)
        {
            try
            {
                // uncomment the two below comments if you wish to add a field EquipmentId that is a foreign key to Id field in Equipment Table to RepairRequests table
                RepairRequest req = new RepairRequest(propertyNumber);
                //int equipmentId = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0]["Id"].ToInt32();

                Dictionary<string, object> reqDict = req.ToDictionary();
                //reqDict.Add("EquipmentId", equipmentId);
                bool done = changeEquipmentCondition(propertyNumber, Condition.Reparing);
                if (!done)
                {
                    return false;
                }

                Program.db.InsertRecord("RepairRequests", reqDict);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //public static void addEquipmentToDB(Equipment newEquipment)
        //{
        //    Dictionary<string, object> info = newEquipment.ToDictionary();

        //    Program.db.InsertRecord("Equipment", info);
        //}

        //public static void assignEquipmentToRoom(string propertyNumber, string roomId)
        //{
        //    var RoomId = Program.db.GetRecordsByField("Rooms", "RoomId", roomId)[0]["Id"]; // need implementation for Id in Room class and adding RoomId column to db
        //    Dictionary<string, object> EquipmentUpdatedValues = new Dictionary<string, object>
        //        {
        //        { "RoomId", RoomId}
        //    };
        //    Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
        //}

        //public static void assignEquipmentToStudent(string propertyNumber, string socialNumber)
        //{
        //    Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
        //    var StudentId = studentDict["Id"];
        //    var RoomId = studentDict["RoomId"];
        //    Dictionary<string, object> EquipmentUpdatedValues = new Dictionary<string, object> {
        //        { "RoomId", RoomId},
        //        { "OwnerId", StudentId}
        //    }
        //    ;
        //    Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
        //}

        //public static void exchangeEquipmentBetweenRooms(string propertyNumber, string roomId)
        //{
        //    var destinationRoomId = Program.db.GetRecordsByField("Rooms", "RoomId", roomId)[0]["Id"];

        //    Dictionary<string, object> ChangedRoomId = new Dictionary<string, object> {
        //        { "RoomId", destinationRoomId}
        //    }
        //    ;

        //    Program.db.UpdateRecord("Equipment", ChangedRoomId, "PropertyNumber", propertyNumber);
        //}

        //public static void changeStudentEquipment(string oldPropertyNumber, string newPropertyNumber, string socialNumber)
        //{
        //    Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
        //    var ownerId = studentDict["Id"];
        //    var roomId = studentDict["RoomId"];

        //    Dictionary<string, object> newEquipmentUpdatedValues = new Dictionary<string, object> {
        //        { "RoomId", roomId},
        //        { "OwnerId", ownerId}
        //    }
        //    ;

        //    Dictionary<string, object> oldEquipmentUpdatedValues = new Dictionary<string, object> {
        //        { "RoomId", DBNull.Value},
        //        { "OwnerId", DBNull.Value}
        //    }
        //    ;

        //    Program.db.UpdateRecord("Equipment", newEquipmentUpdatedValues, "PropertyNumber", newPropertyNumber);
        //    Program.db.UpdateRecord("Equipment", oldEquipmentUpdatedValues, "PropertyNumber", oldPropertyNumber);
        //}

        //public static void changeEquipmentCondition(string propertyNumber, Condition condition)
        //{
        //    Dictionary<string, object> UpdatedCondition = new Dictionary<string, object> {
        //        { "Condition", condition.ToString()}
        //    }
        //    ;

        //    Program.db.UpdateRecord("Equipment", UpdatedCondition, "PropertyNumber", propertyNumber);
        //}

        //public static Condition checkCondition(string propertyNumber)
        //{
        //    Equipment equipment = Equipment.FromDictionary(Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0]);
        //    return equipment._condition;
        //}

        //public static void showAllEquipment()
        //{
        //    List<Dictionary<string, object>> allEquipment = Program.db.GetAllRecords("Equipment");
        //    foreach (Dictionary<string, object> equipment in allEquipment)
        //    {
        //        WriteLine($"{equipment["Type"].ToString()}, property number: {equipment["PropertyNumber"].ToString()}, Condition: {equipment["Condition"].ToString()}");
        //    }
        //}

        //public static void equipmentAssignedToRoom(int RoomId)
        //{
        //    List<Dictionary<string, object>> roomEquipment = Program.db.GetRecordsByField("Equipment", "RoomId", RoomId);
        //    foreach (Dictionary<string, object> equipment in roomEquipment)
        //    {
        //        if (equipment["OwnerId"] != DBNull.Value)
        //        {
        //            Dictionary<string, object> owner = Program.db.GetRecordsByField("Students", "Id", equipment["OwnerId"])[0];
        //            WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}, Owned by: {owner["FullName"]}");
        //        }
        //        else
        //        {
        //            WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
        //        }
        //    }
        //}

        //public static void equipmentAssignedToStudent(int StudentId)
        //{
        //    List<Dictionary<string, object>> studentEquipment = Program.db.GetRecordsByField("Equipment", "OwnerId", StudentId);
        //    foreach (Dictionary<string, object> equipment in studentEquipment)
        //    {
        //        WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
        //    }
        //}

        //public static void showEquipmentWithCondition(Condition condition)
        //{
        //    List<Dictionary<string, object>> allEquipment = Program.db.GetRecordsByField("Equipment", "Condition", condition.ToString());
        //    foreach (Dictionary<string, object> equipment in allEquipment)
        //    {
        //        WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, in Room: {equipment["RoomId"]}");
        //    }
        //}

        //public static void registerRepairRequest(string propertyNumber)
        //{
        //    // uncomment the two below comments if you wish to add a field EquipmentId that is a foreign key to Id field in Equipment Table to RepairRequests table
        //    RepairRequest req = new RepairRequest(propertyNumber);
        //    //int equipmentId = Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0]["Id"].ToInt32();

        //    Dictionary<string, object> reqDict = req.ToDictionary();
        //    //reqDict.Add("EquipmentId", equipmentId);
        //    Program.db.InsertRecord("RepairRequests", reqDict);
        //}


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
        

        public DormitoryBlockSupervisor(string fullName, string socialNumber, string phoneNumber, string address, string role, string blockUnderResponsibility=null) : base(fullName, socialNumber, phoneNumber, address)
        {
            Studentid = null;
            Role = role;
            BlockUnderResponsibility = null;
        }

        public DormitoryBlockSupervisor(string fullName, string socialNumber, string phoneNumber, string address, string studentid , string role, string blockUnderResponsibility=null) : base(fullName, socialNumber, phoneNumber, address)
        {
            Studentid = studentid;
            Role = role;
            BlockUnderResponsibility = null;
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
                {"StudentId",blocksupervisor.Studentid == null ? DBNull.Value : int.Parse(blocksupervisor.Studentid)},
                {"BlockId", blocksupervisor.BlockUnderResponsibility},
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
                        return "Student Exists But SocialNumber Is Not For a Student";
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
            catch (Exception)
            {
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

    class report
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
        public static void ShowFullAndEmptyRoom()
        {
            try
            {
                Program.showFullRooms();
                Program.showEmptyRooms();
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

        public static void ShowAllDefectiveAndInrepairEquipment()
        {
            try
            {
                Program.showAllDefectiveAndInrepairEquipment();
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
        public static string ReadPasswordWithToggleSpectre()
        {
            var password = new StringBuilder();
            bool showPassword = false;
            ConsoleKeyInfo key;

            AnsiConsole.MarkupLine("[yellow]Enter your password (press [green]F2[/] to toggle visibility):[/]");

            Console.Write("Password: ");

            while (true)
            {
                key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.F2)
                {
                    showPassword = !showPassword;

                    Console.Write("\rPassword: ");
                    if (showPassword)
                        Console.Write(password.ToString());
                    else
                        Console.Write(new string('*', password.Length));
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    if (showPassword)
                        Console.Write(key.KeyChar);
                    else
                        Console.Write("*");
                }
            }

            return password.ToString();
        }
    }
    class ENUserInterFace
    {
        public static void Login()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Main Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Login Admin",
                    "2. Sign in Admin",
                    "3. Forgot Password",
                    "4. Exit"
                }));
                switch (choice)
                {
                    case "1. Login Admin":
                        adminLogin();
                        break;
                    case "2. Sign in Admin":
                        signinAdmin();
                        break;
                    case "3. Forgot Password":
                        forgotpassword();
                        break;
                    case "4. Exit":
                        return;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void adminLogin()
        {
            AnsiConsole.MarkupLine("[blue]Login Admin[/]");
            var username = AnsiConsole.Ask<string>("UserName : ");
            if (checkback(username)) Login();
            var password = Security.ReadPasswordWithToggleSpectre();
            if (checkback(password)) Login();
            bool success = DirectorManager.Login(username, password);
            if (success)
            {
                AnsiConsole.MarkupLine($"[green]{username} , Wellcome To Dormitory Mangement App[/]");
                Thread.Sleep(3000);
                mainMenu();
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Invalid Username or Password[/]");
            }
        }
        public static void signinAdmin()
        {
            AnsiConsole.MarkupLine("[blue]Register New Admin[/]");
            var fullname = AnsiConsole.Ask<string>("FullName : ");
            if (checkback(fullname)) Login();
            var socialnumber = AnsiConsole.Ask<string>("SocialNumber : ");
            if (checkback(socialnumber)) Login();
            var phonenumber = AnsiConsole.Ask<string>("PhoneNumber : ");
            if (phonenumber.ToLower() == "back") Login();
            var username = AnsiConsole.Ask<string>("UserName : ");
            if (checkback(username)) Login();
            var passwrod = Security.ReadPasswordWithToggleSpectre();
            if (checkback(passwrod)) Login();
            Director dir = new Director(fullname, socialnumber, phonenumber, username, passwrod);
            bool success = DirectorManager.AddDirector(dir);
            if (success)
            {
                AnsiConsole.MarkupLine("[green]Admin Registered Successfully.[/]");
                Thread.Sleep(3000);
                Login();
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Register Failed , please try again.[/]");
                Thread.Sleep(3000);
                signinAdmin();
            }
        }
        public static void forgotpassword()
        {
            AnsiConsole.MarkupLine("[blue]Forgot Password[/]");
            var username = AnsiConsole.Ask<string>("Username : ");
            if (checkback(username)) Login();
            var socialnumber = AnsiConsole.Ask<string>("SocialNumber : ");
            if (checkback((socialnumber))) Login();
            var phonenumber = AnsiConsole.Ask<string>("PhoneNumber : ");
            if (checkback(phonenumber)) Login();
            bool validation = DirectorManager.ResetPassword(username, socialnumber, phonenumber);
            if (validation)
            {
                setnewpassoword(username);
            }
            else
            {
                AnsiConsole.MarkupLine("[red]invlid username,socialnumber or phonen number,please try again.[/]");
                Thread.Sleep(3000);
                forgotpassword();
            }
        }
        public static void setnewpassoword(string username)
        {
            AnsiConsole.MarkupLine("[blue]Set New Password");
            var newpassword = Security.ReadPasswordWithToggleSpectre();
            if (checkback(newpassword)) forgotpassword();
            var repeatnewpassword = Security.ReadPasswordWithToggleSpectre();
            if (checkback(repeatnewpassword)) forgotpassword();
            if (newpassword.Length >= 8 && repeatnewpassword.Length >= 8)
            {
                if (newpassword == repeatnewpassword)
                {
                    bool setpass = DirectorManager.GetNewpassword(username, newpassword);
                    if (setpass)
                    {
                        AnsiConsole.MarkupLine("[green]Password has been reset.");
                        Thread.Sleep(3000);
                        Login();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]changing password Failed , please try again.[/]");
                        Thread.Sleep(3000);
                        forgotpassword();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Password do not match.[/]");
                    Thread.Sleep(3000);
                    setnewpassoword(username);
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Password's length must be at least 8 character long.[/]");
                Thread.Sleep(3000);
                setnewpassoword(username);
            }
        }
        public static bool checkback(string arg)
        {
            return (arg.ToLower() == "back") ? true : false;
        }
        public static void mainMenu()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Main Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Dormitories Manaegmenet",
                    "2. Blocks Management",
                    "3. People Management",
                    "4. Properties Management",
                    "5. Reporting"
                }));
                switch (choice)
                {
                    case "1. Dormitories Manaegmenet":
                        dormitorymngmnt();
                        break;
                    case "2. Blocks Management":
                        blockmngmnt();
                        break;
                    case "3. People Management":
                        peoplemngmnt();
                        break;
                    case "4. Properties Management":
                        equipmentmngmnt();
                        break;
                    case "5. Reporting":
                        reporting();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }

        }
        public static void dormitorymngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Dormitory Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Add new Dormitory",
                    "2. Remove Dormitory",
                    "3. Edit Dormitory informations",
                    "4. Show All Dormitorise",
                    "5. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Add new Dormitory":
                        Program.GetDormitoryInfo();
                        break;
                    case "2. Remove Dormitory":
                        Program.RemoveDormitory();
                        break;
                    case "3. Edit Dormitory informations":
                        Program.UpdateDormitoryInfo();
                        break;
                    case "4. Show All Dormitorise":
                        Program.ShowAllDormitory();
                        break;
                    case "5. Back to main menu":
                        mainMenu();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }

        public static void blockmngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Block Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Add new Block",
                    "2. Remove Block",
                    "3. Edit Block informations",
                    "4. Show All Blocks",
                    "5. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Add new Block":
                        Program.GetBlockInfo();
                        break;
                    case "2. Remove Block":
                        Program.RemoveBlock();
                        break;
                    case "3. Edit Block informations":
                        Program.UpdateBlockInfo();
                        break;
                    case "4. Show All Blocks":
                        Program.ShowAllBlocks();
                        break;
                    case "5. Back to main menu":
                        mainMenu();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }

        public static void peoplemngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]People Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Manage Dormitory supervisors",
                    "2. Manage Block supervisors",
                    "3. Managa Students",
                    "4. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Manage Dormitory supervisors":
                        sueprvisormngmnt();
                        break;
                    case "2. Manage Block supervisors":
                        blocksupervisormngmnt();
                        break;
                    case "3. Managa Students":
                        studentMngmnt();
                        break;
                    case "4. Back to main menu":
                        mainMenu();
                        break;

                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }

        public static void maintenancemngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Maintenance Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Request Repair of Equipment",
                    "2. Check Status of Equipment Under Repairing",
                    "3. Set Equipment Condition as Broken",
                    "4. Back to Previous Menu"
                }));
                switch (choice)
                {
                    case "1. Request Repair of Equipment":
                        Program.RequestRepair();
                        break;
                    case "2. Check Status of Equipment Under Repairing":
                        Program.CheckRepairStatus();
                        break;
                    case "3. Set Equipment Condition as Broken":
                        Program.SetEquipmentConditionAsBroken();
                        break;
                    case "4. Back to Previous Menu":
                        equipmentmngmnt();
                        break;

                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void equipmentmngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Equipment Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Register new equipment",
                    "2. Assign equipment to Rooms",
                    "3. Assign equipment to Students",
                    "4. Exchange equipment between rooms",
                    "5. Change student's equipment",
                    "6. Maintenance management",
                    "7. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Register new equipment":
                        Program.RegisterNewEquipment();
                        break;
                    case "2. Assign equipment to Rooms":
                        Program.AssignEquipmentToRoom();
                        break;
                    case "3. Assign equipment to Students":
                        Program.AssignEquipmentToStudent();
                        break;
                    case "4. Exchange equipment between rooms":
                        Program.ExchangeEquipmentBetweenRooms();
                        break;
                    case "5. Change student's equipment":
                        Program.ChangeStudentEquipment();
                        break;
                    case "6. Maintenance management":
                        maintenancemngmnt();
                        break;
                    case "7. Back to main menu":
                        mainMenu();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void reporting()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Reporting Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Accommodation status report",
                    "2. Asset report",
                    "3. Advance report",
                    "4. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Accommodation status report":
                        AccommodationStatusReport();
                        break;
                    case "2. Asset report":
                        AssetReport();
                        break;
                    case "3. Advance report":
                        SpecializedReports();
                        break;
                    case "4. Back to main menu":
                        mainMenu();
                        break;

                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void AccommodationStatusReport()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Accommodation status report Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Overall student accommodation statistics",
                    "2. List of empty and full rooms",
                    "3. The remaining capacity of each dormitory and block",
                    "4. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Overall student accommodation statistics":
                        Program.ShowStudentAccommodation();
                        break;
                    case "2. List of empty and full rooms":
                        Program.ShowFullAndEmptyRoom();
                        break;
                    case "3. The remaining capacity of each dormitory and block":
                        Program.ShowRemainingCapacity();
                        break;
                    case "4. Back to main menu":
                        reporting();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void AssetReport()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Asset report Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. The full list of Asset",
                    "2. The Asset allocated to each room",
                    "3. The Asset assigned to each student",
                    "4. Defective Asset and in repairing",
                    "5. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. The full list of Asset":
                        Program.ShowAllEquipment();
                        break;
                    case "2. The Asset allocated to each room":
                        Program.ShowAllEquipmentAssignedToEachRoom();
                        break;
                    case "3. The Asset assigned to each student":
                        Program.ShowAllEquipmentAssignedToEachStudent();
                        break;
                    case "4. Defective Asset and in repairing":
                        Program.ShowAllDefectiveAndInrepairEquipment();
                        break;
                    case "5. Back to main menu":
                        reporting();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }
        public static void SpecializedReports()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Specialized reports Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Report repair requests",
                    "2. Report of student history of accommodation",
                    "3. Back to main menu"
                }));
                switch (choice)
                {
                    case "1. Report repair requests":
                        Program.ShowAllRepairRequests();
                        break;
                    case "2. Report of student history of accommodation":
                        break;
                    case "3. Back to main menu":
                        reporting();
                        break;
                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }

        public static void sueprvisormngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Supervisor Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Register New Dormitory supervisor",
                    "2. Remove Existing Dormitory supervisor",
                    "3. Edit Dormitory Supervisor informatioans",
                    "4. Show All Dormitory Supervisors",
                    "5. Back to previous menu"
                }));
                switch (choice)
                {
                    case "1. Register New Dormitory supervisor":
                        Program.GetSuperVisorInfo();
                        break;
                    case "2. Remove Existing Dormitory supervisor":
                        Program.RemoveSupervisor();
                        break;
                    case "3. Edit Dormitory Supervisor informatioans":
                        Program.UpdateSupervisor();
                        break;
                    case "4. Show All Dormitory Supervisors":
                        Program.ShowAllSupreVisor();
                        break;
                    case "5. Back to previous menu":
                        peoplemngmnt();
                        break;

                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }

        public static void blocksupervisormngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Supervisor Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Register New Block supervisor",
                    "2. Remove Existing Block supervisor",
                    "3. Edit Block Supervisor informatioans",
                    "4. Show All Block Supervisors",
                    "5. Back to previous menu"
                }));
                switch (choice)
                {
                    case "1. Register New Block supervisor":
                        Program.AddBlockSupervisor();
                        break;
                    case "2. Remove Existing Block supervisor":
                        Program.RemoveBlockSupervisor();
                        break;
                    case "3. Edit Block Supervisor informatioans":
                        Program.UpdateBlockSupervisor();
                        break;
                    case "4. Show All Block Supervisors":
                        Program.ShowBlockSupervisor();
                        break;
                    case "5. Back to previous menu":
                        peoplemngmnt();
                        break;

                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
                ReadLine();
            }
        }

        public static void studentMngmnt()
        {
            while (true)
            {
                Clear();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[yellow]Supervisor Management Menu[/]").PageSize(10).AddChoices(new[]
                {
                    "1. Register New Student",
                    "2. Remove Existing Student",
                    "3. Edit Student informatioans",
                    "4. Search for a student",
                    "5. Show complete student details",
                    "6. Register student in dormitory",
                    "7. Back to previous menu"
                }));
                switch (choice)
                {
                    case "1. Register New Student":
                        Program.GetStudentInfo();
                        break;
                    case "2. Remove Existing Student":
                        Program.RemoveStudent();
                        break;
                    case "3. Edit Student informatioans":
                        Program.UpdateStudentInfo();
                        break;
                    case "4. Search for a student":
                        Program.SerachStudent();
                        break;
                    case "5. Show complete student details":
                        Program.showStudentwithdata();
                        break;
                    case "6. Register student in dormitory":
                        break;
                    case "7. Back to previous menu":
                        peoplemngmnt();
                        break;

                }
                AnsiConsole.MarkupLine("[blue]press ENTER to continue...[/]");
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
                AnsiConsole.MarkupLine("[blue]Register New Student[/]");
                string FullName = AnsiConsole.Ask<string>("FullName : ");
                if (ENUserInterFace.checkback(FullName)) ENUserInterFace.studentMngmnt();
                string SocialNumber = AnsiConsole.Ask<string>("Social Nuumber : ");
                if (ENUserInterFace.checkback(SocialNumber)) ENUserInterFace.studentMngmnt();
                string PhoneNumber = AnsiConsole.Ask<string>("Phone Number : ");
                if (ENUserInterFace.checkback(PhoneNumber)) ENUserInterFace.studentMngmnt();
                string Address = AnsiConsole.Ask<string>("Address : ");
                if (ENUserInterFace.checkback(Address)) ENUserInterFace.studentMngmnt();
                string studentid = AnsiConsole.Ask<string>("Student ID : ");
                if (ENUserInterFace.checkback(studentid)) ENUserInterFace.studentMngmnt();
                bool doen = StudentManager.AddStudent(FullName, SocialNumber, PhoneNumber, Address, studentid);
                if (doen)
                {
                    AnsiConsole.MarkupLine("[green]Student Created successflly .[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Creating student failed, please try again .[/]");
                    Thread.Sleep(40000);
                    ENUserInterFace.studentMngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void GetStudentPlace(string fullName, string socialNumber, string phoneNumber, string address, string studentId)
        {
            AnsiConsole.MarkupLine("[blue]Dormitories(remeber ID)[/]");
            db.ShowAllrecords("Dormitories", false);
            AnsiConsole.MarkupLine("[blue]Register Student in dormitory[/]");
            int dormitoryId = AnsiConsole.Ask<int>("Dormitory ID choosen : ");
            AnsiConsole.MarkupLine("[blue]Blocks(remeber ID)[/]");
            db.ShowAllrecords("Blocks", false);
            int blockid = AnsiConsole.Ask<int>("Block ID choosen : ");
            var block = db.GetRecordsByField("Blocks", "Id", blockid);
            if(block.Count() > 0)
            {
                int dormid = int.Parse(block[0]["DormitoryId"].ToString());
                if(dormid == dormitoryId)
                {

                }
            }
           
            //WriteLine("اتاق‌های دارای ظرفیت باقی‌مانده:");
            //var rooms = db.GetRecordsByField("Rooms", "BlockId", blockId);
            //foreach (var room in rooms)
            //{
            //    int RoomId = Convert.ToInt32(room["Id"]);
            //    int capacity = Convert.ToInt32(room["Capacity"]);
            //    var students = db.GetRecordsByField("Students", "RoomId", RoomId);
            //    if (students.Count < capacity)
            //    {
            //        int remaining = capacity - students.Count;
            //        WriteLine($"{room["Id"]}: اتاق {room["RoomNumber"]} - باقی‌مانده {remaining}");
            //    }
            //}

            //Write("آیدی اتاق انتخابی: ");
            //int roomId = int.Parse(ReadLine());
            //Student student = new Student(fullName, socialNumber, phoneNumber, address, studentId, roomId.ToString(), blockId.ToString(), dormitoryId.ToString());
            //return student;
        }
        public static void RemoveStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Remove Student[/]");
                string socialnumber = AnsiConsole.Ask<string>("Enter the social number of student you want to remove : ");
                if (ENUserInterFace.checkback(socialnumber)) ENUserInterFace.studentMngmnt();

                var data = db.GetRecordsByField("Students", "SocialNumber", socialnumber);
                if (data.Count > 0)
                {
                    var answ = AnsiConsole.Ask<string>($"A student with SocialNumber : {socialnumber} & Name : {data[0]["FullName"]} found , are sure to remove ? (Y/N) ");
                    if (answ.ToLower() == "y")
                    {
                        bool done = StudentManager.RemoveStudent(socialnumber);
                        if (done)
                        {
                            AnsiConsole.MarkupLine("[green]Student deleted successfully.[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.studentMngmnt();
                        }
                    }
                    else if (answ.ToLower() == "n")
                    {
                        ENUserInterFace.studentMngmnt();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]unkonwn command,please retry again... [/]");
                        Thread.Sleep(3000);
                        RemoveStudent();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]No student found with SocialNumber : {socialnumber} ![/]");
                    Thread.Sleep(3000);
                    RemoveStudent();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void UpdateStudentInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Update Student Supervisor infomations[/]");
                string SoicalNumber = AnsiConsole.Ask<string>("Enter Student's SocialNumber : ");
                if (ENUserInterFace.checkback(SoicalNumber)) ENUserInterFace.studentMngmnt();
                var studentrecord = db.GetRecordsByField("Students", "SocialNumber", SoicalNumber);
                if (studentrecord == null || studentrecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]No Student found with the SocialNumber : {SoicalNumber} ![/]");
                    Thread.Sleep(3000);
                    UpdateStudentInfo();
                }
                string newPhone = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Phone Number :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newPhone)) ENUserInterFace.studentMngmnt();
                var newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Address :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newAddress)) ENUserInterFace.studentMngmnt();
                bool done = StudentManager.UpdateStudentInfoWithCurrentData(SoicalNumber, newPhone, newAddress);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Informations updated successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Updating information failed, please try later [/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.studentMngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void SerachStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Search Student[/]");
                string socialNumberOrPhoneNumber = AnsiConsole.Ask<string>("Enter Social Number or Phone number to search : ");
                if (ENUserInterFace.checkback(socialNumberOrPhoneNumber)) ENUserInterFace.studentMngmnt();
                if (Security.ValidPhoneNumber(socialNumberOrPhoneNumber))
                {
                    var studnet= Program.db.GetRecordsByField("Students", "PhoneNumber", socialNumberOrPhoneNumber);
                    if (studnet.Count() > 0)
                    {
                        bool done = StudentManager.SerachStudent(null, socialNumberOrPhoneNumber);
                        if(done) ENUserInterFace.studentMngmnt();
                        else
                        {
                            AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.studentMngmnt();
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]No student found with phone number : {socialNumberOrPhoneNumber}");
                        Thread.Sleep(3000);
                        ENUserInterFace.studentMngmnt();          
                    }
                }
                else if(Security.ValidSocialNumber(socialNumberOrPhoneNumber))
                {
                    var studnet = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumberOrPhoneNumber);
                    if (studnet.Count() > 0)
                    {
                        bool done = StudentManager.SerachStudent(socialNumberOrPhoneNumber);
                        if (done) ENUserInterFace.studentMngmnt();
                        else
                        {
                            AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.studentMngmnt();
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]No student found with social number : {socialNumberOrPhoneNumber}");
                        Thread.Sleep(3000);
                        ENUserInterFace.studentMngmnt();
                    }

                }
            }
            catch(Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        public static void ChangeStudentPlace()
        {
            //WriteLine("کد ملی دانشجو را وارد کنید.");
            //string socialNumber = ReadLine();
            //StudentManager.ChangeDoirmiBlckRoom(socialNumber);
        }
        public static void showStudentwithdata()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show student information[/]");
                string socialnumber = AnsiConsole.Ask<string>("Social Number : ");
                if (db.GetRecordsByField("Students", "SocialNumber", socialnumber).Count() > 0)
                {
                    db.ShowRecordsByField("Students", "SocialNumber", socialnumber);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]No student found with social number : {socialnumber}[/]");
                    Thread.Sleep(3000);
                    showStudentwithdata();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.studentMngmnt();
            }
        }
        //dormitory supervisor
        public static void GetSuperVisorInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Register New dormitorySupervisors[/]");
                string FullName = AnsiConsole.Ask<string>("FullName : ");
                if (ENUserInterFace.checkback(FullName)) ENUserInterFace.sueprvisormngmnt();
                string SocialNumber = AnsiConsole.Ask<string>("Social Nuumber : ");
                if (ENUserInterFace.checkback(SocialNumber)) ENUserInterFace.sueprvisormngmnt();
                string PhoneNumber = AnsiConsole.Ask<string>("Phone Number : ");
                if (ENUserInterFace.checkback(PhoneNumber)) ENUserInterFace.sueprvisormngmnt();
                string Address = AnsiConsole.Ask<string>("Address : ");
                if (ENUserInterFace.checkback(Address)) ENUserInterFace.sueprvisormngmnt();
                string Position = AnsiConsole.Ask<string>("Position : ");
                if (ENUserInterFace.checkback(Position)) ENUserInterFace.sueprvisormngmnt();
                bool doen = DormitorySuperVisorManager.AddSuperVisor(FullName, SocialNumber, PhoneNumber, Address, Position);
                if (doen)
                {
                    AnsiConsole.MarkupLine("[green]Dormitory Supervisor Created successflly .[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.sueprvisormngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Creating dormitory Supervisor failed, please try again .[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.sueprvisormngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        public static void RemoveSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Remove Dormitory Supervisor[/]");
                string socialnumber = AnsiConsole.Ask<string>("Enter the social number of supervisor you want to remove : ");
                if (ENUserInterFace.checkback(socialnumber)) ENUserInterFace.sueprvisormngmnt();

                var data = db.GetRecordsByField("DormitorySupervisors", "SocialNumber", socialnumber);
                if (data.Count > 0)
                {
                    var answ = AnsiConsole.Ask<string>($"A dormitory Supervisor with SocialNumber : {socialnumber} & Name : {data[0]["FullName"]} found , are sure to remove ? (Y/N) ");
                    if (answ.ToLower() == "y")
                    {
                        bool done = DormitorySuperVisorManager.DeleteSpuervisor(socialnumber);
                        if (done)
                        {
                            AnsiConsole.MarkupLine("[green]Dormitory Suupervisor deleted successfully.[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.sueprvisormngmnt();
                        }
                    }
                    else if (answ.ToLower() == "n")
                    {
                        ENUserInterFace.sueprvisormngmnt();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]unkonwn command,please retry again... [/]");
                        Thread.Sleep(3000);
                        RemoveSupervisor();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]No dormitory Supervisor found with SocialNumber : {socialnumber} ![/]");
                    Thread.Sleep(3000);
                    RemoveSupervisor();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        public static void UpdateSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Update Dormitory Supervisor infomations[/]");
                string SoicalNumber = AnsiConsole.Ask<string>("Enter Dormitory Supervisor's SocialNumber : ");
                if (ENUserInterFace.checkback(SoicalNumber)) ENUserInterFace.sueprvisormngmnt();
                var suprevisorRecord = Program.db.GetRecordsByField("DormitorySupervisors", "SocialNumber", SoicalNumber);
                if (suprevisorRecord == null || suprevisorRecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]No dormitory Supervisor found with SocialNumber : {SoicalNumber} ![/]");
                    Thread.Sleep(3000);
                    UpdateSupervisor();
                }
                string newPhone = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Phone Number :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newPhone)) ENUserInterFace.sueprvisormngmnt();
                var newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new address :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newAddress)) ENUserInterFace.sueprvisormngmnt();
                string newPosition = AnsiConsole.Prompt(new TextPrompt<string>("Enter New Position :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newPosition)) ENUserInterFace.sueprvisormngmnt();
                bool done = DormitorySuperVisorManager.UpdateSupervisor(SoicalNumber, newPhone, newAddress, newPosition);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Informations updated successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.sueprvisormngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Updating information failed, please try later [/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.sueprvisormngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }

        }
        public static void ShowAllSupreVisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all Dormitory Supervisors[/]");
                DormitorySuperVisorManager.ShowAllSupervisors();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.sueprvisormngmnt();
            }
        }
        //dormitoryBlockSupervisor
        public static void AddBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Register New Block Supervisor[/]");
                string FullName = AnsiConsole.Ask<string>("Full Name : ");
                if (ENUserInterFace.checkback(FullName)) ENUserInterFace.blocksupervisormngmnt();

                string SocialNumber = AnsiConsole.Ask<string>("Social Number : ");
                if (ENUserInterFace.checkback(SocialNumber)) ENUserInterFace.blocksupervisormngmnt();

                string PhoneNumber = AnsiConsole.Ask<string>("Phone Number : ");
                if (ENUserInterFace.checkback(PhoneNumber)) ENUserInterFace.blocksupervisormngmnt();

                string Address = AnsiConsole.Ask<string>("Address : ");
                if (ENUserInterFace.checkback(Address)) ENUserInterFace.blocksupervisormngmnt();

                getrole:
                string role = AnsiConsole.Ask<string>("Role : (Can be Student) ");
                if (ENUserInterFace.checkback(role)) ENUserInterFace.blocksupervisormngmnt();

                string done = DormitoryBlockSupervisorManager.AddBlockSupervisor(
                    FullName, SocialNumber, PhoneNumber, Address, role);

                if (done == "success")
                {
                    AnsiConsole.MarkupLine("[green]Dormitory Block Supervisor Created successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blocksupervisormngmnt();
                }
                else if (done == "Student Exists But SocialNumber Is Not For a Student")
                {
                    AnsiConsole.MarkupLine("[red]Please enter role correctly.[/]");
                    Thread.Sleep(3000);
                    goto getrole;
                }
                else if (done == "studentdosenotexist")
                {
                    AnsiConsole.MarkupLine("[red]Student with this Social Number not found![/]");
                    Thread.Sleep(3000);
                    goto getrole;
                }
                else if (done == "unsucess")
                {
                    AnsiConsole.MarkupLine("[red]Creating dormitory Supervisor failed, please try again.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blocksupervisormngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        public static void RemoveBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Remove Dormitory Block Supervisor[/]");
                string socialnumber = AnsiConsole.Ask<string>("Enter the social number of supervisor you want to remove : ");
                if (ENUserInterFace.checkback(socialnumber)) ENUserInterFace.blocksupervisormngmnt();

                var data = db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", socialnumber);

                if (data.Count() > 0)
                {
                    var answ = AnsiConsole.Ask<string>($"A dormitorySupervisor with SocialNumber: {socialnumber} & Name: {data[0]["FullName"]} found. Are you sure to remove? (Y/N) ");

                    if (answ.ToLower() == "y")
                    {
                        bool done = DormitoryBlockSupervisorManager.RemoveDormitoryBlockSupervisor(socialnumber);
                        if (done)
                        {
                            AnsiConsole.MarkupLine("[green]Dormitory Block Supervisor deleted successfully.[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Failed to delete. Please try again.[/]");
                        }
                        Thread.Sleep(3000);
                        ENUserInterFace.blocksupervisormngmnt();
                    }
                    else if (answ.ToLower() == "n")
                    {
                        ENUserInterFace.blocksupervisormngmnt();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Unknown command. Please retry...[/]");
                        Thread.Sleep(3000);
                        RemoveBlockSupervisor();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]No Dormitory Block Supervisor found with SocialNumber: {socialnumber}![/]");
                    Thread.Sleep(3000);
                    RemoveBlockSupervisor();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        public static void UpdateBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Update Dormitory Block Supervisor infomations[/]");
                string SoicalNumber = AnsiConsole.Ask<string>("Enter Dormitory Block Supervisor's SocialNumber : ");
                if (ENUserInterFace.checkback(SoicalNumber)) ENUserInterFace.blocksupervisormngmnt();
                var suprevisorRecord = db.GetRecordsByField("DormitoryBlockSupervisors", "SocialNumber", SoicalNumber);
                if (suprevisorRecord == null || suprevisorRecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]No such a dormitory Block Supervisor with SocialNumber : {SoicalNumber} ![/]");
                    Thread.Sleep(3000);
                    UpdateBlockSupervisor();
                }
                string newPhone = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Phone Number :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newPhone)) ENUserInterFace.blocksupervisormngmnt();
                var newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Address :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newAddress)) ENUserInterFace.blocksupervisormngmnt();
                string newrole = AnsiConsole.Prompt(new TextPrompt<string>("Enter New Role :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newrole)) ENUserInterFace.blocksupervisormngmnt();
                bool done = DormitoryBlockSupervisorManager.UpdateDormitoryBlockSupervisor(SoicalNumber, newPhone, newAddress, newrole);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Informations updated successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blocksupervisormngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Updating information failed, please try later [/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blocksupervisormngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        public static void ShowBlockSupervisor()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all Dormitory Block Supervisors[/]");
                DormitoryBlockSupervisorManager.ShowAllBlockSupervisors();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blocksupervisormngmnt();
            }
        }
        //director
        //public static bool Login(string username, string password)
        //{
        //    return DirectorManager.Login(username, password);
        //}
        //public static bool ResetPassword(string username, string socialnumber, string phonenumber)
        //{
        //    return DirectorManager.ResetPassword(username, socialnumber, phonenumber);
        //}
        //public static string GetNewPassword(string username, string newpass)
        //{
        //    return DirectorManager.GetNewpassword(username, newpass);
        //}
        //public static bool signin(Director director)
        //{
        //    return DirectorManager.AddDirector(director);
        //}
        //equipment & room
        public static void registerNewEquipment()
        {
            Equipment newEquipment = new Equipment();
            Write("type of equipment: ");
            string type = ReadLine();
            Write("what condition is the equipment in: ");
            string condition = ReadLine();
            Write("Enter Id of block in dormitory you want this equipment in: ");
            int blockid = int.Parse(ReadLine()); // replace this with a method called chooseBlock that works exactly like chooseRoom
            Write("Enter Id of dormitory you want this equipment in: ");
            int dormid = int.Parse(ReadLine()); // replace this with a method called chooseDormitory that works exactly like chooseRoom
            if (condition.ToLower() == "intact")
            {
                newEquipment = new Equipment(type, Condition.Intact, blockid, dormid);
            }
            else if (condition.ToLower() == "broken")
            {
                newEquipment = new Equipment(type, Condition.Intact, blockid, dormid);
            }
            else if (condition.ToLower() == "repairing")
            {
                newEquipment = new Equipment(type, Condition.Reparing, blockid, dormid);
            }
            EquipmentManager.addEquipmentToDB(newEquipment);
        }
        public static Room chooseRoom()
        {
            List<Dictionary<string, object>> allRooms = Program.db.GetAllRecords("Rooms");

            WriteLine("avialable rooms: ");
            for (int i = 0; i < allRooms.Count; i++)
            {
                Dictionary<string, object> room = allRooms[i];
                WriteLine($"{i}: Room number {room["RoomNumber"]} with block ID: {room["BlockId"]}");
            }

            Write("specify a room from the above list: ");
            int roomIndex = int.Parse(ReadLine());
            Dictionary<string, object> specifiedRoomDict = allRooms[roomIndex];

            Room specifiedRoom = Room.FromDictionary(specifiedRoomDict); // implementing FromDictionary method in Room class
            return specifiedRoom;
        }
        public static Equipment chooseEquipment()
        {
            WriteLine("Specify the type of equipment you want: ");
            string type = ReadLine();
            List<Dictionary<string, object>> allEquipment = Program.db.GetRecordsByField("Equipment", "RoomId", DBNull.Value); // needs to be changed to select all equipment that are not assigned to any room and are of specified type and intact
            WriteLine("not assigned equipment: ");
            for (int i = 0; i < allEquipment.Count; i++)
            {
                Dictionary<string, object> equipment = allEquipment[i];
                WriteLine($"Equipment {i}: property number: {equipment["PropertyNumber"]}, Type: {equipment["Type"]} ");
            }
            Write("Specify an equipment from the above list: ");
            int equipmentIndex = int.Parse(ReadLine());
            Dictionary<string, object> specifiedEquipmentDict = allEquipment[equipmentIndex];

            Equipment specifiedEquipment = Equipment.FromDictionary(specifiedEquipmentDict);
            return specifiedEquipment;
        }
        public static void assignEquipmentToRoom()
        {
            Room room = chooseRoom();
            Equipment equipment = chooseEquipment();
            EquipmentManager.assignEquipmentToRoom(equipment._propertyNumber, room.Id.ToString());
        }
        public static void assignEquipmentToStudent()
        {
            Equipment equipment = chooseEquipment();
            Write("Enter Student's social number");
            string socialNumber = ReadLine();
            EquipmentManager.assignEquipmentToStudent(equipment._propertyNumber, socialNumber);
        }
        public static void exchangeEquipmentBetweenRooms()
        {
            Write("Choose Destination Room: ");
            int destinationRoomId = chooseRoom().Id;

            Write("enter property number of equipment you want to transfer to the specified room: ");
            string propertyNumber = ReadLine();

            EquipmentManager.exchangeEquipmentBetweenRooms(propertyNumber, destinationRoomId.ToString());
        }
        public static void changeStudentEquipment(string socialNumber, string oldPropertyNumber)
        { // the two parameters are for reusability
            Write("specify new equipment you want to assign to this student: ");
            Equipment equipment = chooseEquipment();
            string newPropertyNumber = equipment._propertyNumber;

            EquipmentManager.changeStudentEquipment(oldPropertyNumber, newPropertyNumber, socialNumber);
        }
        public static void showAssignedEquipmentToRooms()
        {
            List<Dictionary<string, object>> allRooms = Program.db.GetAllRecords("Rooms");
            foreach (Dictionary<string, object> room in allRooms)
            {
                WriteLine($"All equipment in Room :{room["RoomNumber"]}, located in Floor: {room["FloorNumber"]} in Block {room["BlockId"]}");
                EquipmentManager.equipmentAssignedToRoom(Convert.ToInt32(room["Id"]));
                WriteLine("-----------------");
            }
        }
        public static void showAssignedEquipmentToStudents()
        {
            List<Dictionary<string, object>> allStudents = Program.db.GetAllRecords("Students");
            foreach (Dictionary<string, object> student in allStudents)
            {
                WriteLine($"All Equipment that belong to student {student["FullName"]}, in Room {student["RoomId"]}, in Block {student["BlockId"]}");
                EquipmentManager.equipmentAssignedToStudent(Convert.ToInt32(student["Id"]));
                WriteLine("-----------------");
            }
        }
        public static void showBrokenEquipment()
        {
            WriteLine("List of broken equipment: ");
            EquipmentManager.showEquipmentWithCondition(Condition.Broken);
        }
        public static void showRepairingEquipment()
        {
            WriteLine("List of equipment that are being repaired: ");
            EquipmentManager.showEquipmentWithCondition(Condition.Reparing);
        }
        public static void showStudentAccommodation()
        {
            List<Dictionary<string, object>> allDorms = Program.db.GetAllRecords("Dormitories");
            if (allDorms.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No records found in table Dormitories[/]");
                return;
            }
            foreach (Dictionary<string, object> dorm in allDorms)
            {
                List<Dictionary<string, object>> dormBlocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", Convert.ToInt32(dorm["Id"]));
                if (dormBlocks.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No records found in table Blocks[/]");
                    return;
                }
                foreach (Dictionary<string, object> block in dormBlocks)
                {
                    List<Dictionary<string, object>> blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
                    if (blockRooms.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]No records found in table Rooms[/]");
                        return;
                    }
                    foreach (Dictionary<string, object> room in blockRooms)
                    {
                        List<Dictionary<string, object>> roomStudents = Program.db.GetRecordsByField("Students", "RoomId", Convert.ToInt32(room["Id"]));
                        if (roomStudents.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[red]No records found in table Students[/]");
                            return;
                        }
                        foreach (Dictionary<string, object> student in roomStudents)
                        {
                            AnsiConsole.WriteLine($"Student: {student["FullName"].ToString()} with Student ID: {student["StudentID"]}, in Room: {room["Name"]}, in Block: {block["Name"]}, in Dormitory: {dorm["Name"]}");
                        }
                    }
                }
            }
        }
        public static void showEmptyRooms()
        {
            List<Dictionary<string, object>> emptyRooms = Program.db.GetRecordsByField("Rooms", "Capacity", 6);
            foreach (Dictionary<string, object> room in emptyRooms)
            {
                AnsiConsole.WriteLine($"Room: {room["RoomNumber"]}, located in Floor: {room["FloorNumber"]}, in Block: {room["BlockId"]}, is empety");
            }
        }
        public static void showFullRooms()
        {
            List<Dictionary<string, object>> fullRooms = Program.db.GetRecordsByField("Rooms", "Capacity", 0);
            if (fullRooms == null || fullRooms.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]No records found[/]");
                return;
            }
            foreach (Dictionary<string, object> room in fullRooms)
            {
                AnsiConsole.WriteLine($"Room: {room["RoomNumber"]}, located in Floor: {room["FloorNumber"]}, in Block: {room["BlockId"]}, is full");
            }
        }
        public static void showRemainingCapacity()
        {
            List<Dictionary<string, object>> allDormitory = Program.db.GetAllRecords("Dormitories");
            if (allDormitory.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No records found in table Dormitories[/]");
                return;
            }
            foreach (Dictionary<string, object> dormitory in allDormitory)
            {
                AnsiConsole.WriteLine($"Dormitory: {dormitory["Name"]}, has {dormitory["Capacity"]} remaining space.");
            }

            List<Dictionary<string, object>> allBlocks = Program.db.GetAllRecords("Blocks");
            if (allBlocks.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No records found in table Blocks[/]");
                return;
            }
            foreach (Dictionary<string, object> block in allBlocks)
            {
                int capacity = Convert.ToInt32(block["NumberOfRooms"]) * 6;
                List<Dictionary<string, object>> Rooms = Program.db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
                foreach (Dictionary<string, object> roomcapacity in Rooms)
                {
                    int n = 6 - Convert.ToInt32(roomcapacity["Capacity"]);
                    capacity -= n;
                }
                AnsiConsole.WriteLine($"Block: {block["Name"]}, has {capacity} remaining space.");
            }
        }
        //public static void showOverallAccommodationReport()
        //{
        //    List<Dictionary<string, object>> allDorms = Program.db.GetAllRecords("Dormitories");
        //    foreach (Dictionary<string, object> dorm in allDorms)
        //    {
        //        List<Dictionary<string, object>> dormBlocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", Convert.ToInt32(dorm["Id"]));
        //        foreach (Dictionary<string, object> block in dormBlocks)
        //        {
        //            List<Dictionary<string, object>> blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
        //            foreach (Dictionary<string, object> room in blockRooms)
        //            {
        //                List<Dictionary<string, object>> roomStudents = Program.db.GetRecordsByField("Students", "RoomId", Convert.ToInt32(room["Id"]));
        //                foreach (Dictionary<string, object> student in roomStudents)
        //                {
        //                    WriteLine($"Student: {student["FullName"].ToString()} with Student ID: {student["StudentID"]}, in Room: {room["Name"]}, in Block: {block["Name"]}, in Dormitory: {dorm["Name"]}");
        //                }
        //                WriteLine("-------------");
        //            }
        //        }
        //    }
        //}
        public static void requestRepair()
        {
            Write("Enter property number of equipment that needs repairing: ");
            string propertyNumber = ReadLine();
            EquipmentManager.registerRepairRequest(propertyNumber);
        }
        public static void showAllRepairRequests()
        {
            List<Dictionary<string, object>> allRepairRequests = Program.db.GetAllRecords("RepairRequests");
            foreach (Dictionary<string, object> request in allRepairRequests)
            {
                Dictionary<string, object> equipment = Program.db.GetRecordsByField("Equipment", "PropertyNumber", Convert.ToInt32(request["PropertyNumber"]))[0];
                Dictionary<string, object> room = Program.db.GetRecordsByField("Rooms", "Id", Convert.ToInt32(equipment["RoomId"]))[0];
                Dictionary<string, object> block = Program.db.GetRecordsByField("Blocks", "Id", Convert.ToInt32(room["BlockId"]))[0];
                if (equipment["OwnerId"] != DBNull.Value)
                {
                    Dictionary<string, object> student = Program.db.GetRecordsByField("Students", "Id", Convert.ToInt32(equipment["OwnerId"]))[0];
                    WriteLine($"Request Number: {request["Id"]}, for a(n) {equipment["Type"]}, with Property Number: {request["PropertyNumber"]}, in Room: {room["RoomNumber"]}, in Block: {block["Name"]}, Owned By Student: {student["FullName"]}, with Student ID: {student["StudentID"]}");

                }
                else
                {
                    WriteLine($"Request Number: {request["Id"]}, for a(n) {equipment["Type"]}, with Property Number: {request["PropertyNumber"]}, in Room: {room["RoomNumber"]}, in Block: {block["Name"]}");
                }
            }
        }
        //dormitory
        public static void GetDormitoryInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Register New dormitory[/]");
                string FullName = AnsiConsole.Ask<string>("Name of dormitory : ");
                if (ENUserInterFace.checkback(FullName)) ENUserInterFace.mainMenu();
                string Address = AnsiConsole.Ask<string>("Address of dormitoy : ");
                if (ENUserInterFace.checkback(Address)) ENUserInterFace.mainMenu();
                int capcity = AnsiConsole.Ask<int>("Capacity of dormitory : ");
                string responsible = AnsiConsole.Ask<string>("Social number of responsible");
                if (ENUserInterFace.checkback(responsible)) ENUserInterFace.mainMenu();
                bool done = DormitoryManager.AddDormitory(FullName, Address, capcity, responsible);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Dormitory Created successflly.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.dormitorymngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Creating dormitory failed, please try again .[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.dormitorymngmnt();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        public static void RemoveDormitory()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Remove Dormitory[/]");
                string name = AnsiConsole.Ask<string>("Enter Dormitory Name : ");
                if (ENUserInterFace.checkback(name)) ENUserInterFace.dormitorymngmnt();

                var data = db.GetRecordsByField("Dormitories", "Name", name);
                if (data.Count > 0)
                {
                    var answ = AnsiConsole.Ask<string>($"An dormitory with Name : {name} & id : {data[0]["Id"]} found , are sure to remove ? (Y/N) ");
                    if (answ.ToLower() == "y")
                    {
                        bool done = DormitoryManager.RemoveDormitory(name);
                        if (done)
                        {
                            AnsiConsole.MarkupLine("[green]Dormitory deleted successfully.[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.dormitorymngmnt();
                        }
                    }
                    else if (answ.ToLower() == "n")
                    {
                        ENUserInterFace.dormitorymngmnt();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]unkonwn command,please retry again... [/]");
                        Thread.Sleep(3000);
                        RemoveDormitory();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]No such a dormitory with name : {name} ![/]");
                    Thread.Sleep(3000);
                    RemoveDormitory();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        public static void UpdateDormitoryInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Update Dormitory infomations[/]");
                string name = AnsiConsole.Ask<string>("Enter Dormitory Name : ");
                if (ENUserInterFace.checkback(name)) ENUserInterFace.dormitorymngmnt();
                var dormitoryRecord = Program.db.GetRecordsByField("Dormitories", "Name", name);
                if (dormitoryRecord == null || dormitoryRecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]No such a dormitory with name : {name} ![/]");
                    Thread.Sleep(3000);
                    UpdateDormitoryInfo();
                }
                var newCapacity = AnsiConsole.Prompt(new TextPrompt<string>("Enter new capacity :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newCapacity)) ENUserInterFace.dormitorymngmnt();
                var newAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter new address :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newAddress)) ENUserInterFace.dormitorymngmnt();
                var newResponsible = AnsiConsole.Prompt(new TextPrompt<string>("Enter new responsible's social number :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newResponsible)) ENUserInterFace.dormitorymngmnt();
                bool done = DormitoryManager.UpdateDormitoryInfoWithCurrentData(name, newCapacity, newAddress, newResponsible);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Informations updated successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.dormitorymngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Updating information failed, please try later [/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.mainMenu();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }
        public static void ShowAllDormitory()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all Dormitories[/]");
                DormitoryManager.ShowAlldormitories();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }

        }
        //block
        public static void GetBlockInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Dormitories(remeber ID)[/]");
                db.ShowAllrecords("Dormitories",false);
                AnsiConsole.MarkupLine("[blue]Register New Block[/]");
                string dormitoryId = AnsiConsole.Ask<string>("Dormitory choosen ID : ");
                if (ENUserInterFace.checkback(dormitoryId)) ENUserInterFace.blockmngmnt();
                string FullName = AnsiConsole.Ask<string>("Name of Block : ");
                if (ENUserInterFace.checkback(FullName)) ENUserInterFace.blockmngmnt();
                string Floor = AnsiConsole.Ask<string>("Number of floors : ");
                if (ENUserInterFace.checkback(Floor)) ENUserInterFace.blockmngmnt();
                string Room = AnsiConsole.Ask<string>("Number of rooms : ");
                if (ENUserInterFace.checkback(Room)) ENUserInterFace.blockmngmnt();
                string capacity = (int.Parse(Room) * 6).ToString();
                if (ENUserInterFace.checkback(capacity)) ENUserInterFace.blockmngmnt();
                string responsible = AnsiConsole.Ask<string>("Social Nuumber of resposible : ");
                if (ENUserInterFace.checkback(responsible)) ENUserInterFace.blockmngmnt();
                bool done = BlocksManager.AddBlock(dormitoryId, FullName, Floor, Room, responsible);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Block Created successflly.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blockmngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Creating Block failed, please try again .");
                    Thread.Sleep(3000);
                    ENUserInterFace.dormitorymngmnt();
                }

            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }
        public static void RemoveBlock()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Remove Block[/]");
                string name = AnsiConsole.Ask<string>("Enter Block Name : ");
                if (ENUserInterFace.checkback(name)) ENUserInterFace.blockmngmnt();

                var data = db.GetRecordsByField("Blocks", "Name", name);
                if (data.Count > 0)
                {
                    var answ = AnsiConsole.Ask<string>($"A block with Name : {name} & id : {data[0]["Id"]} found , are sure to remove ? (Y/N) ");
                    if (answ.ToLower() == "y")
                    {
                        bool done = BlocksManager.RemoveBlock(name);
                        if (done)
                        {
                            AnsiConsole.MarkupLine("[green]Block deleted successfully.[/]");
                            Thread.Sleep(3000);
                            ENUserInterFace.blockmngmnt();
                        }
                    }
                    else if (answ.ToLower() == "n")
                    {
                        ENUserInterFace.blockmngmnt();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]unkonwn command,please retry again... [/]");
                        Thread.Sleep(3000);
                        RemoveBlock();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]No such a block with name : {name} ![/]");
                    Thread.Sleep(3000);
                    RemoveBlock();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }
        public static void UpdateBlockInfo()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Update Block infomations[/]");
                string name = AnsiConsole.Ask<string>("Enter Block Name : ");
                if (ENUserInterFace.checkback(name)) ENUserInterFace.blockmngmnt();
                var blockRecord = Program.db.GetRecordsByField("Blocks", "Name", name);
                if (blockRecord == null || blockRecord.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]No such a block with name : {name} ![/]");
                    Thread.Sleep(3000);
                    UpdateBlockInfo();
                }
                var newfloor = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Number of floor(s) :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newfloor)) ENUserInterFace.blockmngmnt();
                var newroom = AnsiConsole.Prompt(new TextPrompt<string>("Enter new Number of room(s) (if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newroom)) ENUserInterFace.blockmngmnt();
                var newResponsible = AnsiConsole.Prompt(new TextPrompt<string>("Enter new responsible's social number :(if you don't want to change it,leave it blank) ").AllowEmpty());
                if (ENUserInterFace.checkback(newResponsible)) ENUserInterFace.blockmngmnt();
                bool done = BlocksManager.UpdateBlockInfoWithCurrentData(name, newfloor, newroom, newResponsible);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Informations updated successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.blockmngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Updating information failed, please try later [/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.mainMenu();
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.dormitorymngmnt();
            }
        }

        public static void showAllEquipmentAssignedToEachRoom()
        {
            List<Dictionary<string, object>> allDorms = Program.db.GetAllRecords("Dormitories");
            if (allDorms.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No records found in table Dormitories[/]");
                return;
            }
            foreach (Dictionary<string, object> dorm in allDorms)
            {
                List<Dictionary<string, object>> dormBlocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", Convert.ToInt32(dorm["Id"]));
                if (dormBlocks.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No records found in table Blocks[/]");
                    return;
                }
                foreach (Dictionary<string, object> block in dormBlocks)
                {
                    List<Dictionary<string, object>> blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
                    if (blockRooms.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]No records found in table Rooms[/]");
                        return;
                    }
                    foreach (Dictionary<string, object> room in blockRooms)
                    {
                        List<Dictionary<string, object>> equipment = Program.db.GetRecordsByField("Equipment", "RoomId", Convert.ToInt32(room["Id"]));
                        if (equipment.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[red]No records found in table Equipment[/]");
                            return;
                        }
                        foreach (Dictionary<string, object> equip in equipment)
                        {
                            AnsiConsole.WriteLine($"Equipment: {equip["Type"]} with PartNumber: {equip["PartNumber"]}, in Room: {room["Name"]}, in Block: {block["Name"]}, in Dormitory: {dorm["Name"]}");
                        }
                    }
                }
            }
        }
        public static void showAllEquipmentAssignedToEachStudent()
        {
            List<Dictionary<string, object>> allDorms = Program.db.GetAllRecords("Dormitories");
            if (allDorms.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No records found in table Dormitories[/]");
                return;
            }
            foreach (Dictionary<string, object> dorm in allDorms)
            {
                List<Dictionary<string, object>> dormBlocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", Convert.ToInt32(dorm["Id"]));
                if (dormBlocks.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No records found in table Blocks[/]");
                    return;
                }
                foreach (Dictionary<string, object> block in dormBlocks)
                {
                    List<Dictionary<string, object>> blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", Convert.ToInt32(block["Id"]));
                    if (blockRooms.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]No records found in table Rooms[/]");
                        return;
                    }
                    foreach (Dictionary<string, object> room in blockRooms)
                    {
                        List<Dictionary<string, object>> student = Program.db.GetRecordsByField("Students", "RoomId", Convert.ToInt32(room["Id"]));
                        if (student.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[red]No records found in table Students[/]");
                            return;
                        }
                        foreach (Dictionary<string, object> stu in student)
                        {
                            List<Dictionary<string, object>> equipment = Program.db.GetRecordsByField("Equipment", "OwnerId", Convert.ToInt32(stu["Id"]));
                            if (equipment.Count == 0)
                            {
                                AnsiConsole.MarkupLine("[red]No records found in table Equipment[/]");
                                return;
                            }
                            foreach (Dictionary<string, object> equip in equipment)
                            {
                                AnsiConsole.WriteLine($"Equipment: {equip["Type"]} with PartNumber: {equip["PartNumber"]},is for {stu["FullName"]}, in Room: {room["Name"]}, in Block: {block["Name"]}, in Dormitory: {dorm["Name"]}");
                            }
                        }
                    }
                }
            }
        }

        public static void showAllDefectiveAndInrepairEquipment()
        {
            AnsiConsole.MarkupLine("[blue]Show all defective and inrepair equipment[/]");
            List<Dictionary<string, object>> allEquipment = Program.db.GetAllRecords("Equipment");
            if (allEquipment.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No records found in table Equipment[/]");
                return;
            }
            List<Dictionary<string, object>> BrokenEquipment = Program.db.GetRecordsByField("Equipment", "Condition", "Broken");
            foreach (Dictionary<string, object> equipment in BrokenEquipment)
            {
                AnsiConsole.WriteLine($"The situation of Equipment: {equipment["Type"]} with PartNumber: {equipment["PartNumber"]}, and with PropertyNumber: {equipment["PropertyNumber"]}, is Broken");
            }
            List<Dictionary<string, object>> RepairEquipment = Program.db.GetRecordsByField("Equipment", "Condition", "Reparing");
            foreach (Dictionary<string, object> equipment in RepairEquipment)
            {
                AnsiConsole.WriteLine($"The situation of Equipment: {equipment["Type"]} with PartNumber: {equipment["PartNumber"]}, and with PropertyNumber: {equipment["PropertyNumber"]}, is Reparing");
            }
        }
        public static void ShowAllBlocks()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all Blocks[/]");
                BlocksManager.ShowAllblocks();
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]An error occurred. Returning to menu...[/]");
                Thread.Sleep(3000);
                ENUserInterFace.blockmngmnt();
            }
        }

        public static void RegisterNewEquipment()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Dormitories(remeber ID)[/]");
                db.ShowAllrecords("Dormitories", false);
                AnsiConsole.MarkupLine("[blue]Blocks(remeber ID)[/]");
                db.ShowAllrecords("Blocks", false);
                AnsiConsole.MarkupLine("[blue]Registering a New Equipment[/]");
                string type = AnsiConsole.Ask<string>("Enter the Type of Equipment: ");
                if (ENUserInterFace.checkback(type)) ENUserInterFace.equipmentmngmnt();
                string condition = AnsiConsole.Ask<string>("Enter the Condition of Equipment(intact, broken, repairing): ");
                if (ENUserInterFace.checkback(condition)) ENUserInterFace.equipmentmngmnt();
                string blockid = AnsiConsole.Ask<string>("Enter ID of the Block the Equipment Belongs to: ");
                if (ENUserInterFace.checkback(blockid)) ENUserInterFace.equipmentmngmnt();
                string dormid = AnsiConsole.Ask<string>("Enter ID of the Dormitory the Equipment Belongs to: ");
                if (ENUserInterFace.checkback(dormid)) ENUserInterFace.equipmentmngmnt();
                Equipment NewEquipment = new Equipment(type, Condition.Intact, int.Parse(blockid), int.Parse(dormid));

                if (condition.ToLower() == "broken")
                {
                    NewEquipment._condition = Condition.Broken;
                }
                else if (condition.ToLower() == "repairing")
                {
                    NewEquipment._condition = Condition.Reparing;
                }

                bool done = EquipmentManager.addEquipmentToDB(NewEquipment);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Equipment Added Successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Adding Equipment Failed, Try Again.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.equipmentmngmnt();
            }
        }

        public static void AssignEquipmentToRoom()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Add the Desired Equipment to Room[/]");
                AnsiConsole.MarkupLine("[blue]Rooms(remeber ID)[/]");
                db.ShowAllrecords("Rooms", false);
                string roomid = AnsiConsole.Ask<string>("Chosen Room ID: ");
                if (ENUserInterFace.checkback(roomid)) ENUserInterFace.equipmentmngmnt();
                string propertynumber = AnsiConsole.Ask<string>("Property Number of Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.equipmentmngmnt();
                bool done = EquipmentManager.assignEquipmentToRoom(propertynumber, roomid);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Equipment Assigned Successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Assigning Equipment Failed, Please Try Again.");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }

            }
            catch (Exception)
            {
                Thread.Sleep(3000);
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
                string propertynumber = AnsiConsole.Ask<string>("Property Number of Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.equipmentmngmnt();
                bool done = EquipmentManager.assignEquipmentToStudent(propertynumber, socialid);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Equipment Assigned Successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Assigning Equipment Failed, Please Try Again.");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }

            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.equipmentmngmnt();
            }
        }

        public static void ExchangeEquipmentBetweenRooms()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Move the Desired Equipment to Another Room[/]");
                AnsiConsole.MarkupLine("[blue]Rooms(remeber ID)[/]");
                db.ShowAllrecords("Rooms", false);
                string roomid = AnsiConsole.Ask<string>("Enter Destination Room's ID: ");
                if (ENUserInterFace.checkback(roomid)) ENUserInterFace.equipmentmngmnt();
                string propertynumber = AnsiConsole.Ask<string>("Property Number of Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.equipmentmngmnt();
                bool done = EquipmentManager.exchangeEquipmentBetweenRooms(propertynumber, roomid);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Equipment Moved Successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Moving Equipment Failed, Please Try Again.");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }

            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.equipmentmngmnt();
            }
        }
        public static void ChangeStudentEquipment()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Change Student's Equipment[/]");
                string socialid = AnsiConsole.Ask<string>("Enter Student's Social ID: ");
                if (ENUserInterFace.checkback(socialid)) ENUserInterFace.equipmentmngmnt();
                string oldpropertynumber = AnsiConsole.Ask<string>("Property Number of The Current Equipment: ");
                if (ENUserInterFace.checkback(oldpropertynumber)) ENUserInterFace.equipmentmngmnt();
                string newpropertynumber = AnsiConsole.Ask<string>("Property Number of Equipment to Replace With The Current Equipment: ");
                if (ENUserInterFace.checkback(newpropertynumber)) ENUserInterFace.equipmentmngmnt();
                bool done = EquipmentManager.changeStudentEquipment(oldpropertynumber, newpropertynumber, socialid);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Student's Equipment Changed Successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Changing Equipment Failed, Please Try Again.");
                    Thread.Sleep(3000);
                    ENUserInterFace.equipmentmngmnt();
                }

            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.equipmentmngmnt();
            }
        }

        public static void RequestRepair()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Request Repair of an Equipment[/]");
                string propertynumber = AnsiConsole.Ask<string>("Enter Property Number of Broken Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.maintenancemngmnt();
                bool done = EquipmentManager.registerRepairRequest(propertynumber);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Request for Repair of Equipment Registered Successfully.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.maintenancemngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Registering Repair Request for Equipment Failed, Please Try Again.");
                    Thread.Sleep(3000);
                    ENUserInterFace.maintenancemngmnt();
                }

            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.maintenancemngmnt();
            }
        }

        public static void CheckRepairStatus()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Checking Repair Request of an Equipment[/]");
                string propertynumber = AnsiConsole.Ask<string>("Enter Property Number of The Equipment Being Repaired: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.maintenancemngmnt();
                Program.db.ShowRecordsByField("RepairRequests", "PropertyNumber", propertynumber);
            }
            catch (Exception)
            {
                AnsiConsole.Markup("[red]Checking Status of Repairing Equipment Failed, Please Try Again.");
                Thread.Sleep(3000);
                ENUserInterFace.maintenancemngmnt();
            }
        }

        public static void SetEquipmentConditionAsBroken()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Changing Condition of Equipment to Broken[/]");
                string propertynumber = AnsiConsole.Ask<string>("Enter Property Number of Broken Equipment: ");
                if (ENUserInterFace.checkback(propertynumber)) ENUserInterFace.maintenancemngmnt();
                bool done = EquipmentManager.changeEquipmentCondition(propertynumber, Condition.Broken);
                if (done)
                {
                    AnsiConsole.MarkupLine("[green]Equipment's Condition Successfully Changed to Broken.[/]");
                    Thread.Sleep(3000);
                    ENUserInterFace.maintenancemngmnt();
                }
                else
                {
                    AnsiConsole.Markup("[red]Changing Condition of Equipment to Broken Failed, Please Try Again.");
                    Thread.Sleep(3000);
                    ENUserInterFace.maintenancemngmnt();
                }

            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.maintenancemngmnt();
            }
        }

        public static void ShowStudentAccommodation()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show student accommodation[/]");
                report.ShowStudentAccommodation();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowFullAndEmptyRoom()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show full and empty room[/]");
                report.ShowFullAndEmptyRoom();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowRemainingCapacity()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]show remaining capacity[/]");
                report.showRemainingCapacity();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipment()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all equipment[/]");
                report.ShowAllEquipment();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipmentAssignedToEachRoom()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all equipment assigned to each room[/]");
                report.ShowAllEquipmentAssignedToEachRoom();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllEquipmentAssignedToEachStudent()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all equipment assigned to each student[/]");
                report.ShowAllEquipmentAssignedToEachStudent();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllDefectiveAndInrepairEquipment()
        {
            try
            {
                report.ShowAllDefectiveAndInrepairEquipment();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
            }
        }
        public static void ShowAllRepairRequests()
        {
            try
            {
                AnsiConsole.MarkupLine("[blue]Show all repair requests[/]");
                report.ShowAllRepairRequests();
            }
            catch (Exception)
            {
                Thread.Sleep(3000);
                ENUserInterFace.reporting();
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
            db = new DatabaseManager();
            db.InitializeDatabase();
            //resetdatabase();
            ENUserInterFace.Login();
        }
    }
}