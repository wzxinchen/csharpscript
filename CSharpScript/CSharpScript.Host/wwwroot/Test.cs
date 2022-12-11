using System;
using System.Runtime.InteropServices.JavaScript;

namespace CSharpScript.Host.wwwroot
{
    public class Test
    {
        [JSExport]
        public static void T()
        {
            Console.WriteLine("xxx");
        }
    }
}
