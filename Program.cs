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


namespace FinalProj
{
    public class DatabaseManager
    {
        private string _connectionString = "Data Source=MyDatabase.sqlite;Version=3;";

        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                CreateTables(connection);
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
        public bool DoesSocialNumberExist(string tableName, string socialNumber)
        {
            var result = GetRecordsByField(tableName, "SocialNumber", socialNumber);
            return result.Count > 0;
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
                        command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                    }
                    command.ExecuteNonQuery();
                }
            }
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
            Name TEXT NOT NULL,
            Address TEXT NOT NULL,
            Capacity INTEGER NOT NULL,
            Responsible TEXT
        );";

            string blockSql = @"
        CREATE TABLE IF NOT EXISTS Blocks (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            DormitoryId INTEGER NOT NULL,
            Responsible TEXT,
            NumberOfFloors INTEGER,
            NumberOfRooms INTEGER,
            FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id)
        );";

            string roomSql = @"
        CREATE TABLE IF NOT EXISTS Rooms (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BlockId INTEGER NOT NULL,
            RoomNumber INTEGER,
            FloorNumber INTEGER,
            Capacity INTEGER,
            FOREIGN KEY (BlockId) REFERENCES Blocks(Id)
        );";

            string dormitoryBlockSupervisorSql = @"
        CREATE TABLE IF NOT EXISTS DormitoryBlockSupervisors (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FullName TEXT NOT NULL,
            SocialNumber TEXT UNIQUE NOT NULL,
            PhoneNumber TEXT NOT NULL,
            Address TEXT NOT NULL,
            StudentId INTEGER,
            RoomId INTEGER,
            BlockId INTEGER,
            Role TEXT NOT NULL,
            FOREIGN KEY (StudentId) REFERENCES Students(Id),
            FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
            FOREIGN KEY (BlockId) REFERENCES Blocks(Id)
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
            FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id)
        );";

            string personSql = @"
        CREATE TABLE IF NOT EXISTS Persons (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FullName TEXT NOT NULL,
            SocialNumber TEXT UNIQUE,
            PhoneNumber TEXT,
            Address TEXT
        );";

            string studentSql = @"
            CREATE TABLE IF NOT EXISTS Students (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                SocialNumber TEXT UNIQUE NOT NULL,
                PhoneNumber TEXT,
                Address TEXT,
                StudentID TEXT,
                RoomId INTEGER,
                BlockId INTEGER,
                DormitoryId INTEGER,
                FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
                FOREIGN KEY (BlockId) REFERENCES Blocks(Id),
                FOREIGN KEY (DormitoryId) REFERENCES Dormitories(Id)
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
            FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
            FOREIGN KEY (OwnerId) REFERENCES Students(Id)
        );";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = dormitorySql; cmd.ExecuteNonQuery();
                cmd.CommandText = blockSql; cmd.ExecuteNonQuery();
                cmd.CommandText = roomSql; cmd.ExecuteNonQuery();
                cmd.CommandText = personSql; cmd.ExecuteNonQuery();
                cmd.CommandText = studentSql; cmd.ExecuteNonQuery();
                cmd.CommandText = itemSql; cmd.ExecuteNonQuery();
                cmd.CommandText = equipmentSql; cmd.ExecuteNonQuery();
                cmd.CommandText = dormitoryBlockSupervisorSql; cmd.ExecuteNonQuery();
                cmd.CommandText = dormitorySupervisorSql; cmd.ExecuteNonQuery();
            }

        }
    }
    class Person
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
    class Student : Person
    {
        public string _StudentID { get; set; }
        public string _RoomId { get; set; }
        public string _BlockId { get; set; }
        public string _DormitoryId {  get; set; }
        protected List<string> _PersonalItems { get; set; }
        public Student(string fullName, string socialNumber, string phoneNumber, string address, string studentID, List<string> personalItems =null)
            : base(fullName, socialNumber, phoneNumber, address)
        {
            _StudentID = studentID;
            _PersonalItems = personalItems ?? new List<string>();
        }
        public void AssignAccommodation(string dormitoryid , string blockid , string roomid)
        {
            _RoomId = roomid;
            _BlockId = blockid;
            _DormitoryId = dormitoryid;
        }
        public static void AddStudent(DatabaseManager db)
        {
            Write("نام کامل: ");
            string FullName = ReadLine();
            Write("شماره ملی: ");
            string SocialNumber = ReadLine();
            Write("شماره تماس: ");
            string PhoneNumber = ReadLine();
            Write("آدرس: ");
            string Address = ReadLine();
            Write("شماره دانشجویی: ");
            string StudentId = ReadLine();
            Dictionary<string,object> info = new Dictionary<string,object>
            {
                {"FullName",FullName},
                {"SocialNumber",SocialNumber},
                {"StudentId",StudentId},
                {"PhoneNumber",PhoneNumber},
                {"Address",Address},
            };
            RegisterDormitoryStudent(db,info);
        }
        public static void RemoveStudent(DatabaseManager db, string socialNumber) 
        {
            if (db.DoesSocialNumberExist("students", socialNumber))
            {
                db.DeleteRecord("students", "SocialNumber", socialNumber);
                WriteLine("دانشجو با موفقیت حذف شد.");
            }
            else
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
        }
        public static void UpdateStudentInfoWithCurrentData(DatabaseManager db, string SocialNumber)
        {
            var studentRecord = db.GetRecordsByField("students", "SocialNumber", SocialNumber);
            if (studentRecord == null || studentRecord.Count == 0)
            {
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
                return;
            }

            var student = studentRecord[0];
            string currentPhoneNumber = student["PhoneNumber"]?.ToString() ?? "";
            string currentAddress = student["Address"]?.ToString() ?? "";
            string newPhone = ReadLine();
            if (string.IsNullOrEmpty(newPhone)) newPhone = currentPhoneNumber;
            string newAddress = ReadLine();
            if (string.IsNullOrEmpty(newAddress)) newAddress = currentAddress;

            var updateFields = new Dictionary<string, object>
        {
            { "PhoneNumber", newPhone },
            { "Address", newAddress }
        };
            db.UpdateRecord("students", updateFields, "SocialNumber",SocialNumber);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
        public static void SerachStudent(DatabaseManager db, string socialNumber, string phoneNumber = null)
        {
            var studentRecord = phoneNumber == null ?
                db.GetRecordsByField("students", "SocialNumber", socialNumber) :
                db.GetRecordsByField("students", "PhoneNumber", phoneNumber);

            if(studentRecord == null || studentRecord.Count == 0)
            {
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
                return;
            }

            var student = studentRecord[0];
            WriteLine($"نام کامل: {student["FullName"]}");
            WriteLine($"کد ملی: {student["SocialNumber"]}");
            WriteLine($"شماره دانشجویی: {student["StudentID"]}");
            WriteLine($"شماره تلفن: {student["PhoneNumber"]}");
            WriteLine($"آدرس: {student["Address"]}");
        }
        public static void ShowFullStudentInfo(DatabaseManager db, string socialNumber)
        {
            var studentRecord = db.GetRecordsByField("students", "SocialNumber", socialNumber);
            if (studentRecord == null || studentRecord.Count == 0)
            {
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
                return;
            }
            var student = studentRecord[0];
            int roomId = student["RoomId"] != DBNull.Value ? Convert.ToInt32(student["RoomId"]) : -1;
            int blockId = student["BlockId"] != DBNull.Value ? Convert.ToInt32(student["BlockId"]) : -1;
            string roomNumber = "نامشخص", blockName = "نامشخص", dormitoryName = "نامشخص";

            if (roomId != -1)
            {
                var room = db.GetRecordsByField("Rooms", "Id", roomId).FirstOrDefault();
                if (room != null)
                {
                    roomNumber = room["RoomNumber"].ToString();
                    blockId = Convert.ToInt32(room["BlockId"]);
                }
            }

            if (blockId != -1)
            {
                var block = db.GetRecordsByField("Blocks", "Id", blockId).FirstOrDefault();
                if (block != null)
                {
                    blockName = block["Name"].ToString();
                    int dormitoryId = Convert.ToInt32(block["DormitoryId"]);
                    var dorm = db.GetRecordsByField("Dormitories", "Id", dormitoryId).FirstOrDefault();
                    if (dorm != null) dormitoryName = dorm["Name"].ToString();
                }
            }

            var equipmentRecords = db.GetRecordsByField("Equipment", "OwnerId", student["Id"].ToString());
            List<string> equipmentList = equipmentRecords.Select(eq =>
            {
                string item = eq["Type"]?.ToString() ?? "نامشخص";
                string partNumber = eq["PartNumber"]?.ToString() ?? "";
                return $"{item} (شماره قطعه: {partNumber})";
            }).ToList();

            WriteLine($"نام کامل: {student["FullName"]}");
            WriteLine($"کد ملی: {student["SocialNumber"]}");
            WriteLine($"شماره دانشجویی: {student["StudentID"]}");
            WriteLine($"شماره تلفن: {student["PhoneNumber"]}");
            WriteLine($"آدرس: {student["Address"]}");
            WriteLine($"اتاق: {roomNumber}");
            WriteLine($"بلوک: {blockName}");
            WriteLine($"خوابگاه: {dormitoryName}");
            WriteLine("تجهیزات:");
            if (equipmentList.Count == 0) WriteLine("بدون تجهیزات");
            else equipmentList.ForEach(item => WriteLine($"- {item}"));
        }
        public static void RegisterDormitoryStudent(DatabaseManager db,Dictionary<string,object> info)
        {
            WriteLine("لیست خوابگاه‌ها:");
            var dormitories = db.GetAllRecords("Dormitories");
            foreach (var dorm in dormitories)
                WriteLine($"{dorm["Id"]}: {dorm["Name"]} - {dorm["Address"]}");

            Write("آیدی خوابگاه انتخابی: ");
            int dormitoryId = int.Parse(ReadLine());

            WriteLine("لیست بلوک‌های خوابگاه:");
            var blocks = db.GetRecordsByField("Blocks", "DormitoryId", dormitoryId);
            foreach (var block in blocks)
                WriteLine($"{block["Id"]}: {block["Name"]}");

            Write("آیدی بلوک انتخابی: ");
            int blockId = int.Parse(ReadLine());

            WriteLine("اتاق‌های دارای ظرفیت باقی‌مانده:");
            var rooms = db.GetRecordsByField("Rooms", "BlockId", blockId);
            foreach (var room in rooms)
            {
                int roomId = Convert.ToInt32(room["Id"]);
                int capacity = Convert.ToInt32(room["Capacity"]);
                var students = db.GetRecordsByField("students", "RoomId", roomId);
                if (students.Count < capacity)
                {
                    int remaining = capacity - students.Count;
                    WriteLine($"{room["Id"]}: اتاق {room["RoomNumber"]} - باقی‌مانده {remaining}");
                }
            }

            Write("آیدی اتاق انتخابی: ");
            int roomIdChosen = int.Parse(ReadLine());



            info.Add("RoomId", roomIdChosen.ToString());
            info.Add("BlockId",blockId.ToString());
            info.Add("DormitoryId",dormitoryId.ToString());
            db.InsertRecord("students", info);
        }
        public static void ChangeDoirmiBlckRoom(DatabaseManager db, string socialNumber)
        {
            var studentRecord = db.GetRecordsByField("students", "SocialNumber", socialNumber);
            if (studentRecord == null || studentRecord.Count == 0)
            {
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
                return;
            }

            WriteLine("لیست خوابگاه‌ها:");
            var dormitories = db.GetAllRecords("Dormitories");
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
            var blocks = db.GetRecordsByField("Blocks", "DormitoryId", dormitoryId);
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
            var rooms = db.GetRecordsByField("Rooms", "BlockId", blockId);
            bool hasAvailableRoom = false;
            foreach (var room in rooms)
            {
                int roomId = Convert.ToInt32(room["Id"]);
                int capacity = Convert.ToInt32(room["Capacity"]);
                var studentsInRoom = db.GetRecordsByField("students", "RoomId", roomId);
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

            db.UpdateRecord("students", newDormBlckRoom, "SocialNumber", socialNumber);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
    }
    class Dormitory
    {
        public int Id { get; set; }

        public string _name;
        public string _address { get; set; }
        public string _responsible { get; set; }

        private List<string> _blocks = new List<string>();
        public int _capacity { get; set; }
        public Dormitory(string name, string address, int capcity, string responsible, string block)
        {
            _name = name;
            _address = address;
            _capacity = capcity;
            _responsible = responsible;
            _blocks.Add(block);
        }
        public static void AddDormitory(DatabaseManager db)
        {
            Write("نام: ");
            string FullName = ReadLine();
            Write("آدرس: ");
            string Address = ReadLine();
            Write("ظرفیت: ");
            int capcity = int.Parse(ReadLine());
            Write("مسئول خوابگاه: ");
            string responsible = ReadLine();

            Dictionary<string, object> info = new Dictionary<string, object>
            {
                {"Name",FullName},
                {"Address",Address},
                {"capcity",capcity},
                {"Responsible",responsible},
            };
            db.InsertRecord("Dormitories", info);
        }
        public static void RemoveDormitory(DatabaseManager db, string name)
        {
            if (db.DoesSocialNumberExist("Dormitories", name))
            {
                db.DeleteRecord("Dormitories", "Name", name);
                WriteLine("خوابگاه با موفقیت حذف شد.");
            }
            else
                WriteLine("خوابگاه با این اسم یافت نشد.");
        }
        public static void UpdateDormitoryInfoWithCurrentData(DatabaseManager db, string name)
        {
            var dormitoryRecord = db.GetRecordsByField("Dormitories", "Name", name);
            if (dormitoryRecord == null || dormitoryRecord.Count == 0)
            {
                WriteLine("خوابگاهی با این اسم یافت نشد.");
                return;
            }

            var dormitory = dormitoryRecord[0];
            string currentCapcity = dormitory["Capacity"]?.ToString() ?? "";
            string currentAddress = dormitory["Address"]?.ToString() ?? "";
            string currentResponsible = dormitory["Responsible"]?.ToString() ?? "";

            string newCapcity = ReadLine();
            if (string.IsNullOrEmpty(newCapcity)) newCapcity = currentCapcity;
            string newAddress = ReadLine();
            if (string.IsNullOrEmpty(newAddress)) newAddress = currentAddress;
            string newResponsible = ReadLine();
            if (string.IsNullOrEmpty(newResponsible)) newResponsible = currentAddress;

            var updateFields = new Dictionary<string, object>
        {
            { "Capacity", newCapcity },
            { "Address", newAddress },
            { "Responsible", newResponsible }
        };
            db.UpdateRecord("Dormitories", updateFields, "Name", name);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
        public static void ShowAlldormitories(DatabaseManager db)
        {
            var dormitories = db.GetAllRecords("Dormitories");

            if (dormitories.Count == 0)
            {
                WriteLine("هیچ خوابگاهی ثبت نشده است.");
                return;
            }

            foreach (var sup in dormitories)
            {
                WriteLine("-------------");
                WriteLine($"نام: {sup["Name"]}");
                WriteLine($"آدرس: {sup["Address"]}");
                WriteLine($"ظرفیت کل: {sup["Capacity"]}");
                WriteLine($"مسئول خوابگاه: {sup["Responsible"]}");
            }
        }
    }
    class Block
    {
        public int Id { get; set; }
        public string _name { set; get; }
        public string _responsible { get; set; }
        public string _dormitory { get; set; }
        public int _NO_floors { get; set; }
        public int _NO_rooms { get; set; }
        private List<string> _rooms { get; set; }
        public Block(string dormitory, string name, int floor, int room, string responsible, List<string> rooms)
        {
            _dormitory = dormitory;
            _name = name;
            _NO_floors = floor;
            _NO_rooms = room;
            _responsible = responsible;
            _rooms = rooms;
        }
        public static void AddBlock(DatabaseManager db)
        {
            Write("نام: ");
            string FullName = ReadLine();
            Write("طبقات: ");
            string Floor = ReadLine();
            Write("اتاق: ");
            string Room = ReadLine();
            Write("ظرفیت: ");
            int capcity = int.Parse(ReadLine());
            Write("مسئول بلوک: ");
            string responsible = ReadLine();

            Dictionary<string, object> info = new Dictionary<string, object>
            {
                {"Name",FullName},
                {"NumberOfFloors",Floor},
                {"NumberOfRooms",Room},
                {"Responsible",responsible},
            };
            db.InsertRecord("Blocks", info);
        }
        public static void RemoveBlock(DatabaseManager db, string name)
        {
            if (db.DoesSocialNumberExist("Blocks", name))
            {
                db.DeleteRecord("Blocks", "Name", name);
                WriteLine("بلوک با موفقیت حذف شد.");
            }
            else
                WriteLine("بلوک با این اسم یافت نشد.");
        }
        public static void UpdateBlockInfoWithCurrentData(DatabaseManager db, string name)
        {
            var blockRecord = db.GetRecordsByField("Blocks", "Name", name);
            if (blockRecord == null || blockRecord.Count == 0)
            {
                WriteLine("بلوک با این اسم یافت نشد.");
                return;
            }

            var block = blockRecord[0];
            string currentFloor = block["NumberOfFloors"]?.ToString() ?? "";
            string currentRoom = block["NumberOfRooms"]?.ToString() ?? "";
            string currentResponsible = block["Responsible"]?.ToString() ?? "";

            string newFloor = ReadLine();
            if (string.IsNullOrEmpty(newFloor)) newFloor = currentFloor;
            string newRoom = ReadLine();
            if (string.IsNullOrEmpty(newRoom)) newRoom = currentRoom;
            string newResponsible = ReadLine();
            if (string.IsNullOrEmpty(newResponsible)) newResponsible = currentResponsible;

            var updateFields = new Dictionary<string, object>
        {
            { "NumberOfFloors", newFloor },
            { "NumberOfRooms", newRoom },
            { "Responsible", newResponsible }
        };
            db.UpdateRecord("Blocks", updateFields, "Name", name);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
        public static void ShowAllblocks(DatabaseManager db)
        {
            var blocks = db.GetAllRecords("Blocks");

            if (blocks.Count == 0)
            {
                WriteLine("هیچ بلوکی ثبت نشده است.");
                return;
            }
            foreach (var sup in blocks)
            {
                WriteLine("-------------");
                WriteLine($"نام: {sup["Name"]}");
                WriteLine($"طبقات: {sup["NumberOfFloors"]}");
                WriteLine($"اتاق ها: {sup["NumberOfRooms"]}");
                WriteLine($"مسئول بلوک: {sup["Responsible"]}");
            }
        }
    }
    class Room
    {
        public int Id { get; set; }

        private List<string> _equipment = new List<string>();

        private List<string> _students = new List<string>();
        public string _block { set; get; }

        public int _NO_room { get; set; }
        public int _NO_floors { get; set; }
        public int _capacity { get; set; }
        public Room(int no_room, int no_floor, int capacity, string block, List<string> equipment, List<string> students)
        {
            _NO_room = no_room;
            _NO_floors = no_floor;
            _capacity = capacity;
            _block = block;
            _equipment = equipment;
            _students = students;
        }
    }
    class Equipment
    {
        private string _type { get; set; }
        private string _partNumber { get; set; }
        private string _propertyNumber { get; set; }
        private Condition _condition { get; set; }
        private Room _room { get; set; }
        private Student _owner { get; set; }

        public Equipment(string type, string partNumber, string propertyNumber, Condition condition, Room room, Student owner)
        {
            _type = type;
            _partNumber = partNumber;
            _propertyNumber = propertyNumber;
            _condition = condition;
            _room = room;
            _owner = owner;
        }
    }
    class DormitorySupervisor : Person
    {
        public string _position { get; set; }
        public Dormitory _dormitory { get; set; }
        public DormitorySupervisor(string fullName, string socialNumber, string phoneNumber, string address, string position, Dormitory dormitory) : base(fullName, socialNumber, phoneNumber, address)
        {
            _position = position;
            _dormitory = dormitory;
        }

        public static void AddSuperVisor(DatabaseManager dbManager, DormitorySupervisor dormiSupervisor)
        {
            var values = new Dictionary<string, object>
            {
                { "FullName", dormiSupervisor._fullName },
                { "SocialNumber", dormiSupervisor._socialNumber },
                { "PhoneNumber", dormiSupervisor._phoneNumber },
                { "Address", dormiSupervisor._address },
                { "Position", dormiSupervisor._position },
                { "DormitoryID", dormiSupervisor._dormitory != null ? (object)dormiSupervisor._dormitory.Id : DBNull.Value }
            };

            dbManager.InsertRecord("dormitorySupervisor", values);
        }

        public static void DeleteSpuervisor(DatabaseManager DBmanager, string socialNumber)
        {
            DBmanager.DeleteRecord("dormitorySupervisor", "socialNumber", socialNumber);
        }

        public static void UpdateSupervisor(DatabaseManager db, string socialNumber)
        {
            Write("شماره تلفن جدید: ");
            string newPhone = ReadLine();

            Write("آدرس جدید: ");
            string newAddress = ReadLine();

            Write("سمت جدید: ");
            string newPosition = ReadLine();

            Write("آیدی خوابگاه جدید (یا خالی برای حذف): ");
            string dormIdStr = ReadLine();
            int? dormitoryId = null;
            if (!string.IsNullOrWhiteSpace(dormIdStr) && int.TryParse(dormIdStr, out int parsedId))
                dormitoryId = parsedId;

            var values = new Dictionary<string, object>
            {
                { "PhoneNumber", newPhone },
                { "Address", newAddress },
                { "Position", newPosition },
                { "DormitoryID", dormitoryId ?? (object)DBNull.Value }
            };

            db.UpdateRecord("dormitorySupervisor", values, "SocialNumber", socialNumber);
            WriteLine("مسئول با موفقیت بروزرسانی شد.");
        }

        public static void ShowAllSupervisors(DatabaseManager db)
        {
            var supervisors = db.GetAllRecords("dormitorySupervisor");

            if (supervisors.Count == 0)
            {
                WriteLine("هیچ مسئولی ثبت نشده است.");
                return;
            }

            foreach (var sup in supervisors)
            {
                WriteLine("-------------");
                WriteLine($"کد: {sup["Id"]}");
                WriteLine($"نام و نام خانوادگی: {sup["FullName"]}");
                WriteLine($"کد ملی: {sup["SocialNumber"]}");
                WriteLine($"شماره تلفن: {sup["PhoneNumber"]}");
                WriteLine($"آدرس: {sup["Address"]}");
                WriteLine($"سمت: {sup["Position"]}");
                WriteLine($"آیدی خوابگاه: {sup["DormitoryID"]}");
            }
        }
    }
    class DormitoryBlockSupervisor
    {
        public Person PersonInfo { get; private set; }
        public Student StudentInfo { get; private set; }

        public string Role { get; set; }
        public string BlockUnderResponsibility { get; set; }

        public DormitoryBlockSupervisor(Person person, string role, string blockUnderResponsibility)
        {
            PersonInfo = person;
            StudentInfo = null;
            Role = role;
            BlockUnderResponsibility = blockUnderResponsibility;
        }

        public DormitoryBlockSupervisor(Student student, string role, string blockUnderResponsibility)
        {
            StudentInfo = student;
            PersonInfo = student;
            Role = role;
            BlockUnderResponsibility = blockUnderResponsibility;
        }

        public static void AddBlockSupervisor(DatabaseManager dbManager, DormitoryBlockSupervisor supervisor)
        {
            var values = new Dictionary<string, object>
        {
            {"FullName", supervisor.PersonInfo._fullName},
            {"SocialNumber", supervisor.PersonInfo._socialNumber},
            {"PhoneNumber", supervisor.PersonInfo._phoneNumber},
            {"Address", supervisor.PersonInfo._address},
            {"StudentId", supervisor.StudentInfo != null ? (object)supervisor.StudentInfo._StudentID : DBNull.Value},
            {"Room", supervisor.StudentInfo != null ? (object)supervisor.StudentInfo._RoomId : DBNull.Value},
            {"Block", supervisor.StudentInfo != null ? (object)supervisor.StudentInfo._BlockId : DBNull.Value},
            {"Role", supervisor.Role},
            {"BlockUnderResponsibility", supervisor.BlockUnderResponsibility}
        };

            dbManager.InsertRecord("dormitoryBlockSupervisor", values);
        }


        public static void RemoveDormitoryBlockSupervisor(DatabaseManager databaseManager, string socialNumber)
        {
            databaseManager.DeleteRecord("dormitoryBlockSupervisor", "socialNumber", socialNumber);
        }

        public static void UpdateDormitoryBlockSupervisor(DatabaseManager dbManager, string socialNumber, DormitoryBlockSupervisor supervisor)
        {
            if (dbManager.DoesSocialNumberExist("dormitoryBlockSupervisor", socialNumber))
            {
                var values = new Dictionary<string, object>
                {
                    {"FullName", supervisor.PersonInfo._fullName},
                    {"PhoneNumber", supervisor.PersonInfo._phoneNumber},
                    {"Address", supervisor.PersonInfo._address},
                    {"StudentId", supervisor.StudentInfo != null ? (object)supervisor.StudentInfo._StudentID : DBNull.Value},
                    {"Room", supervisor.StudentInfo != null ? (object)supervisor.StudentInfo._RoomId : DBNull.Value},
                    {"Block", supervisor.StudentInfo != null ? (object)supervisor.StudentInfo._BlockId : DBNull.Value},
                    {"Role", supervisor.Role},
                    {"BlockUnderResponsibility", supervisor.BlockUnderResponsibility}
                };

                dbManager.UpdateRecord("dormitoryBlockSupervisor", values, "SocialNumber", socialNumber);
            }
            else
            {
                throw new Exception();
            }
        }

        public static void ShowAllBlockSupervisors(DatabaseManager dbManager)
        {
            var supervisors = dbManager.GetAllRecords("dormitoryBlockSupervisor");

            if (supervisors.Count == 0)
            {
                WriteLine("هیچ مسئول بلوکی ثبت نشده است.");
                return;
            }

            foreach (var sup in supervisors)
            {
                WriteLine("-------------");
                WriteLine($"نام و نام خانوادگی: {sup["FullName"]}");
                WriteLine($"کد ملی: {sup["SocialNumber"]}");
                WriteLine($"شماره تلفن: {sup["PhoneNumber"]}");
                WriteLine($"آدرس: {sup["Address"]}");

                if (sup["StudentId"] != DBNull.Value)
                {
                    WriteLine($"کد دانشجویی: {sup["StudentId"]}");
                    WriteLine($"اتاق: {sup["Room"]}");
                    WriteLine($"بلوک: {sup["Block"]}");
                }
                else
                {
                    WriteLine("این فرد دانشجو نیست.");
                }

                WriteLine($"سمت: {sup["Role"]}");
                WriteLine($"بلوک تحت مسئولیت: {sup["BlockUnderResponsibility"]}");
            }
        }
    }
    enum Condition
    {
        Intact,
        Broken,
        Reparing,
    }
}