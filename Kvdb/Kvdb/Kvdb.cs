
using Newtonsoft.Json.Linq;
using System.IO;
using static System.Net.WebRequestMethods;
using System;
using System.Text;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using System.Reflection;
using File = System.IO.File;
using System.Globalization;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using System.Text.Unicode;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using NUnit.Compatibility;

namespace Kvdb
{
    // [active key_l key value 10] ???
    // Формат хранения - [{ke_l} {key} {value}]
    
    // класс базы данных
    public class db
    {
        public string name; // имя базы данных
        public string path; // путь до файл с базой ланных
        public int num; // номер бд
        public string mode_path; // путть до файла со словарем начальных индексов чепочек каждого отдельного хеша
        long lastind; // индекс последней запсиис хеша
        public string module_path;//путь до файла с модулем хеширования
        bool itwas; // есть или нет записи по данному хешу
        int module = 2; // модкль хеширования
        int active=0; // колчичество активных запсией в хеш-цепочке
        int disable=0; // количество неактивных запсиийе в хеш-цепочке

        // метод получает модуль хеширования из файла
        public void upddatemodule()
        {
            using (StreamReader reader = new StreamReader(module_path))
            {
                string line = reader.ReadLine();
                module = Int32.Parse(line);
            }
        }
        // метод получает стабильный хеш от строчки
        public  int GetStableHashCode( string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                
                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
        // метод получает стабильных хеш от строчки по модулю хеширования
        public int hs(string str)
        {
            return Math.Abs(GetStableHashCode(str)) % module;
        }
        // метод читает стрчоик при помощи filestream
        public string readline(FileStream fs)
        {
            string ans;
            int count=0;
            byte[] newline_byte = Encoding.UTF8.GetBytes(Environment.NewLine);
            int l = newline_byte.Length;
            int go;
            byte[] buffer = new byte[l];
            bool flag = true;
            // читаем файл пока не встретим символ перевода строки
            while (true)
            {
                go = fs.Read(buffer, 0, l);
                if(go<l)
                {
                    break;
                }
                flag = true;
                for(int i=0; i<l; i++)
                {
                    if(buffer[i] != newline_byte[i])
                    {
                        flag = false;
                    }
                }    
                if(flag)
                {
                    break;
                }
                try
                {
                    fs.Seek(-l + 1, SeekOrigin.Current);
                }
                catch(System.IO.IOException)
                {
                    return "";
                }
                count++;
            }
            try
            {
                fs.Seek(-count - l, SeekOrigin.Current);
            }
            catch (System.IO.IOException)
            {
                return "";
            }
            // возвращаемся назад и читаем строчку
            byte[] buffer1 = new byte[count];
            fs.Read(buffer1, 0, buffer1.Length);
            ans = Encoding.UTF8.GetString(buffer1);
            fs.Seek(l, SeekOrigin.Current);
            return ans;

        }
        //метод записывает строчку при помощи filestream c переводом строки
        public void wrightline(FileStream fs,  string str)
        {
            // кодируем и зааписываем строчку
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            for(int i=0; i<buffer.Length; i++)
            {
                fs.WriteByte(buffer[i]);
                fs.Flush();
            }
            // кодируем и записываем перевод строки
            byte[] newline_byte = Encoding.UTF8.GetBytes(Environment.NewLine);
            for(int i=0; i<newline_byte.Length; i++)
            {
                fs.WriteByte(newline_byte[i]);
                fs.Flush();
            }
        }
        //метод записывает строчку при помощи filestream без перевода строки
        public void wright(FileStream fs, string str)
        {
            // кодируем и записываем строчку
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            for (int i = 0; i < buffer.Length; i++)
            {
                fs.WriteByte(buffer[i]);
                fs.Flush();
            }
            
        }
        // метод расзделяет запись на составляющие
        public string[] parse(string str)
        {
            string[] mass = new string[3];
            mass[0] = str.Substring(0, 1);
            mass[1] = str.Substring(2, 10);
            mass[2] = str.Substring(13);
            return mass;
        }
        // метод переводит число в 10 цифрный формат с лидирующими нуями
        public string adopt(int s)
        {
            string num = $"{s}";
            if(num.Length<10)
            {
                while(num.Length!=10)
                {
                    num = "0" + num;
                }
            }
            //Console.WriteLine($"From {s} got {num}");
            return num;

        }
        // метод выделяют число из формата с лидирующими нулями
        public string readopt(string s)
        {
            int ind = s.Length-1;
            for(int i = 0; i < s.Length; i++)
            {
                if (s[i]!='0')
                {
                    ind = i;
                    break;
                }
            }
            //Console.WriteLine($"From1 {s} got {s.Substring(ind)}");
            return s.Substring(ind);

        }
        // метод удаляет строчку из файла
        public void delete (FileStream fstream)
        {
            // сдвинаем все байты на длину строки в байтах влево
            string temp = readline(fstream);
            //Console.WriteLine($"Del {temp}");
            byte[] temp_bytes = Encoding.UTF8.GetBytes(temp);
            int len = temp_bytes.Length; // длина строки
            byte[] newline_byte = Encoding.UTF8.GetBytes(Environment.NewLine);
            int nl = newline_byte.Length; // длина символ перевода строки
            int l = len + nl + 1;
            byte[] buffer = new byte[l];
            fstream.Seek(-len-nl, SeekOrigin.Current);
            int a = fstream.Read(buffer, 0, buffer.Length);
            string textFromFile = Encoding.UTF8.GetString(buffer);
            //Console.WriteLine($"{textFromFile}");
            if (a == l) // проверяем нужно ли что-то сдвигать
            {
                while (a == l)
                {
                    fstream.Seek(-l, SeekOrigin.Current);
                    fstream.WriteByte(buffer[l - 1]);
                    fstream.Flush();
                    a = fstream.Read(buffer, 0, buffer.Length);
                }
                //удалим последние символы, обрезав поток
                fstream.SetLength(fstream.Length - l + 1);
                fstream.Flush();
                
            }
            else
            {
                //удалим последние символы, обрезав поток
                fstream.SetLength(fstream.Length - l + 1);
                fstream.Flush();
               
            }
        }
        // метод перестраивает хеш таблицу
        public void rebuild(int code, FileStream fs)
        {
            //Console.WriteLine($"REbuild! {code}");

            // меняем модуль хешированияя если нужно и сразу записываем его в файл
            if(code==0)
            {
                module = 2 * module - 1;
                using (StreamWriter writer = new StreamWriter(module_path, false))
                {
                    writer.WriteLine($"{module}");
                }
            }
            // читаем по очереди строчки и удаляем неактивные записис
            byte[] newline_byte = Encoding.UTF8.GetBytes(Environment.NewLine);
            int nl = newline_byte.Length;
            while (true)
            {
                 string temp = readline(fs);
                 if (temp == "")
                 {
                       break;
                 }
                 string[] go = parse(temp);
                 if (go[0]=="0")
                 {
                    //long pos = fs.Position;
                    //int copypos = unchecked((int)pos);
                    fs.Seek(-Encoding.UTF8.GetBytes(temp).Length - nl, SeekOrigin.Current);
                    delete(fs);
                    fs.Seek(0, SeekOrigin.Begin);
                 }
            }
            File.Delete(mode_path);
            var s = File.Create(mode_path);
            s.Close();
            fs.Seek(0, SeekOrigin.Begin);
            // пересчитываем укзаатель в хеш-цепочке
            while (true)
            {
                    // полуем ключь из очереной записи и посчитаем хеш
                    long pos = fs.Position;
                    int copypos = unchecked((int)pos);
                    string temp = readline(fs);
                    //Console.WriteLine($"Entered {temp}");
                    string orig = temp;
                    if (temp == "")
                    {
                        break;
                    }
                    string[] go = parse(temp);
                    temp = go[2];
                    int key_l = Int32.Parse(temp.Split(' ')[0]);
                    string l = $"{key_l}";
                    string go_key = temp.Substring(l.Length + 1, key_l);
                    int got = get_mode(hs(go_key));
                    if(got==-1) // если хеш новый то потсавим укзаатель на 0 и добавим новый хеш в словарь
                    {
                        
                        long p = fs.Position;
                        int copyp = unchecked((int)pos);
                        //Console.WriteLine($"added {hs(go_key)} {copypos}");
                        wright_mode(hs(go_key), copypos);
                        fs.Seek(-Encoding.UTF8.GetBytes(orig).Length - nl + 2, SeekOrigin.Current);
                        wright(fs, $"0000000000");
                        fs.Seek(copyp, SeekOrigin.Begin);
                    }
                    else // если хеш уже встречался то найдем последнюю запись в цепочке и поменям указатель в ней
                    {
                        //Console.WriteLine($"Go for {go_key} has {hs(go_key)}");
                        long togo = fs.Position;
                        int copytogo = unchecked((int)togo);
                        fs.Seek(got, SeekOrigin.Begin);
                        bool g = true;
                        while(g)
                        {
                            temp = readline(fs);
                            //Console.WriteLine($"go {temp}");
                            if (temp == "")
                            {
                                break;
                            }
                            go = parse(temp);
                           // Console.WriteLine(go[2]);
                            if (Int32.Parse(readopt(go[1]))==0)
                            {
                                //Console.WriteLine("END");
                                fs.Seek(-Encoding.UTF8.GetBytes(temp).Length - nl+2, SeekOrigin.Current);
                                wright(fs, $"{adopt(copypos)}");
                                fs.Seek(copytogo, SeekOrigin.Begin);
                                break;
                            }
                            else
                            {
                                fs.Seek(Int32.Parse(readopt(go[1])), SeekOrigin.Begin);
                            }

                        }
                    }
                
            }

        }
        // метод проверяет надо ли перестроить хещ таблицу
        public void check(FileStream fs)
        {
            if (num != -1)
            {
                if (disable >= active)
                {
                    rebuild(1, fs);
                }
                if (active >= module)
                {
                    rebuild(0, fs);
                }
            }
            
        }
        // метод проходиться по хеш-цепочке и заполняет параметры lastind, active, disable
        public bool gothrow(int hash)
        {
            // берем стартовый индекс из словаря хешей
            int start = get_mode(hash);
            if(start==-1)
            {
                itwas = false;
                return false;
            }
            // побежали по списку и все пересчитали
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                fs.Seek(start, SeekOrigin.Begin);
                while (true)
                {
                    //читаем строчку
                    lastind = fs.Position;
                    string temp = readline(fs);
                    itwas = true;
                    if (temp == "")
                    {
                        break;
                    }
                    itwas = true;
                    string[] go = parse(temp);
                    temp = go[2];
                    int move = Int32.Parse(readopt(go[1])) - 1;
                    if (move != -1)
                    {
                        // иедем дальше по укзаателю
                        fs.Seek(move + 1, SeekOrigin.Begin);
                    }
                    else
                    {
                        return false;
                    }
                }
                
            }
            return true;
        }
        // метод получает значение по ключу
        public string get(string key)
        {
            // посчитаем хеш, если такого хеша у нас нет, то сразу вернем ""
            //Console.WriteLine($"get {key}");
            int key_l = 0;
            int hash = hs(key);
            int start = get_mode(hash);
            if(start==-1)
            {
                return "";
            }
            //Console.WriteLine($"Start is {start}");
            active = 0;
            disable = 0;
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                itwas = false;
                fs.Seek(start, SeekOrigin.Begin);
                while (true)
                {
                    // прочитаем строчку
                    lastind =  fs.Position;
                    string temp = readline(fs);

                    if (temp == "")
                    {
                        break;
                    }
                    itwas = true;
                    string[] go = parse(temp);
                    //Console.WriteLine(go[0]);
                    //Console.WriteLine(go[1]);
                    //Console.WriteLine(go[2]);
                    temp = go[2];
                    int move = Int32.Parse(readopt(go[1])) - 1;

                    if (go[0] == "1") // если активна  то сравним ключи, либо нашли либо пойдем дальше по цепочке
                    {
                        active++;
                        key_l = Int32.Parse(temp.Split(' ')[0]);
                        string l = $"{key_l}";
                        string go_key = temp.Substring(l.Length + 1, key_l);
                       // Console.WriteLine(go_key);
                        if (go_key == key)
                        {
                           // Console.WriteLine("end");
                            return temp.Substring(l.Length + key_l + 2);
                        }
                        else
                        {
                            if (move != -1)
                            {
                                //Console.WriteLine($"Moved to {move + 1}");
                                fs.Seek(move+1, SeekOrigin.Begin);
                            }
                            else
                            { 
                                return "";
                            }
                        }
                    }
                    else // если неактивна то просто идем дальше
                    {
                        disable++;
                        if (move != -1)
                        {
                            fs.Seek(move+1, SeekOrigin.Begin);
                        }
                        else
                        {
                            return "";
                        }
                    }

                }
                return "";
            }

        }
        
        //метод получает первый индекс хеш-цепочки по ключу (из словаря)
        public int get_mode(int key_num)
        {
            string key = $"{key_num}";
            //Console.WriteLine("get_mode");
            int nl = Encoding.UTF8.GetBytes(Environment.NewLine).Length;
           
            using (FileStream fs = new FileStream(mode_path, FileMode.OpenOrCreate))
            {
               
               //readline(fs);
                //Console.WriteLine("!!!");
               while(true)
               { 
                    
                    string temp = readline(fs);
                    //Console.WriteLine("!!!");
                    if (temp=="")
                    {
                        break;
                    }
                   
                    string[] go = temp.Split(' ');
                    if (go[0]==key)
                    {
                        return Int32.Parse(go[1]);
                    }
                   
               }
                    
             }
           // Console.WriteLine("get_mode ended");
             return -1;
        }
        //метод дописывает записаь в словарь первых индексов хешей
        public void wright_mode(int key, int value)
        {
            //Console.WriteLine("wright_mode");
            int nl = Encoding.UTF8.GetBytes(Environment.NewLine).Length;
            
            using (FileStream fs = new FileStream(mode_path, FileMode.OpenOrCreate))
            {
                
                fs.Seek(0, SeekOrigin.End);
                wrightline(fs, $"{key} {value}");
            }
            //Console.WriteLine("wright_mode ended");
        }


        // метод удаляет значение по ключу
        public string remove(string key)
        {
            byte[] newline_byte = Encoding.UTF8.GetBytes(Environment.NewLine);
            string newline = Environment.NewLine;
            string value = get(key);
            int key_l = key.Length;
            if (value != "")
            {
                using (FileStream fstream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    //Console.WriteLine("!!!");
                    fstream.Seek(lastind, SeekOrigin.Begin);
                    wright(fstream, "0");
                }

                return value;
            }
            else
            {
                return "";
            }
        }
        //метод добавляет значение по ключу
        public string put(string key, string value)
        {
            byte[] newline_byte = Encoding.UTF8.GetBytes(Environment.NewLine);
            string newline = Environment.NewLine;
            int key_l = key.Length;
            string oldvalue = get(key);
            
            int len;
           // Console.WriteLine("!!!");
            //удаляем старое значение если оно было
            if (oldvalue != "")
            {
                remove(key);
            }
            //Console.WriteLine("!!!");
            //записываем новое
            gothrow(hs(key));
            using (FileStream fstream = new FileStream(path, FileMode.OpenOrCreate))
            {
                //Console.WriteLine("!!!");
                fstream.Seek(lastind+2, SeekOrigin.Begin);
                len = unchecked((int)fstream.Length);
                //Console.WriteLine("!!!");
                // если это не первый элемент в хеш-цепочке - поменяем укзаатель у последнего
                if (itwas)
                {
                    //Console.WriteLine($"Add ss {len}");
                    wright(fstream, adopt(len));
                }
                fstream.Seek(0, SeekOrigin.End);
                // если это нвый хеш - добавим его в словарь
                //Console.WriteLine("!!!");
                if (get_mode(hs(key)) == -1)
                {
                    //Console.WriteLine($"wright {hs(key)}, {fstream.Length}");
                    wright_mode(hs(key), unchecked((int)fstream.Length));
                }
                //кодируем новую запись
                string s = $"1 0000000000 {key_l} {key} {value}";
                byte[] buffer = Encoding.UTF8.GetBytes(s);
                // запись массива байтов в файл
                for(int i=0; i<buffer.Length; i++)
                {
                    fstream.WriteByte(buffer[i]);
                }
                //записываем в файл символ перевода строки
                string s1 = newline;
                byte[] buffer1 = Encoding.UTF8.GetBytes(s1);
               
                for (int i = 0; i < buffer1.Length; i++)
                {
                    fstream.WriteByte(buffer1[i]);
                }
                /*
                using (FileStream fstream1 = new FileStream(mode_path, FileMode.OpenOrCreate))
                {
                    fstream1.Seek(-10-newline_byte.Length, SeekOrigin.End);
                    wrightline(fstream1 , adopt(len+Encoding.UTF8.GetBytes(s).Length+newline_byte.Length));
                }*/

            }
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                check(fs);
            }
            return oldvalue;
        }
        

    }

    public class Kvdb
    {
        //создание служебной базы данных быз данных
        public static db admin = new db();
        
       // метод распознает команду
        public static List<string> parse(string enter)
        {
            string enter_copy = enter;
            List<string> items = new List<string>();
            List<string> fin_items = new List<string>();
            while (true)
            {
                string item;
                int match = 0, match1 = 0, match2 = 0, found_smth = 0;
                for (int i = 0; i < enter.Length; i++)
                {
                    if (enter[i] == '\"' && match == 0)
                    {
                        found_smth++;
                        match++;
                        match1 = i;
                    }
                    else if (enter[i] == '\"' && match == 1)
                    {
                        match2 = i;
                        match = 0;
                        item = enter.Substring(match1 + 1, match2 - match1 - 1);
                        items.Add(item);
                        //Console.WriteLine("Path found: "+path);
                        if (match2 != enter.Length - 1)
                        {
                            enter = enter.Substring(0, match1) + " " + enter.Substring(match2 + 1);
                        }
                        else
                        {
                            enter = enter.Substring(0, match1);
                        }
                        //Console.WriteLine("New Enter: " + enter);
                        break;
                    }
                }
                if (found_smth == 0)
                {
                    break;
                }
            }
            string[] not_puzzeled_paths = enter.Split(' ');
            for (int i = 0; i < not_puzzeled_paths.Length; i++)
            {
                if (not_puzzeled_paths[i] != "")
                {

                    items.Add(not_puzzeled_paths[i]);
                    //Console.WriteLine($"Path found + [{not_puzzeled_paths[i]}]");

                }
            }

            string sub;

            for (int i = 0; i < enter_copy.Length; i++)
            {
                for (int j = i; j < enter_copy.Length; j++)
                {
                    sub = enter_copy.Substring(i, j - i + 1);
                    if (items.Contains(sub))
                    {
                        //Console.WriteLine($"PAth added {sub}");
                        fin_items.Add(sub);
                        items.Remove(sub);
                    }
                }
            }
            return fin_items;
        }
        //метод создает базу данных
        public static db CreateDb(string name)
        {
            //Console.WriteLine($"Create db {name}");
            db db = new db();
            initialize(db, name, Int32.Parse(admin.get("Count")));
            FileStream cr = File.Create($"../../../data/{db.num}.txt");
            cr.Close();
            FileStream cr1 = File.Create($"../../../data/{db.num}_mode.txt");
            cr1.Close();
            FileStream cr2 = File.Create($"../../../data/{db.num}_module.txt");
            cr2.Close();
            File.WriteAllText($"../../../data/{db.num}_module.txt", "2");
            admin.put(db.name, $"{db.num}");
            return db;

        }
        //метод инициализирует базу данных
        public static void initialize(db db, string name, int num)
        {
            //Console.WriteLine($"Initialize db {name}");
            db.name = name;
            db.num = num;
            db.path = $"../../../data/{num}.txt";
            db.mode_path = $"../../../data/{num}_mode.txt";
            admin.put("Count", $"{Int32.Parse(admin.get("Count")) + 1}");
            using (FileStream fs = new FileStream(admin.path, FileMode.OpenOrCreate))
            {
                admin.rebuild(1, fs);
            }
        }
        public static void Main(string[] args)
        {
            //инициализация
            //Console.WriteLine("Preparing...");
            //создание стартовой базы данных
            db base_db = new db();
            base_db.name = "base";
            base_db.num = 0;
            base_db.path = $"../../../data/0.txt";
            base_db.mode_path = $"../../../data/0_mode.txt";
            base_db.module_path = $"../../../data/0_module.txt";
            base_db.upddatemodule();
            admin.path = $"../../../data/db_list.txt";
            admin.mode_path = $"../../../data/db_list_mode.txt";
            admin.num = -1;
            //admin.put("base", "0");
            //admin.put("Count", "1");

            //Console.WriteLine("Command!");
            //работа програмы
            string enter = "";

            if (!(args.Length > 0))
            {
                enter = Console.ReadLine();
            }
            while (enter != "end")
            {
                List<string> command = new List<string>();
                //чтение команды
                if(args.Length>0)
                { 
                    for(int i = 0; i < args.Length; i++)
                    {
                        command.Add(args[i]);
                    }
                }
                else
                {
                    command = parse(enter);
                }
                
                //printList(command);
                if (command[0] == "put")
                {
                    if (command.Count == 3) //пользователь обратился к стартовой бд
                    {
                        Console.WriteLine(base_db.put(command[1], command[2]));
                    }
                    else if (command.Count == 4) // пользователь обраился к своей бд



                    {
                        if (admin.get(command[1])!="") // если бд существует - проводим поперацию с ней
                        {
                            db db = new db();
                            db.name = command[1];
                            db.num = Int32.Parse(admin.get(command[1]));
                            db.path = $"../../../data/{db.num}.txt";
                            db.mode_path = $"../../../data/{db.num}_mode.txt";
                            db.module_path = $"../../../data/{db.num}_module.txt";
                            db.upddatemodule();
                            Console.WriteLine(db.put(command[2], command[3]));
                        }
                        else //иначе создаем ее и проводим операцию с новой
                        {
                            db new_db = CreateDb(command[1]);
                            Console.WriteLine(new_db.put(command[2], command[3]));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Команда некорректна {enter}");
                    }
                }
                else if (command[0] == "get")
                {
                    if (command.Count == 2) //польователь обратился к стартовой бд
                    {
                        Console.WriteLine(base_db.get(command[1]));
                    }
                    else if (command.Count == 3) // пользователь обратился к своей бд
                    {
                        if (admin.get(command[1]) != "") // если бд существует - проводим операцию с ней
                        {
                            db db = new db();
                            db.name = command[1];
                            db.num = Int32.Parse(admin.get(command[1]));
                            db.path = $"../../../data/{db.num}.txt";
                            db.mode_path = $"../../../data/{db.num}_mode.txt";
                            db.module_path = $"../../../data/{db.num}_module.txt";
                            db.upddatemodule();
                            Console.WriteLine(db.get(command[2]));
                        }
                        else //иначе содаем ее и проводим операцию с новой
                        {
                            Console.WriteLine($"Базы данных {command[1]} не существует");
                            Environment.Exit(-1);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Команда некорректна {enter}");
                    }
                }
                else if (command[0] == "remove")
                {
                    if (command.Count == 2) // пользователь обратился к стартовой бд
                    {
                        Console.WriteLine(base_db.remove(command[1]));
                    }
                    else if (command.Count == 3) // пользователь обратился к своей бд
                    {
                        if (admin.get(command[1]) != "") // если бд существует - проводим опрерацию с ней
                        {
                            db db = new db();
                            db.name = command[1];
                            db.num = Int32.Parse(admin.get(command[1]));
                            db.path = $"../../../data/{db.num}.txt";
                            db.mode_path = $"../../../data/{db.num}_mode.txt";
                            db.module_path = $"../../../data/{db.num}_module.txt";
                            db.upddatemodule();
                            Console.WriteLine(db.remove(command[2]));
                        }
                        else // иначе создаем ее и провлим операцию с новой
                        {
                            Console.WriteLine($"Базы данных {command[1]} не существует");
                            Environment.Exit(-1);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Команда некорректна {enter}");
                    }
                }
                else
                {
                    Console.WriteLine($"Комнада некореектна {enter}");
                }

                if(args.Length>0)
                {
                    enter = "end";
                }
                else 
                {
                    enter = Console.ReadLine();
                }
                
                
            }

        }
    }
}
