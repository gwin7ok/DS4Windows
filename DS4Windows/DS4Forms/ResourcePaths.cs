using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using NonFormTimer = System.Timers.Timer;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4Windows;
using DS4Windows.DI;
using System.ComponentModel;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;
using DS4Windows.Actions;

namespace DS4WinWPF.DS4Forms
{

    public class ResourcePaths
    {
        public string SizePNG { get => $"{Global.RESOURCES_PREFIX}/size.png"; }
        public string DS4ConfigPNG { get => $"{Global.RESOURCES_PREFIX}/DS4 Config.png"; }
        public string DS4LightbarPNG { get => $"{Global.RESOURCES_PREFIX}/DS4 lightbar.png"; }
        public string DS4ConfigRSPNG { get => $"{Global.RESOURCES_PREFIX}/DS4-Config_RS.png"; }
        public string RainbowPNG { get => $"{Global.RESOURCES_PREFIX}/rainbow.png"; }
    }
}