using System;
using static System.Console;
namespace dormitory
{
    class Khabgah
    {
        string name, address, responsible;
        string[] bolook=new string[10];
        int zarfiat,count;
        public Khabgah(string n, string a, int z,string m)
        {
            name = n;
            address = a;
            zarfiat = z;
            responsible = m;
            count = 0;
        }
        public Khabgah(string b)
        {
            bolook[count] = b;
            count++;
        }
    }
    class Bolook
    {
        string name, responsible, khabgah;
        int tabaghat, otagh;
        string[] otaghha;
        public Bolook(string kh, string n, int t, int o,string m)
        {
            khabgah = kh;
            name = n;
            tabaghat = t;
            otagh = o;
            responsible = m;
        }
    }
    class Otagh
    {
        string[] taghizat = new string[5];
        string[] students = new string[5];
        int tcount, scount;
        string bolook;
        int shomare, tabaghat, zarfiat;
        public Otagh(int sh,int t,int z,string b)
        {
            shomare = sh;
            tabaghat = t;
            zarfiat = z;
            bolook = b;
            tcount = 0;
            scount = 0;
        }
        public Otagh(string ta, int n)
        {
            taghizat[tcount] = ta;
            tcount++;

        }
        public Otagh(string s)
        {
            students[scount] = s;
            scount++;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {

        }
    }
}