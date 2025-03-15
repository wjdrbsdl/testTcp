using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ColorConsole
{

    public static void Default(string _messege)
    {
        Console.WriteLine(_messege);
    }

    public static void ConsoleColor(string _messege)
    {
        Console.ForegroundColor = System.ConsoleColor.DarkBlue;
        Console.WriteLine(_messege);
        Console.ResetColor();
    }


}

