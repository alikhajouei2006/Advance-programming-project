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


namespace FinalProj
{
    public class DatabaseManager
    {
        private string connectionString = "Data Source=MyDatabase.sqlite;Version=3;";

        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                CreateTables(connection);
            }
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
                    PersonId INTEGER NOT NULL,
                    StudentID TEXT,
                    RoomId INTEGER,
                    BlockId INTEGER,
                    FOREIGN KEY (PersonId) REFERENCES Persons(Id),
                    FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
                    FOREIGN KEY (BlockId) REFERENCES Blocks(Id)
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
            }
        }
    }
    class DormitoryBlockSupervisor : Student
    {
        private string _Role;
        private string _BlckUnRespons;
        public DormitoryBlockSupervisor(string fullName, string socialNumber, string phoneNumber, string address, string studentID, string room, string block, List<string> personalItems , string Role, string BlckUnRespons) : base(fullName, socialNumber, phoneNumber, address , studentID , room , block , personalItems)
        {
            _Role = Role;
            _BlckUnRespons = BlckUnRespons;
        }
    }
    class Student : Person
    {
        protected string _StudentID;
        protected string _Room;
        protected string _Block;
        protected List<string> _PersonalItems;
        public Student(string fullName , string socialNumber , string phoneNumber , string address , string studentID, string room, string block, List<string> personalItems) : base(fullName , socialNumber, phoneNumber , address)
        {
            _StudentID = studentID;
            _Room = room;
            _Block = block;
            _PersonalItems = personalItems;
        }
    }

    enum Condition
    {
        Intact,
        Broken,
        Reparing,
    }

    class Equipment
    {
        protected string _type;
        protected string _partNumber;
        protected string _propertyNumber;
        protected Condition _condition;
        protected Room _room;

        public Equipment(string type, string partNumber, string propertyNumber, Condition condition, Room room)
        {
            _type = type;
            _partNumber = partNumber;
            _propertyNumber = propertyNumber;
            _condition = condition;
            _room = room;
        }


    }

    class PersonalEquipment : Equipment {
	    private Student _owner;

	    public PersonalEquipment(string type, string partNumber, string propertyNumber, Condition condition, Room room, Student owner):base(type, partNumber, propertyNumber, condition, room) {
		    _owner = owner;
	    }
    }

    class Person
    {
        protected string _fullName;
        protected string _socialNumber;
        protected string _phoneNumber;
        protected string _address;

        public Person(string fullName, string socialNumber, string phoneNumber, string address)
        {
            _fullName = fullName;
            _socialNumber = socialNumber;
            _phoneNumber = phoneNumber;
            _address = address;
        }
    }

    class DormitorySupervisor : Person
    {
        protected string _position;
        protected Dormitory _dormitory;

        public DormitorySupervisor(string fullName, string socialNumber, string phoneNumber, string address , string position, Dormitory dormitory) : base(fullName, socialNumber, phoneNumber, address)
        {
            _position = position;
            _dormitory = dormitory;
        }
    }


    class Dormitory
    {
        private string _name;
        private string _address; 
        private string _responsible;
        private List<string> _blocks = new List<string>();
        private int _capacity;
        public Dormitory(string name, string address ,int capcity , string responsible , string block)
        {
            _name = name;
            _address = address;
            _capacity = capcity;
            _responsible = responsible;
            _blocks.Add(block);
        }
        
    }
    class Block
    {
        private string _name;
        private string _responsible;
        private string _dormitory;
        private int _NO_floors;
        private int _NO_rooms;
        private List<string> _rooms;
        public Block(string dormitory, string name, int floor, int room, string responsible , List<string> rooms)
        {
            _dormitory = dormitory;
            _name = name;
            _NO_floors = floor;
            _NO_rooms = room;
            _responsible = responsible;
            _rooms = rooms;
        }
    }
    class Room
    {
        private List<string> _equipment = new List<string>();
        private List<string> _students = new List<string>();
        string _block;
        int _NO_room, _NO_floors, _capacity;
        public Room(int no_room, int no_floor, int capacity, string block , List<string> equipment , List<string> students)
        {
            _NO_room = no_room;
            _NO_floors = no_floor ;
            _capacity = capacity;
            _block = block;
            _equipment = equipment;
            _students = students;
        }
    }
}
