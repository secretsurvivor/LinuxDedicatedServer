using System.Diagnostics;

namespace update;

public class Program
{
    static void Main(string[] args)
    {
        if (!int.TryParse(args[0], out var pid))
        {
            Console.WriteLine("Invalid PID");
            Console.ReadLine();
            return;
        }
        
        Process process = Process.GetProcessById(pid);
    }
}
