using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DearLoveGUI;
using Love;
using System.IO;
using ImGuiNET;
using Love.Imgui;

namespace DearLoveGUI.Example
{
    class Program : Scene
    {
        Renderer imGuiRenderer;
        private IntPtr _imGuiTexture;

        public override void Load()
        {
            imGuiRenderer = new Renderer("G:/font/msyh.ttf", 18);
            _imGuiTexture = imGuiRenderer.Texture(CreateTexture(300, 150, pixel =>
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
        private Vector3 clear_color = new Vector3(114f / 255f, 144f / 255f, 154f / 255f);
        private byte[] _textBuffer = new byte[100];

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

                ImGui.InputText("Text input", _textBuffer, 100, ImGuiInputTextFlags.EnterReturnsTrue);

                {
                    var anc = ImGui.GetItemRectMin();
                    var siz = ImGui.GetItemRectSize();
                    Keyboard.SetTextInput(ImGui.IsItemActive(), anc.X, anc.Y, siz.X, siz.Y);
                }


                ImGui.Text("Texture sample");
                ImGui.Image(_imGuiTexture, new Vector2(300, 150), Vector2.Zero, Vector2.One, Vector4.One, Vector4.One); // Here, the previously loaded texture is used
            }

            // 2. Show another simple window, this time using an explicit Begin/End pair
            if (show_another_window)
            {
                ImGui.SetNextWindowSize(new Vector2(200, 100), ImGuiCond.FirstUseEver);
                ImGui.Begin("Another Window", ref show_another_window);
                ImGui.Text("Hello");
                ImGui.End();
            }

            // 3. Show the ImGui test window. Most of the sample code is in ImGui.ShowTestWindow()
            if (show_test_window)
            {
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
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
            //imgData.SetPixels(0, 0, imgData.GetWidth(), imgData.GetHeight(), data);

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
            Boot.Init(new BootConfig()
            {
                WindowWidth = 1700,
                WindowHeight = 800,
                WindowResizable = true,
            });
            Boot.Run(new Program());
        }
    }
}
