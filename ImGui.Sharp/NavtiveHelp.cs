using NativeLibraryUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ImGuiNET
{
    public class NavtiveHelp
    {
        public static byte[] GetEmbedResourceWithMatchName(Func<string, bool> adj)
        {
            var assem = Assembly.GetExecutingAssembly();
            var names = assem.GetManifestResourceNames();
            var name = names.FirstOrDefault(adj);
            return name == null ? null : ReadFully(assem.GetManifestResourceStream(name));
        }

        public static byte[] ReadFully(System.IO.Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (input)
            {
                if (input == null)
                {
                    throw new ArgumentNullException(nameof(input));
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static void InitEngine()
        {
            LibraryLoader.Load(new LibraryConfig()
            {
                Linux64 = new LibraryContent[]
                {
                    new LibraryContent("cimgui.so", () => GetEmbedResourceWithMatchName((name) => name.Contains("cimgui") && name.Contains("linux") && name.Contains("64") )),
                },

                Win32 = new LibraryContent[]
                {
                    new LibraryContent("cimgui.dll", () => GetEmbedResourceWithMatchName((name) => name.Contains("cimgui") && name.Contains("win") && name.Contains("86"))),
                },

                Win64 = new LibraryContent[]
                {
                    new LibraryContent("cimgui.dll", () => GetEmbedResourceWithMatchName((name) => name.Contains("cimgui") && name.Contains("win") && name.Contains("64") )),
                },

                Mac64 = new LibraryContent[]
                {
                    new LibraryContent("cimgui.dylib", () => GetEmbedResourceWithMatchName((name) => name.Contains("cimgui") && name.Contains("osx") && name.Contains("64") )),
                },
            });

        }
    }
}
