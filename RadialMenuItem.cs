using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenMeido
{
    public class RadialMenuItem
    {
        public object Icon { get; set; }  // 文本或图像资源
        public ICommand Command { get; set; }
        public string ToolTip { get; set; }
    }
}