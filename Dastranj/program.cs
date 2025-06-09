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

    enum RequestStatus:string {
	    Pending="Pending",
	    Done="Done",
    }

    class RepairRequest {
	    private string propertyNumber;
	    private RequestStatus status;

	    public RepairRequest(string propertyNumber, RequestStatus status=RequestStatus.Pending) {
		    this.propertyNumber = propertyNumber;
		    this.status = status;
	    }

	    public void setStatusToDone() {
		    this.status = RequestStatus.Done;
	    }

	    public Dicrtionary<string, object> ToDictionary() {
		    Dictionary<string, object> outDict = Dictionary<string, object> {
			    {"PropertyNumber", this.propertyNumber},
			    {"Status", (string)this.status}
		    };

		    return outDict;
	    }

	    public static FromDictionary(Dictionary<string, object> requestDict) {
		    return new RepairRequest(requestDict["PropertyNumber"].ToString(), (RequestStatus)requestDict["Status"])
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

    enum Condition : string
    {
        Intact = "Intact",
        Broken = "Broken",
        Repairing = "Repairing",
    }

    class Equipment
    {
        protected string _type;
        protected string _partNumber;
        protected string _propertyNumber;
        protected Condition _condition;
        protected int _RoomId;

        public Equipment(string type, string partNumber, string propertyNumber, Condition condition, int roomid)
        {
            _type = type;
            _partNumber = partNumber;
            _propertyNumber = propertyNumber;
            _condition = condition;
            _RoomId = roomid;
        }

	public Equipment(string type, string partNumber, string propertyNumber, Condition condition) {
		_type = type;
		_partNumber = partNumber;
		_propertyNumber = propertyNumber;
		_condition = condition;
		_RoomId = null;
	}

	public virtual Dictionary<string, object> ToDictionary() {
		return new Dictionary<string, object> {
			{"Type", this._type},
			{"PartNumber", this._partNumber},
			{"PropertyNumber", this._propertyNumber},
			{"Condition", this._condition},
			{"RoomId", this._RoomId};
		}
	}

	public static Equipment FromDictionary(Dictionary<string, object> equipmentDict) {
		Equipment equipment = new Equipment(equipmentDict["Type"].ToString(), equipmentDict["PartNumber"].ToString(), equipmentDict["PropertyNumber"].ToString(), (Condition)equipmentDict["Condition"], equipmentDict["RoomId"].ToInt32());
		return equipment;
	}


    }

    class PersonalEquipment : Equipment {
	    private Student _owner;

	    public PersonalEquipment(string type, string partNumber, string propertyNumber, Condition condition, Room room, Student owner):base(type, partNumber, propertyNumber, condition, room) {
		    _owner = owner;
	    }

	    public override Dictionary<string, object> ToDictionary() {
		    Dictionary<string, object> outputDict = base.ToDictionary();
		    outputDict.Add("OwnerId", this._owner);
		    return outputDict;
		 
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
	public int Id {get; set;};
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
	public Room(int no_room, int no_floor, in capacity, string block) {
		// this constructor is for cases when we don't want to specify students and equipment in the room
		_NO_room = no_room;
		_NO_floors = no_floor;
		_capacity = capacity;
		_block = block;
	}

	public static Room FromDictionary(Dictionary<string, object> roomDict) {
		room = new Room(roomDict["RoomNumber"].ToInt32(), roomDict["FloorNumber"].ToInt32(), roomDict["Capacity"].ToInt32(), roomDict["BlockId"].ToString());
		room.Id = roomDict["Id"];
		return room;
	}
    }

    class EquipmentManager {
	    public static void addEquipmentToDB(Equipment newEquipment) {
		    Dictionary<string, object> info = newEquipment.ToDictionary();
		    
		    if (newEquipment._RoomId == null) {
			    info["RoomId"] = DBNull;
		    }
		    else {
			    info["RoomId"] = Program.db.GetRecordsByField("Rooms", "Id", newEquipment._room.Id)[0]["Id"];
		    }

		    if (newEquipment.GetType() == typeof(SharedEquipment)) {
			    info["OwnerId"] = Program.db.GetRecordsByField("Students", "SocialNumber", newEquipment._owner._socialNumber)[0]["Id"];
		    }
		    else {
			    info.Add("OwnerId", DBNull);
		    }
		    
		    Program.db.InsertRecord("Equipment", info);
	    }

	    public static void assignEquipmentToRoom(string propertyNumber, string roomId) {
		    var RoomId = Program.db.GetRecordsByField("Rooms", "RoomId", roomId)[0]["Id"]; // need implementation for Id in Room class and adding RoomId column to db
		    Dictionary<string, object> EquipmentUpdatedValues = Dictionary<string, object> {
			    {"RoomId", RoomId}
		    };
		    Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", propertyNumber);
	    }
	    
	    public static void assignEquipmentToStudent(string propertyNumber, string socialNumber) {
		    Dictionary<string, object> studentDict = Prorgam.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
		    var StudentId = studentDict["Id"];
		    var RoomId = studentDict["RoomId"];
		    Dictionary<string, object> EquipmentUpdatedValues = Dictionary<string, object> {
			    {"RoomId", RoomId},
			    {"OwnerId", StudentId}
		    };
		    Program.db.UpdateRecord("Equipment", EquipmentUpdatedValues, "PropertyNumber", string propertyNumber);
	    }

	    public static void exchangeEquipmentBetweenRooms(string propertyNumber, string roomId) {
		    var destinationRoomId = Program.db.GetRecordsByField("Rooms", "RoomId", roomId)[0]["Id"];

		    Dictionary<string, object> ChangedRoomId = Dictionary<string, object> {
			    {"RoomId", destinationRoomId}
		    };

		    Program.db.UpdateRecord("Equipment", ChangedRoomId, "PropertyNumber", propertyNumber);
	    }

	    public static void changeStudentEquipment(string oldPropertyNumber, string newPropertyNumber, string socialNumber) {
		    Dictionary<string, object> studentDict = Program.db.GetRecordsByField("Students", "SocialNumber", socialNumber)[0];
		    var ownerId = studentDict["Id"];
		    var roomId = studentDict["RoomId"];

		    Dictionary<string, object> newEquipmentUpdatedValues = Dictionary<string, object> {
			    {"RoomId", roomId},
			    {"OwnerId", ownerId}
		    };

		    Dictionary<string, object> oldEquipmentUpdatedValues = Dictionary<string, object> {
			    {"RoomId", DBNull},
			    {"OwnerId", DBNull}
		    };

		    Program.db.UpdateRecord("Equipment", newEquipmentUpdatedValues, "PropertyNumber", newPropertyNumber);
		    Program.db.UpdateRecord("Equipment", oldEquipmentUpdatedValues, "PropertyNumber", oldPropertyNumber);
	    }

	    public static void changeEquipmentCondition(string propertyNumber, Condition condition) {
		    Dictionary<string, object> UpdatedCondition = Dictionary<string, object> {
			    {"Condition", (string)condition}
		    };

		    Program.db.UpdateRecord("Equipment", UpdatedCondition, "PropertyNumber", propertyNumber);
	    }

	    public static Condition checkCondition(string propertyNumber) {
		    Equipment equipment = Equipment.FromDictionary(Program.db.GetRecordsByField("Equipment", "PropertyNumber", propertyNumber)[0]);
		    return (string)equipment._condition;
	    }

	    public static void showAllEquipment() {
		    List<Dictionary<string, object>> allEquipment = Program.db.GetAllRecords("Equipment");
		    foreach (Dictionary<string, object> equipemnt in allEquipment) {
			    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
		    }
	    }

	    public static void equipmentAssignedToRoom(int RoomId) {
		    List<Dictionary<string, object>> roomEquipment = Program.db.GetRecordsByField("Equipment", "RoomId", RoomId);
		    foreach (Dictionary<string, object> equipment in roomEquipment) {
			    if (equipment["OwnerId"] != DBNull.Value) {
				    Dictionary<string, object> owner = Program.db.GetRecordsByField("Students", "Id", equipment["OwnerId"])[0];
			    	    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}, Owned by: {owner["FullName"]}");
			    }
			    else {
				    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
			    }
		    }
	    }

	    public static void equipmentAssignedToStudent(int StudentId) {
		    List<Dictionary<string, object>> studentEquipment = Program.db.GetRecordsByField("Equipment", "OwnerId", StudentId);
		    foreach (Dictionary<string, object> equipment in studentEquipment) { 
			    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, Condition: {equipment["Condition"]}");
		    }
	    }

	    public static void showEquipmentWithCondition(Condition condition) {
		    List<Dictionary<string, object>> allequipment = Program.db.GetRecordsByField("Equipment", "Condition", (string)condition);
		    foreach (Dictionary<string, object> equipment in allEquipment) {
			    WriteLine($"{equipment["Type"]}, property number: {equipment["PropertyNumber"]}, in Room: {equipment["RoomId"]}");
		    }
	    }
    }

    internal static class Program {
	    int MAX_ROOM_CAPACITY = 6;

	    public static void registerNewEquipment() {
		    Write("type of equipment: ");
		    string type = ReadLine();
		    Write("what condition is the equipment in: ");
		    string condition = ReadLine();
		    string partnumber;
		    string propertynumber;

		    Equipment newEquipment = new Equipment(type, partnumber, propertynumber, condition);
		    EquipmentManager.addEquipmentToDB(newEquipment);
	    }

	    public static Room chooseRoom() {
		    List<Dictionary<string, object>> allRooms = Program.db.GetAllRecords("Rooms");
		    
		    WriteLine("avialable rooms: ");
		    for (int i=0; i<allRooms.Count; i++) {
			    Dictionary<string, object> room = allRooms[i];
			    WriteLine($"{i}: Room number {room["RoomNumber"]} with block ID: {room["BlockId"]}");
		    }

		    Write("specify a room from the above list: ");
		    int roomIndex = int.Parse(ReadLine());
		    Dictionary<string, object> specifiedRoomDict = allRooms[roomIndex];

		    Room specifiedRoom = Room.FromDictionary(specifiedRoomDict); // implementing FromDictionary method in Room class
		    return specifiedRoom;
	    }

	    public static Equipment chooseEquipment() {
		    WriteLine("Specify the type of equipment you want: ");
		    string type = ReadLine();
		    List<Dictionary<string, object>> allEquipment = Program.db.GetRecordsByField("Equipment", "RoomId", DBNull); // needs to be changed to select all equipment that are not assigned to any room and are of specified type and intact
		    WriteLine("not assigned equipment: ");
		    for (int i=0; i<allEquipment.Count; i++) {
			    Dictionary<string, object> equipment = allEquipment[i];
			    WriteLine($"Equipment {i}: property number: {equipment["PropertyNumber"]}, Type: {equipment["Type"]} ");
		    }
		    Write("Specify an equipment from the above list: ");
		    int equipmentIndex = int.Parse(ReadLine());
		    Dictionary<string, object> specifiedEquipmentDict = allEquipment[equipmentIndex];

		    Equipment specifiedEquipment = Equipment.FromDictionary(specifiedEquipmentDict);
		    return specifiedEquipment;
	    }

	    public static void assignEquipmentToRoom() {
		    Room room = chooseRoom();
		    Equipment equipment = chooseEquipment();
		    EquipmentManager.assignEquipmentToRoom(equipment._propertyNumber, room.Id);
	    }
	    
	    public static void assignEquipmentToStudent() {
		    Equipment equipment = chooseEquipment();
		    Write("Enter Student's social number");
		    string socialNumber = ReadLine();
		    EquipmentManager.assignEquipmentToStudent(equipment._propertyNumber, socialNumber);
	    }

	    public static void exchangeEquipmentBetweenRooms() {
		    Write("Choose Destination Room: ");
		    int destinationRoomId = chooseRoom().Id;

		    Write("enter property number of equipment you want to transfer to the specified room: ");
		    string propertyNumber = ReadLine();

		    EquipmentManager.exchangeEquipmentBetweenRooms(propertyNumber, destinationRoomId);
	    }

	    public static void changeStudentEquipment(string socialNumber, string oldPropertyNumber) { // the two parameters are for reusability
		    Write("specify new equipment you want to assign to this student: ");
		    Equipment equipment = chooseEquipment();
		    string newPropertyNumber = equipment._propertyNumber;

		    EquipmentManager.changeStudentEquipment(oldPropertyNumber, newPropertyNumber, socialNumber);
	    }

	    public static void showAssignedEquipmentToRooms() {
		    List<Dictionary<string, object>> allRooms = Program.db.GetAllRecords("Rooms");
		    foreach (Dictionary<string, obejct> room in allRooms) {
			    WriteLine($"All equipment in Room :{room["RoomNumber"]}, located in Floor: {room["FloorNumber"]} in Block {room["BlockId"]}");
			    EquipmentManager.equipmentAssignedToRoom(room["Id"].ToInt32());
			    WriteLine("-----------------");
		    }
	    }

	    public static void showAssignedEquipmentToStudents() {
		    List<Dictionary<string, object>> allStudents = Program.db.GetAllRecords("Students");
		    foreach (Dictionary<string, object> student in allStudents) {
			    WriteLine($"All Equipment that belong to student {student["FullName"]}, in Room {student["RoomId"]}, in Block {student["BlockId"]}");
			    EquipmentManager.equipmentAssignedToStudent(student["Id"].ToInt32());
			    WriteLine("-----------------");
		    }
	    }

	    public static void showBrokenEquipment() {
		    WriteLine("List of broken equipment: ");
		    EquipmentManager.showEquipmentWithCondition(Condition.Broken);
	    }

	    public static void showRepairingEquipment() {
		    WriteLine("List of equipment that are being repaired: ");
		    EquipmentManager.showEquipmentWithCondition(Condition.Repairing);
	    }

	    public static void showEmptyRooms() {
		    List<Dictionary<string, object>> emptyRooms = Program.db.GetRecordsByField("Rooms", "Capacity", 0);
		    foreach (Dictionary<string, object> room in emptyRooms) {
			    WriteLine($"Room: {room["RoomNumber"]}, located in Floor: {room["FloorNumber"]}, in Block: {room["BlockId"]}");
		    }
	    }

	    public static void showFullRooms() {
		    List<Dictionary<string, object>> fullRooms = Program.db.GetRecordsByField("Rooms", "Capacity", MAX_ROOM_CAPACITY);
		    foreach (Dictionary<string, object> room in fullRooms) {
			    WriteLine($"Room: {room["RoomNumber"]}, located in Floor: {room["FloorNumber"]}, in Block: {room["BlockId"]}");
		    }
	    }

	    public static void showBlockRemainingCapacity() {
		    List<Dictionary<string, object>> allBlocks = Program.db.GetAllRecords("Blocks");
		    foreach (Dictionary<string, object> block in allBlocks) {
			    List<Dictionary<string, object>> blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", block["Id"].ToInt32());
			    foreach(Dictionary<string, object> room in blockRooms) {
				    int capacity = MAX_ROOM_CAPACITY - room["Capacity"].ToInt32();
				    WriteLine($"Room: {room["RoomNumber"]}, in Block: {block["Name"]}, in Dormitory: {block["DormitoryId"]}, has {capacity} remaining space.");
			    }
		    }
	    }

	    public static void showOverallAccommodationReport() {
		    List<Dictionary<string, object>> allDorms = Program.db.GetAllRecords("Dormitories");
		    foreach (Dictionary<string, object> dorm in allDorms) {
			    List<Dictionary<string, object>> dormBlocks = Program.db.GetRecordsByField("Blocks", "DormitoryId", dorm["Id"].ToInt32());
			    foreach (Dictionary<string, object> block in dormBlocks) {
				    List<Dictionary<string, object>> blockRooms = Program.db.GetRecordsByField("Rooms", "BlockId", block["Id"].ToInt32());
				    foreach (Dictionary<string, object> room in blockRooms) {
					    List<Dictionary<string, object>> roomStudents = Program.db.GetRecordsByField("Students", "RoomId", room["Id"].ToInt32());
					    foreach (Dictionary<string, object> student in roomStudents) {
						    WriteLine($"Student: {student["FullName"].ToString()} with Student ID: {student["StudentID"]}, in Room: {room["Name"]}, in Block: {block["Name"]}, in Dormitory: {dorm["Name"]}");
					    }
					    WriteLine("-------------");
				    }
			    }
		    }
	    }

    }

}
