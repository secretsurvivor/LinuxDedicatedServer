using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinuxDedicatedServer.Legacy.Interfaces
{
    public interface IConfig
    {
        public int ConsolePort { get; set; }
        public string PackageDirectory { get; set; }
    }
}
