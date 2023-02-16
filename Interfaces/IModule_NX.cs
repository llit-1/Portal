using System;
using System.Drawing;
using RKNet_Model;
using RKNet_Model.VMS.NX;


namespace Interfaces
{
    public interface IModule_NX
    {
        string ModuleName { get; }
        string ModuleVersion { get; }
        Result<Bitmap> GetCameraPicture(DateTime dateTime, NxCamera camera, int height);
        Result<string> GetSystemName(NxSystem nxSystem);
        


    }
}
