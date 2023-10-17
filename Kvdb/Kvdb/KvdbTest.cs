using NUnit.Framework;
using System.Text;
using static NUnit.Framework.Assert;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using NUnit.Framework.Internal.Execution;

namespace Kvdb;

public class Tests
{
    private readonly TextReader _standartIn = Console.In;
    private readonly TextWriter _standartOut = Console.Out;
    private StringWriter _stringWriter = new StringWriter();

    [SetUp]
    public void Setup()
    {
        var stringWriter = new StringWriter();
        _stringWriter = stringWriter;
        Console.SetOut(_stringWriter);
    }

    [TearDown]
    public void TearDown()
    {
        Console.SetIn(_standartIn);
        Console.SetOut(_standartOut);
        _stringWriter.Close();
    }

    
    private void AssertOut(String expected)
    {
        That(_stringWriter.ToString().TrimEnd(Environment.NewLine.ToCharArray()), Is.EqualTo(expected));
    }
    [Test]
    public void PutGetRemoveTest()
    {
        db db = new db();
        db.name = "test";
        db.num = -1;
        db.path = $"../../../data_t/test.txt";
        db.mode_path = $"../../../data_t/test_mode.txt";
        db.module_path = $"../../../data_t/test_module.txt";
        File.WriteAllText(db.path, string.Empty);
        File.WriteAllText(db.mode_path, string.Empty);
        File.WriteAllText(db.module_path, string.Empty);
        File.WriteAllText(db.module_path, "2");

        string s = db.get("bloodhound gang");
        That(s, Is.EqualTo(""));

        s = db.put("bloodhound gang", "Foxtrot Uniform Charlie Kilo");
        That(s, Is.EqualTo(""));

        s = db.put("metallica", "Unforgiven");
        That(s, Is.EqualTo(""));

        s = db.put("fall out boy", "Memories");
        That(s, Is.EqualTo(""));

        s = db.put("linkin park", "burn it down");
        That(s, Is.EqualTo(""));

        s = db.get("metallica");
        That(s, Is.EqualTo("Unforgiven"));

        s = db.get("fall out boy");
        That(s, Is.EqualTo("Memories"));

        s = db.get("linkin park");
        That(s, Is.EqualTo("burn it down"));

        s = db.get("bloodhound gang");
        That(s, Is.EqualTo("Foxtrot Uniform Charlie Kilo"));

        s = db.remove("bloodhound gang");
        That(s, Is.EqualTo("Foxtrot Uniform Charlie Kilo"));

        s = db.get("bloodhound gang");
        That(s, Is.EqualTo(""));

        s = db.remove("fall out boy");
        That(s, Is.EqualTo("Memories"));

        s = db.get("fall out boy");
        That(s, Is.EqualTo(""));

        s = db.get("linkin park");
        That(s, Is.EqualTo("burn it down"));

        s = db.put("linkin park", "In the end");
        That(s, Is.EqualTo("burn it down"));

        s = db.put("metallica", "Ride the lightning");
        That(s, Is.EqualTo("Unforgiven"));

        s = db.get("linkin park");
        That(s, Is.EqualTo("In the end"));

        s = db.get("metallica");
        That(s, Is.EqualTo("Ride the lightning"));

        File.WriteAllText(db.path, string.Empty);
    }

    [Test]
    public void dbManageTest()
    {
        db admin = new db();
        admin.name = "";
        admin.num = -1;
        admin.path = $"../../../data/db_list.txt";
        admin.mode_path = $"../../../data/db_list.txt";
        DirectoryInfo dir = new DirectoryInfo("../../../data/");
        foreach (FileInfo file in dir.GetFiles())
        {
            file.Delete();
        }
        FileStream cr = File.Create($"../../../data/0.txt");
        cr.Close();
        cr = File.Create($"../../../data/db_list.txt");
        cr.Close();
        cr = File.Create($"../../../data/db_list_mode.txt");
        cr.Close();
        cr = File.Create($"../../../data/0_mode.txt");
        cr.Close();
        cr = File.Create($"../../../data/0_module.txt");
        cr.Close();
        db test_db = new db();
        test_db.path = $"../../../data/db_list.txt";
        test_db.mode_path = $"../../../data/db_list_mode.txt";
        test_db.num = -1;
        test_db.put("base", "0");
        test_db.put("Count", "1");
        File.AppendAllText($"../../../data/0_module.txt","2");
        
        string ans = "";
        string command = "put music queen YAENTIKPOLOSKUN";
        string[] args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans  +"";
        AssertOut(ans);
        
        command = "get music queen";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + Environment.NewLine + "YAENTIKPOLOSKUN";
        AssertOut(ans);

        command = "put smeshariki krosh rabbit";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans +  "";
        AssertOut(ans);
        ans = ans + Environment.NewLine + "";

        command = "get music queen";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + Environment.NewLine + "YAENTIKPOLOSKUN";
        AssertOut(ans);

        command = "get smeshariki krosh";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + Environment.NewLine + "rabbit" ;
        AssertOut(ans);

        command = "put smeshariki ezik ezik";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans +"";
        AssertOut(ans);
        ans = ans + Environment.NewLine + "";

        command = "get smeshariki ezik";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + Environment.NewLine+"ezik";
        AssertOut(ans);


        command = "put music \"linkin_park\" \"in_the_end\"";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + "";
        AssertOut(ans);
        ans = ans + Environment.NewLine + "";

        command = "get music queen";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + Environment.NewLine + "YAENTIKPOLOSKUN";
        AssertOut(ans);

        command = "get music \"linkin_park\"";
        args = command.Split(' ');
        Kvdb.Main(args);
        ans = ans + Environment.NewLine + "\"in_the_end\"";
        AssertOut(ans);


    }
}
