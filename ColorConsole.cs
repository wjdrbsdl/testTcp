#define Debu

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;



public class ColorConsole
{


    public static void Default(string _messege)
    {
#if Debug
        Console.WriteLine(_messege);
#endif
    }

    public static void ConsoleColor(string _messege)
    {
#if Debug
        Console.ForegroundColor = System.ConsoleColor.DarkBlue;
        Console.WriteLine(_messege);
        Console.ResetColor();
#endif
    }


}

