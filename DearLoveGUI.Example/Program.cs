using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DearLoveGUI;
using Love;
using System;
using System.IO;
using Num = System.Numerics;
using ImGuiNET;

namespace DearLoveGUI.Example
{
    class Program : Scene
    {
        ImGuiRenderer imGuiRenderer = new ImGuiRenderer();
        private IntPtr _imGuiTexture;

        public override void Load()
        {
            _imGuiTexture = imGuiRenderer.BindTexture(CreateTexture(300, 150, pixel =>
            {
                var red = (pixel % 300) / 2;
                return new Color((byte)red, 1, 1, 255);
            }));
        }

        public override void Update(float dt)
        {
            imGuiRenderer.Update(dt, ImGuiLayout);
        }

        public override void TextInput(string text)
        {
            imGuiRenderer.TextInput(text);
        }

        // Direct port of the example at https://github.com/ocornut/imgui/blob/master/examples/sdl_opengl2_example/main.cpp
        private float f = 0.0f;

        private bool show_test_window = false;
        private bool show_another_window = false;
        private Num.Vector3 clear_color = new Num.Vector3(114f / 255f, 144f / 255f, 154f / 255f);
        private byte[] _textBuffer = new byte[100];

        static System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
        static byte[] EmptyStringByteArray = new byte[1] { 0 };
        public static byte[] GetNullTailUTF8Bytes(string str)
        {
            if (str == null)
            {
                return EmptyStringByteArray;
            }

            var bytes = utf8.GetBytes(str);
            var output = new byte[bytes.Length + 1];
            Array.Copy(bytes, output, bytes.Length);
            output[output.Length - 1] = 0;
            return output;
        }
        void ImGuiLayout()
        {
            // 1. Show a simple window
            // Tip: if we don't call ImGui.Begin()/ImGui.End() the widgets appears in a window automatically called "Debug"
            {
                ImGui.Text("Hello, world!【世界】");
                ImGui.SliderFloat("float", ref f, 0.0f, 1.0f, string.Empty, 1f);
                ImGui.ColorEdit3("clear color", ref clear_color);
                if (ImGui.Button("Test Window")) show_test_window = !show_test_window;
                if (ImGui.Button("Another Window")) show_another_window = !show_another_window;
                ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGui.GetIO().Framerate, ImGui.GetIO().Framerate));

                ImGui.InputText("Text input", _textBuffer, 100);

                ImGui.Text("Texture sample");
                ImGui.Image(_imGuiTexture, new Num.Vector2(300, 150), Num.Vector2.Zero, Num.Vector2.One, Num.Vector4.One, Num.Vector4.One); // Here, the previously loaded texture is used
            }

            // 2. Show another simple window, this time using an explicit Begin/End pair
            if (show_another_window)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(200, 100), ImGuiCond.FirstUseEver);
                ImGui.Begin("Another Window", ref show_another_window);
                ImGui.Text("Hello");
                ImGui.End();
            }

            // 3. Show the ImGui test window. Most of the sample code is in ImGui.ShowTestWindow()
            if (show_test_window)
            {
                ImGui.SetNextWindowPos(new Num.Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref show_test_window);
            }
        }

        public static Image CreateTexture(int width, int height, Func<int, Color> paint)
        {
            //initialize a texture

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for (var pixel = 0; pixel < data.Length; pixel++)
            {
                //the function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            //set the color
            var imgData = Image.NewImageData(width, height);
            imgData.SetPixels(data);

            return Graphics.NewImage(imgData);
        }
        public override void Draw()
        {
            Graphics.SetColor(0x7d / 255f, 0x9e / 255f, 0xb9 / 255f, 1f);
            Graphics.Rectangle(DrawMode.Fill, 0, 0, Graphics.GetWidth(), Graphics.GetHeight());

            imGuiRenderer.Draw();
        }


        static void Main(string[] args)
        {
            Helper.InitEngine();
            Boot.Init(new BootConfig()
            {
                WindowWidth = 1200,
                WindowHeight = 700,
            });
            Boot.Run(new Program());
        }
    }
}
