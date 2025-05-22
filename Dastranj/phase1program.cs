using static System.Console;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FinalProject
{
  enum Condition {
    Intact,
    Broken,
    Reparing,
  }
  
  class Equipment {
    private string _type;
    private string _partNumber;
    private string _propertyNumber;
    private Condition _condition;
    private Room _room;
    private Student _owner;
    
    public Equipment(string type, string partNumber, string propertyNumber, Condition condition, Room room, Student owner) {
      _type = type;
      _partNumber = partNumber;
      _propertyNumber = propertyNumber;
      _condition = condition;
      _room = room;
      _owner = owner;
    }
  }
  
  class Person {
    protected string _fullName;
    protected string _socialNumber;
    protected string _phoneNumber;
    protected string _address;
    
    public Person(string fullName, string socialNumber, string phoneNumber, string address) {
      _fullName = fullName;
      _socialNumber = socialNumber;
      _phoneNumber = phoneNumber;
      _address = address;
    }
  }
  
  class DormitorySupervisor:Person {
    private string _position;
    private Dormitory _dormitory;
    
    public DormitorySupervisor(string position, Dormitory dormitory) {
      _position = position;
      _dormitory = dormitory;
    }
  }

}
