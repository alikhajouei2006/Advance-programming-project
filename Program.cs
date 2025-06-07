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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using form1;
using form2;
using form3;
using form4;

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
                        command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
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
                cmd.CommandText = directorSql; cmd.ExecuteNonQuery();
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
        public Student(string fullName, string socialNumber, string phoneNumber, string address, string studentID, string roomId, string blockId, string dormitoryId, List<string> personalItems = null)
            : base(fullName, socialNumber, phoneNumber, address)
        {
            _StudentID = studentID;
            _RoomId = roomId;
            _BlockId = blockId;
            _DormitoryId = dormitoryId;
            _PersonalItems = personalItems ?? new List<string>();
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
            Dictionary<string, object> info = new Dictionary<string, object>
            {
                {"FullName",student._fullName},
                {"SocialNumber",student._socialNumber},
                {"StudentId" , student._StudentID},
                {"PhoneNumber",student._phoneNumber},
                {"Address",student._address},
                {"RoomId" ,student._RoomId},
                {"BlockId",student._BlockId},
                {"DormitoryId" , student._DormitoryId}
            };
            return info;
        }
        public static void AddStudent()
        {
            Student student = Program.GetStudentInfo();
            Program.db.InsertRecord("Studetns", StudentManager.ToDictionary(student));
        }
        public static void RemoveStudent(string socialNumber)
        {
            if (Program.db.DoesSocialNumberExist("Students", socialNumber))
            {
                Program.db.DeleteRecord("Students", "SocialNumber", socialNumber);
                WriteLine("دانشجو با موفقیت حذف شد.");
            }
            else
                WriteLine("دانشجویی با این شماره ملی یافت نشد.");
        }
        public static void UpdateStudentInfoWithCurrentData(string SocialNumber)
        {
            var studentRecord = Program.db.GetRecordsByField("Students", "SocialNumber", SocialNumber);
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
            Program.db.UpdateRecord("Students", updateFields, "SocialNumber", SocialNumber);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
        public static void SerachStudent(string socialNumber, string phoneNumber = null)
        {
            var studentRecord = phoneNumber == null ?
                Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber) :
                Program.db.GetRecordsByField("Students", "PhoneNumber", phoneNumber);

            if (studentRecord == null || studentRecord.Count == 0)
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

    class Dormitory
    {
        public int Id { get; set; }

        public string _name;
        public string _address { get; set; }
        public string _responsible { get; set; }

        private List<string> _blocks = new List<string>();
        public int _capacity { get; set; }
        public Dormitory(string name, string address, int capcity, string responsible, List<string> block = null)
        {
            _name = name;
            _address = address;
            _capacity = capcity;
            _responsible = responsible;
            _blocks = block ?? new List<string>();
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
                { "capcity",dormitory._capacity},
                { "Responsible",dormitory._responsible},
            };
            return info;
        }
        public static void AddDormitory(DatabaseManager db)
        {
            Dormitory dormitory = Program.GetDormitoryInfo();
            Program.db.InsertRecord("Dormitories", DormitoryManager.ToDictionary(dormitory));
        }
        public static void RemoveDormitory(string name)
        {
            if (Program.db.DoesSocialNumberExist("Dormitories", name))
            {
                Program.db.DeleteRecord("Dormitories", "Name", name);
                WriteLine("خوابگاه با موفقیت حذف شد.");
            }
            else
                WriteLine("خوابگاه با این اسم یافت نشد.");
        }
        public static void UpdateDormitoryInfoWithCurrentData(string name)
        {
            var dormitoryRecord = Program.db.GetRecordsByField("Dormitories", "Name", name);
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
            Program.db.UpdateRecord("Dormitories", updateFields, "Name", name);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
        public static void ShowAlldormitories()
        {
            var dormitories = Program.db.GetAllRecords("Dormitories");

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
        public string _dormitoryId { get; set; }
        public int _NO_floors { get; set; }
        public int _NO_rooms { get; set; }
        private List<string> _rooms { get; set; }
        public Block(string dormitory, string name, int floor, int room, string responsible, List<string> rooms=null)
        {
            _dormitoryId = dormitory;
            _name = name;
            _NO_floors = floor;
            _NO_rooms = room;
            _responsible = responsible;
            _rooms = rooms ?? new List<string>();
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
                { "Responsible",block._responsible},
            };
            return info;
        }
        public static void AddBlock(DatabaseManager db)
        {
            Block block = Program.GetBlockInfo();
            Program.db.InsertRecord("Blocks", BlocksManager.ToDictionary(block));
        }
        public static void RemoveBlock(string name)
        {
            if (Program.db.DoesSocialNumberExist("Blocks", name))
            {
                Program.db.DeleteRecord("Blocks", "Name", name);
                WriteLine("بلوک با موفقیت حذف شد.");
            }
            else
                WriteLine("بلوک با این اسم یافت نشد.");
        }
        public static void UpdateBlockInfoWithCurrentData(string name)
        {
            var blockRecord = Program.db.GetRecordsByField("Blocks", "Name", name);
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
            Program.db.UpdateRecord("Blocks", updateFields, "Name", name);
            WriteLine("\nتغییرات با موفقیت ذخیره شد.");
        }
        public static void ShowAllblocks()
        {
            var blocks = Program.db.GetAllRecords("Blocks");

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
    public class Equipment
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
    public class DormitorySupervisor : Person
    {
        public string _position { get; set; }
        public Dormitory _dormitory { get; set; }
        public DormitorySupervisor(string fullName, string socialNumber, string phoneNumber, string address, string position, Dormitory dormitory = null) : base(fullName, socialNumber, phoneNumber, address)
        {
            _position = position;
            _dormitory = dormitory;
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
                { "DormitoryID", dormitorySupervisor._dormitory != null ? (object)dormitorySupervisor._dormitory.Id : DBNull.Value }
             };
            return values;
        }
        public static void AddSuperVisor()
        {
            DormitorySupervisor dormiSupervisor = Program.GetSuperVisorInfo();
            Program.db.InsertRecord("dormitorySupervisor", DormitorySuperVisorManager.ToDictionary(dormiSupervisor));
        }
        public static void DeleteSpuervisor()
        {
            string socialNumber = Program.RemoveSupervisor();
            Program.db.DeleteRecord("dormitorySupervisor", "socialNumber", socialNumber);
        }
        public static void UpdateSupervisor()
        {

            var values = Program.UpdateSupervisor();
            if (values["DormitoryId"] == "")
            {
                values.Remove("DormitoryId");
            }
            string socialNumber = (string)values["SocialNumber"];
            values.Remove(socialNumber);
            Program.db.UpdateRecord("dormitorySupervisor", values, "SocialNumber", socialNumber);
            WriteLine("مسئول با موفقیت بروزرسانی شد.");
        }
        public static void ShowAllSupervisors()
        {
            var supervisors = Program.db.GetAllRecords("dormitorySupervisor");

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
    public class DormitoryBlockSupervisor
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

    }
    public class DormitoryBlockSupervisorManager
    {
        public static Dictionary<string, object> ToDictionary(DormitoryBlockSupervisor blocksupervisor)
        {
            var values = new Dictionary<string, object>
            {
                {"FullName", blocksupervisor.PersonInfo._fullName},
                {"SocialNumber", blocksupervisor.PersonInfo._socialNumber},
                {"PhoneNumber", blocksupervisor.PersonInfo._phoneNumber},
                {"Address", blocksupervisor.PersonInfo._address},
                {"StudentId", blocksupervisor.StudentInfo != null ? (object)blocksupervisor.StudentInfo._StudentID : DBNull.Value},
                {"Room", blocksupervisor.StudentInfo != null ? (object)blocksupervisor.StudentInfo._RoomId : DBNull.Value},
                {"Block", blocksupervisor.StudentInfo != null ? (object)blocksupervisor.StudentInfo._BlockId : DBNull.Value},
                {"Role", blocksupervisor.Role},
                {"BlockUnderResponsibility", blocksupervisor.BlockUnderResponsibility}
            };
            return values;
        }
        public static void AddBlockSupervisor()
        {
            DormitoryBlockSupervisor supervisor = Program.AddBlockSupervisor();
            Program.db.InsertRecord("dormitoryBlockSupervisor", DormitoryBlockSupervisorManager.ToDictionary(supervisor));
        }
        public static void RemoveDormitoryBlockSupervisor()
        {
            string socialNumber = Program.RemoveBlockSupervisor();
            Program.db.DeleteRecord("dormitoryBlockSupervisor", "socialNumber", socialNumber);
        }
        public static void UpdateDormitoryBlockSupervisor(string socialNumber)
        {
            if (Program.db.DoesSocialNumberExist("dormitoryBlockSupervisor", socialNumber))
            {

                var dormiBlockSupervisorRecrod = Program.db.GetRecordsByField("DormitoryBlockSupervisor", "SocialNumber", socialNumber);
                if (Program.db.DoesSocialNumberExist("students", socialNumber))
                {
                    Program.UpdateStudentInfo();
                    var studentRecord = Program.db.GetRecordsByField("students", "SocialNumber", socialNumber);
                    if (studentRecord[0]["BlockId"] != dormiBlockSupervisorRecrod[0]["BlockUnderResponsibility"])
                    {
                        throw new Exception();
                    }
                    else
                    {
                        StudentManager.UpdateStudentInfoWithCurrentData(socialNumber);
                    }
                }
                else
                {
                    WriteLine("شماره تلفن جدید را وارد کنید(خالی در صورت عدم تغییر) :");
                    string newPhone = ReadLine();
                    WriteLine("آدرس جدید را وارد کنید(خالی در صورت عدم تغییر) :");
                    string newAddress = ReadLine();
                    WriteLine("آیدی بلوک جدید را وارد کنید(خالی در صورت عدم تغییر) :");
                    string newDormitoryBlock = ReadLine();
                    WriteLine("سمت جدید(خالی در صورت عدم تغییر) :");
                    string newPosition = ReadLine();
                    if (newPhone == "" && newAddress == "" && newDormitoryBlock == "" && newPosition == "")
                    {
                        WriteLine("هیچ تغییری ایجاد نشد!");
                        return;
                    }
                    var values = new Dictionary<string, object>
                    {
                        {"FullName",dormiBlockSupervisorRecrod[0]["FullName"] },
                        {"SocialNumber",dormiBlockSupervisorRecrod[0]["SocialNumber"] },
                        {"PhoneNumber",newPhone != null ? newPhone : dormiBlockSupervisorRecrod[0]["[PhoneNumber"] },
                        {"Adress",newAddress != null ? newAddress : dormiBlockSupervisorRecrod[0]["Address"] },
                        {"Position" , newPosition != null ? newPosition : dormiBlockSupervisorRecrod[0]["Position"]}
                    };
                    Program.db.UpdateRecord("dormitoryBlockSupervisor", values, "SocialNumber", socialNumber);
                }

            }
            else
            {
                throw new Exception();
            }
        }
        public static void ShowAllBlockSupervisors()
        {
            var supervisors = Program.db.GetAllRecords("dormitoryBlockSupervisor");

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
    public enum Condition
    {
        Intact,
        Broken,
        Reparing,
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
                { "Password", PasswordHasher.HashPassword(director._Password) }
            };
        }
        public static bool AddDirector(Director director)
        {
            Program.db.InsertRecord("Director", DirectorManager.ToDictionary(director));
            if (Program.db.DoesUserNameExist(director._UserName)) return true;
            return false;
        }
        public static bool Login(string username, string password)
        {
            if (Program.db.DoesUserNameExist(username))
            {
                var directrRecord = Program.db.GetRecordsByField("Director", "UserName", username);
                if (directrRecord[0]["Password"] == PasswordHasher.HashPassword(password))
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
            if (directorRecord[0]["SocialNumber"] == socialnumber && directorRecord[0]["PhoneNumber"] == phonenumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static string GetNewpassword(string username, string newpass)
        {
            var directorRecord = Program.db.GetRecordsByField("Director", "UserName", username);

            if (newpass.Length >= 8)
            {
                Dictionary<string, object> newinfo = new Dictionary<string, object>
                {
                    { "Password", PasswordHasher.HashPassword( newpass) }
                };
                Program.db.UpdateRecord("Director", newinfo, "UserName", username);
                return "success";
            }
            return "lenerror";
        }
    }
    public static class PasswordHasher
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
    }
    internal static class Program
    {

        public static DatabaseManager db;

        public static Dormitory GetDormitoryInfo()
        {
            Write("نام: ");
            string FullName = ReadLine();
            Write("آدرس: ");
            string Address = ReadLine();
            Write("ظرفیت: ");
            int capcity = int.Parse(ReadLine());
            Write("مسئول خوابگاه: ");
            string responsible = ReadLine();
            Dormitory dormitory = new Dormitory(FullName, Address, capcity, responsible);
            return dormitory;
        }
        public static void RemoveDormitory()
        {
            WriteLine("نام خوابگاه را وارد کنید.");
            string name = ReadLine();
            DormitoryManager.RemoveDormitory(name);
        }
        public static void UpdateDormitoryInfo()
        {
            WriteLine("نام خوابگاه را وارد کنید.");
            string name = ReadLine();
            DormitoryManager.UpdateDormitoryInfoWithCurrentData(name);
        }
        public static void ShowAllDormitory()
        {
            DormitoryManager.ShowAlldormitories();
        }
        public static Block GetBlockInfo()
        {
            WriteLine("لیست خوابگاه‌ها:");
            var dormitories = db.GetAllRecords("Dormitories");
            foreach (var dorm in dormitories)
                WriteLine($"{dorm["Id"]}: {dorm["Name"]} - {dorm["Address"]}");

            Write("آیدی خوابگاه انتخابی: ");
            int dormitoryId = int.Parse(ReadLine());
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
            //var studentRecord = Program.db.GetRecordsByField("Students", "FullName", responsible);
            //if (studentRecord == null || studentRecord.Count == 0)
            //{
            //    WriteLine("دانشجویی با این اسم یافت نشد.");
            //}
            Block block = new Block(dormitoryId.ToString(), FullName, int.Parse(Floor), int.Parse(Room) ,responsible);
            return block;
        }
        public static void RemoveBlock()
        {
            WriteLine("نام بلوک را وارد کنید.");
            string name = ReadLine();
            BlocksManager.RemoveBlock(name);
        }
        public static void UpdateBlockInfo()
        {
            WriteLine("نام بلوک را وارد کنید.");
            string name = ReadLine();
            BlocksManager.UpdateBlockInfoWithCurrentData(name);
        }
        public static void ShowAllBlock()
        {
            BlocksManager.ShowAllblocks();
        }
        public static Student GetStudentInfo()
        {
            WriteLine("نام کامل: ");
            string FullName = ReadLine();
            WriteLine("شماره ملی: ");
            string SocialNumber = ReadLine();
            WriteLine("شماره تماس: ");
            string PhoneNumber = ReadLine();
            WriteLine("آدرس: ");
            string Address = ReadLine();
            WriteLine("شماره دانشجویی: ");
            string StudentId = ReadLine();
            Student student = GetStudentPlace(FullName, SocialNumber, PhoneNumber, Address, StudentId);
            return student;
        }
        public static Student GetStudentPlace(string fullName, string socialNumber, string phoneNumber, string address, string studentId)
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
                int RoomId = Convert.ToInt32(room["Id"]);
                int capacity = Convert.ToInt32(room["Capacity"]);
                var students = db.GetRecordsByField("Students", "RoomId", RoomId);
                if (students.Count < capacity)
                {
                    int remaining = capacity - students.Count;
                    WriteLine($"{room["Id"]}: اتاق {room["RoomNumber"]} - باقی‌مانده {remaining}");
                }
            }

            Write("آیدی اتاق انتخابی: ");
            int roomId = int.Parse(ReadLine());
            Student student = new Student(fullName, socialNumber, phoneNumber, address, studentId, roomId.ToString(), blockId.ToString(), dormitoryId.ToString());
            return student;
        }
        public static void RemoveStudent()
        {
            WriteLine("کد ملی دانشجو را وارد کنید.");
            string socialNumber = ReadLine();
            StudentManager.RemoveStudent(socialNumber);
        }
        public static void UpdateStudentInfo()
        {
            WriteLine("کد ملی دانشجو را وارد کنید.");
            string socialNumber = ReadLine();
            StudentManager.UpdateStudentInfoWithCurrentData(socialNumber);
        }
        public static void SerachStudent()
        {
            WriteLine("کد ملی / شماره تلفن دانشجو را وارد کنید.");
            string socialNumberOrPhoneNumber = ReadLine();
            string regex = @"^09(10|11|12|13|14|15|16|17|18|19|00|01|02|03|04|05|06|07|08|09|30|33|35|36|37|38|39)\d{7}$";
            if (Regex.IsMatch(socialNumberOrPhoneNumber, regex))
            {
                StudentManager.SerachStudent(null, socialNumberOrPhoneNumber);
            }
            else
            {
                StudentManager.SerachStudent(socialNumberOrPhoneNumber);
            }
        }
        public static void ChangeStudentPlace()
        {
            WriteLine("کد ملی دانشجو را وارد کنید.");
            string socialNumber = ReadLine();
            StudentManager.ChangeDoirmiBlckRoom(socialNumber);
        }
        //dormitory supervisor
        public static DormitorySupervisor GetSuperVisorInfo()
        {
            WriteLine("نام کامل: ");
            string FullName = ReadLine();
            WriteLine("شماره ملی: ");
            string SocialNumber = ReadLine();
            WriteLine("شماره تماس: ");
            string PhoneNumber = ReadLine();
            WriteLine("آدرس: ");
            string Address = ReadLine();
            WriteLine("سممت :");
            string Position = ReadLine();
            WriteLine("آیدی خوابگاه :");
            string DormitoryId = ReadLine();
            DormitorySupervisor dormitorysupervisor = new DormitorySupervisor(FullName, SocialNumber, PhoneNumber, Address, Position);
            return dormitorysupervisor;
        }
        public static string RemoveSupervisor()
        {
            WriteLine("کد ملی مسئول را وارد کنید :");
            string soicalNumber = ReadLine();
            return soicalNumber;
        }
        public static Dictionary<string, object> UpdateSupervisor()
        {
            WriteLine("کد ملی مسئول را وارد کنید :");
            string SoicalNumber = ReadLine();
            WriteLine("شماره تلفن جدید: ");
            string newPhone = ReadLine();

            WriteLine("آدرس جدید: ");
            string newAddress = ReadLine();

            WriteLine("سمت جدید: ");
            string newPosition = ReadLine();

            WriteLine("آیدی خوابگاه جدید(خالی برای عدم تغییر) :");
            string dormitoryId = ReadLine();

            var values = new Dictionary<string, object>
            {
                {"SoicalNumber", SoicalNumber},
                { "PhoneNumber", newPhone },
                { "Address", newAddress },
                { "Position", newPosition },
                { "DormitoryID", dormitoryId}
            };
            return values;
        }
        public static void ShowAllSupreVisor()
        {
            DormitorySuperVisorManager.ShowAllSupervisors();
        }
        //dormitoryBlockSupervisor
        public static DormitoryBlockSupervisor AddBlockSupervisor()
        {
            Person person = null;
            Student student = null;
            Write("نام کامل: ");
            string FullName = ReadLine();
            WriteLine("شماره ملی: ");
            string SocialNumber = ReadLine();
            WriteLine("شماره تماس: ");
            string PhoneNumber = ReadLine();
            WriteLine("آدرس: ");
            string Address = ReadLine();
            WriteLine("شماره دانشحویی (درصورت دانشجو نبودن خالی(:");
            string StudentId = ReadLine();
            //if student we should get more info from database
            if (StudentId == null)
            {
                WriteLine("سمت :");
                string Position = ReadLine();
                //show all block
                WriteLine("ایدی بلوک :");
                string Block = ReadLine();
                person = new Person(FullName, SocialNumber, PhoneNumber, Address);
                return new DormitoryBlockSupervisor(person, Position, Block);
            }
            else
            {
                var StudentRecord = db.GetRecordsByField("stdents", "SocialNumber", SocialNumber);
                string block = StudentRecord[0]["BlockId"].ToString();
                string room = StudentRecord[0]["RoomId"].ToString();
                string dormitory = StudentRecord[0]["DormitoryId"].ToString();
                student = new Student(FullName, SocialNumber, PhoneNumber, Address, StudentId, room, block, dormitory);
                return new DormitoryBlockSupervisor(student, "Student", block);
            }
        }
        public static string RemoveBlockSupervisor()
        {
            WriteLine("کد ملی مسئول را وارد کنید :");
            string SocialNumber = ReadLine();
            return SocialNumber;
        }
        public static void UpdateBlockSupervisor()
        {
            WriteLine("کد ملی مسئول را وارد کنید :");
            string SocialNumber = ReadLine();
            DormitoryBlockSupervisorManager.UpdateDormitoryBlockSupervisor(SocialNumber);
        }
        public static void ShowBlockSupervisor()
        {
            DormitoryBlockSupervisorManager.ShowAllBlockSupervisors();
        }
        //director
        public static bool Login(string username, string password)
        {
            return DirectorManager.Login(username, password);
        }
        public static bool ResetPassword(string username, string socialnumber, string phonenumber)
        {
            return DirectorManager.ResetPasswird(username, socialnumber, phonenumber);
        }
        public static string GetNewPassword(string username, string newpass)
        {
            return DirectorManager.GetNewpassword(username, newpass);
        }
        public static bool signin(Director director)
        {
            return DirectorManager.AddDirector(director);
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
        [STAThread]
        static void Main()
        {
            db = new DatabaseManager();
            db.InitializeDatabase();
            //resetdatabase();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();
            Application.Run(new frmLogin());
        }
    }
}