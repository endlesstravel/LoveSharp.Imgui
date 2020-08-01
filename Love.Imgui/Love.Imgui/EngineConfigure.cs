using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Love.Imgui
{
    public static class EngineConfigure
    {
        public static bool IsInit { private set; get; }

        static public void Init()
        {
            if (!IsInit)
            {
                IsInit = true;
                NavtiveHelper.InitEngine();
            }
        }
    }

}
