using System;
using Interfaces;
using System.Drawing;

namespace module_NX
{
    public partial class NX : IModule_NX
    {
        public string ModuleName { get { return "NX Witness API Video Module"; } }
        public string ModuleVersion { get { return "1.00"; } }
        
    }
}
