using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ImGuiNET;
using Love;
using System.Buffers;

namespace DearLoveGUI
{
    public class ImGuiRenderer
    {
        // Textures
        readonly private Dictionary<IntPtr, Image> _loadedTextures = new Dictionary<IntPtr, Image>();
        private int _textureId;
        private IntPtr? _fontTextureId;

        readonly static Dictionary<ImGuiKey, KeyConstant> _keys_map = new Dictionary<ImGuiKey, KeyConstant>()
        {
                {ImGuiKey.Tab, KeyConstant.Tab },
                {ImGuiKey.LeftArrow, KeyConstant.Left },
                {ImGuiKey.RightArrow, KeyConstant.Right },
                {ImGuiKey.UpArrow, KeyConstant.Up },
                {ImGuiKey.DownArrow, KeyConstant.Down },
                {ImGuiKey.PageUp, KeyConstant.PageUp },
                {ImGuiKey.PageDown, KeyConstant.PageDown },
                {ImGuiKey.Home, KeyConstant.Home },
                {ImGuiKey.End, KeyConstant.End },
                {ImGuiKey.Delete, KeyConstant.Delete },
                {ImGuiKey.Backspace, KeyConstant.Backspace },
                {ImGuiKey.Enter, KeyConstant.Enter },
                {ImGuiKey.Escape, KeyConstant.Escape },
                {ImGuiKey.A, KeyConstant.A },
                {ImGuiKey.C, KeyConstant.C },
                {ImGuiKey.V, KeyConstant.V },
                {ImGuiKey.X, KeyConstant.X },
                {ImGuiKey.Y, KeyConstant.Y },
                {ImGuiKey.Z, KeyConstant.Z },
        };

        public ImGuiRenderer()
        {
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            SetupInput();
            RebuildFontAtlas();
        }

        /// <summary>
        /// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
        /// </summary>
        unsafe void RebuildFontAtlas()
        {
            // Get font texture from ImGui
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

            // Copy the data to a managed array
            var pixels = new byte[width * height * bytesPerPixel];
            unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }

            // Create and register the texture
            var tex2d = Graphics.NewImage(Image.NewImageData(width, height, ImageDataPixelFormat.RGBA8, pixels));
            // new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            //tex2d.SetData(pixels);

            // Should a texture already have been build previously, unbind it first so it can be deallocated
            if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);

            // Bind the new texture to an ImGui-friendly id
            _fontTextureId = BindTexture(tex2d);

            // Let ImGui know where to find the texture
            io.Fonts.SetTexID(_fontTextureId.Value);
            io.Fonts.ClearTexData(); // Clears CPU side texture data
        }

        /// <summary>
        /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
        /// </summary>
        public virtual IntPtr BindTexture(Image texture)
        {
            var id = new IntPtr(_textureId++);

            _loadedTextures.Add(id, texture);

            return id;
        }

        /// <summary>
        /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
        /// </summary>
        public virtual void UnbindTexture(IntPtr textureId)
        {
            _loadedTextures.Remove(textureId);
        }

        public void Update(float dt, Action layoutFunc)
        {
            UpdateInput(dt);
            ImGui.NewFrame();
            (layoutFunc ?? throw new ArgumentNullException(nameof(layoutFunc)))();
            ImGui.Render();
        }


        void SetupInput()
        {
            var io = ImGui.GetIO();

            //// key mapping
            foreach (var ikey in _keys_map)
            {
                io.KeyMap[(int)ikey.Key] = (int)(ikey.Value);
            }
            //io.ConfigFlags |= ImGui.;
            //io.Fonts.AddFontDefault();
            io.Fonts.AddFontFromFileTTF("G:/font/方正准圆_GBK.TTF", 16.0f);
        }

        public void TextInput(string text)
        {
            Console.WriteLine(text);
            //foreach (var c in text)
            //{
            //    char.ConvertToUtf32()
            //    ImGui.GetIO().AddInputCharacter(Encoding.Unicode.getco);
            //}
            for (var i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
            {
                var codepoint = char.ConvertToUtf32(text, i);
                ImGui.GetIO().AddInputCharacter((uint)codepoint);
            }

            //ImGui.GetIO().AddInputCharactersUTF8(text);
        }

        void UpdateInput(float dt)
        {
            var io = ImGui.GetIO();

            // dt
            io.DeltaTime = dt;

            // key
            {
                //foreach (KeyConstant ks in Enum.GetValues(typeof(KeyConstant)))
                //{
                //    bool downFlag = Keyboard.IsDown(ks);
                //    io.KeysDown[(int)Keyboard.GetScancodeFromKey(ks)] = downFlag;
                //}
                foreach (KeyConstant ks in Enum.GetValues(typeof(KeyConstant)))
                {
                    bool downFlag = Keyboard.IsDown(ks);
                    //io.KeysDown[(int)Keyboard.GetScancodeFromKey(ks)] = downFlag;
                    io.KeysDown[(int)ks] = downFlag;
                }
                //io.KeysDown['1'] = Keyboard.IsDown(KeyConstant.Number1);
                //Scancode.Number1
                //SDL_Scancode.SDL_SCANCODE_1

                foreach (var ikey in _keys_map)
                {
                    //io.KeysDown[(int)ikey.Key] = Keyboard.IsDown((ikey.Value));
                    io.KeysDown[(int)ikey.Value] = Keyboard.IsDown((ikey.Value));
                }

                io.KeyShift = Keyboard.IsDown(KeyConstant.LShift) || Keyboard.IsDown(KeyConstant.RShift);
                io.KeyCtrl = Keyboard.IsDown(KeyConstant.LCtrl) || Keyboard.IsDown(KeyConstant.RCtrl);
                io.KeyAlt = Keyboard.IsDown(KeyConstant.LAlt) || Keyboard.IsDown(KeyConstant.RAlt);
                io.KeySuper = Keyboard.IsDown(KeyConstant.LGUI) || Keyboard.IsDown(KeyConstant.RGUI);
            }

            // windows
            {
                io.DisplaySize = new System.Numerics.Vector2(Graphics.GetWidth(), Graphics.GetHeight());
                io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
            }

            // mouse
            {
                io.MousePos = new System.Numerics.Vector2(Mouse.GetX(), Mouse.GetY());
                for (int i = 0; i < 5; i++)
                    io.MouseDown[i] = Mouse.IsDown(i);

                io.MouseWheel = Mouse.GetScrollY();


            }

        }


        readonly int _Vert_Size_ = Marshal.SizeOf<ImDrawVert>();

        static uint[] Trans(ushort[] src)
        {
            var len = src.Length;
            uint[] dest = new uint[len];
            for (int i = 0; i < len; i++)
                dest[i] = src[i];

            return dest;
        }

        public void Draw()
        {
            unsafe
            {
                ImDrawDataPtr drawData = ImGui.GetDrawData();
                Graphics.SetColor(Color.White);
                for (int n = 0; n < drawData.CmdListsCount; n++)
                {
                    ImDrawListPtr cmdList = drawData.CmdListsRange[n];
                    var _vertexData = new byte[cmdList.VtxBuffer.Size * _Vert_Size_];
                    var _indexData = new ushort[cmdList.IdxBuffer.Size];
                    fixed (void* vtxDstPtr = _vertexData)
                    fixed (void* idxDstPtr = _indexData)
                    {
                        Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * _Vert_Size_);
                        Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length * sizeof(ushort), cmdList.IdxBuffer.Size * sizeof(ushort));
                    }
                    var mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo.formatList, _vertexData,
                        MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
                    mesh.SetVertexMap(Trans(_indexData));

                    for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
                    {
                        ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                        if (!_loadedTextures.TryGetValue(drawCmd.TextureId, out var img))
                            throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");

                        Graphics.SetScissor(
                            (int)drawCmd.ClipRect.X,
                            (int)drawCmd.ClipRect.Y,
                            (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                            (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                        );

                        mesh.SetTexture(img);
                        mesh.SetDrawRange((int)drawCmd.IdxOffset, (int)drawCmd.ElemCount);
                        Graphics.Draw(mesh);
                    }
                    mesh.Dispose();
                    mesh = null;
                }


            }

            Graphics.SetScissor();

        }





        //static bool IsDefKeyDown(IoDefKey defkey)
        //{
        //    return keyMapping.TryGetValue(defkey, out var key) ? Keyboard.IsDown(key) : false;
        //}


        //static readonly Dictionary<IoDefKey, KeyConstant> keyMapping = new Dictionary<IoDefKey, KeyConstant>()
        //{
        //    { IoDefKey.A, Keycon }
        //};

        //public enum IoDefKey
        //{
        //    None = 0,
        //    Back = 8,
        //    Tab = 9,
        //    Enter = 13,
        //    Pause = 19,
        //    CapsLock = 20,
        //    Kana = 21,
        //    Kanji = 25,
        //    Escape = 27,
        //    ImeConvert = 28,
        //    ImeNoConvert = 29,
        //    Space = 32,
        //    PageUp = 33,
        //    PageDown = 34,
        //    End = 35,
        //    Home = 36,
        //    Left = 37,
        //    Up = 38,
        //    Right = 39,
        //    Down = 40,
        //    Select = 41,
        //    Print = 42,
        //    Execute = 43,
        //    PrintScreen = 44,
        //    Insert = 45,
        //    Delete = 46,
        //    Help = 47,
        //    D0 = 48,
        //    D1 = 49,
        //    D2 = 50,
        //    D3 = 51,
        //    D4 = 52,
        //    D5 = 53,
        //    D6 = 54,
        //    D7 = 55,
        //    D8 = 56,
        //    D9 = 57,
        //    A = 65,
        //    B = 66,
        //    C = 67,
        //    D = 68,
        //    E = 69,
        //    F = 70,
        //    G = 71,
        //    H = 72,
        //    I = 73,
        //    J = 74,
        //    K = 75,
        //    L = 76,
        //    M = 77,
        //    N = 78,
        //    O = 79,
        //    P = 80,
        //    Q = 81,
        //    R = 82,
        //    S = 83,
        //    T = 84,
        //    U = 85,
        //    V = 86,
        //    W = 87,
        //    X = 88,
        //    Y = 89,
        //    Z = 90,
        //    LeftWindows = 91,
        //    RightWindows = 92,
        //    Apps = 93,
        //    Sleep = 95,
        //    NumPad0 = 96,
        //    NumPad1 = 97,
        //    NumPad2 = 98,
        //    NumPad3 = 99,
        //    NumPad4 = 100,
        //    NumPad5 = 101,
        //    NumPad6 = 102,
        //    NumPad7 = 103,
        //    NumPad8 = 104,
        //    NumPad9 = 105,
        //    Multiply = 106,
        //    Add = 107,
        //    Separator = 108,
        //    Subtract = 109,
        //    Decimal = 110,
        //    Divide = 111,
        //    F1 = 112,
        //    F2 = 113,
        //    F3 = 114,
        //    F4 = 115,
        //    F5 = 116,
        //    F6 = 117,
        //    F7 = 118,
        //    F8 = 119,
        //    F9 = 120,
        //    F10 = 121,
        //    F11 = 122,
        //    F12 = 123,
        //    F13 = 124,
        //    F14 = 125,
        //    F15 = 126,
        //    F16 = 127,
        //    F17 = 128,
        //    F18 = 129,
        //    F19 = 130,
        //    F20 = 131,
        //    F21 = 132,
        //    F22 = 133,
        //    F23 = 134,
        //    F24 = 135,
        //    NumLock = 144,
        //    Scroll = 145,
        //    LeftShift = 160,
        //    RightShift = 161,
        //    LeftControl = 162,
        //    RightControl = 163,
        //    LeftAlt = 164,
        //    RightAlt = 165,
        //    BrowserBack = 166,
        //    BrowserForward = 167,
        //    BrowserRefresh = 168,
        //    BrowserStop = 169,
        //    BrowserSearch = 170,
        //    BrowserFavorites = 171,
        //    BrowserHome = 172,
        //    VolumeMute = 173,
        //    VolumeDown = 174,
        //    VolumeUp = 175,
        //    MediaNextTrack = 176,
        //    MediaPreviousTrack = 177,
        //    MediaStop = 178,
        //    MediaPlayPause = 179,
        //    LaunchMail = 180,
        //    SelectMedia = 181,
        //    LaunchApplication1 = 182,
        //    LaunchApplication2 = 183,
        //    OemSemicolon = 186,
        //    OemPlus = 187,
        //    OemComma = 188,
        //    OemMinus = 189,
        //    OemPeriod = 190,
        //    OemQuestion = 191,
        //    OemTilde = 192,
        //    ChatPadGreen = 202,
        //    ChatPadOrange = 203,
        //    OemOpenBrackets = 219,
        //    OemPipe = 220,
        //    OemCloseBrackets = 221,
        //    OemQuotes = 222,
        //    Oem8 = 223,
        //    OemBackslash = 226,
        //    ProcessKey = 229,
        //    OemCopy = 242,
        //    OemAuto = 243,
        //    OemEnlW = 244,
        //    Attn = 246,
        //    Crsel = 247,
        //    Exsel = 248,
        //    EraseEof = 249,
        //    Play = 250,
        //    Zoom = 251,
        //    Pa1 = 253,
        //    OemClear = 254
        //}





        //#region buffer info
        //Mesh mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo.formatList, 1, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);

        //private byte[] _vertexData;
        //private int _vertexBufferSize;

        //private ushort[] _indexData;
        //private int _indexBufferSize;
        //#endregion
        //private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
        //{
        //    if (drawData.TotalVtxCount == 0)
        //    {
        //        return;
        //    }

        //    // Expand buffers if we need more room
        //    if (drawData.TotalVtxCount > _vertexBufferSize)
        //    {
        //        _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
        //        _vertexData = new byte[_vertexBufferSize * _Vert_Size_];
        //        mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo.formatList, _vertexBufferSize, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
        //    }

        //    if (drawData.TotalIdxCount > _indexBufferSize)
        //    {
        //        _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
        //        _indexData = new ushort[_indexBufferSize];
        //    }

        //    // Copy ImGui's vertices and indices to a set of managed byte arrays
        //    int vtxOffset = 0;
        //    int idxOffset = 0;

        //    for (int n = 0; n < drawData.CmdListsCount; n++)
        //    {
        //        ImDrawListPtr cmdList = drawData.CmdListsRange[n];

        //        fixed (void* vtxDstPtr = &_vertexData[vtxOffset * _Vert_Size_])
        //        fixed (void* idxDstPtr = &_indexData[idxOffset])
        //        {
        //            Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * _Vert_Size_);
        //            Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length * sizeof(ushort), cmdList.IdxBuffer.Size * sizeof(ushort));
        //        }
        //        vtxOffset += cmdList.VtxBuffer.Size;
        //        idxOffset += cmdList.IdxBuffer.Size;
        //    }

        //    // Copy the managed byte arrays to the gpu vertex- and index buffers
        //    mesh.SetVertexMap(Trans(_indexData));
        //    //mesh.SetVertices(0, _vertexData);
        //}


        //public void DrawXXXX()
        //{
        //    unsafe
        //    {
        //        ImDrawDataPtr drawData = ImGui.GetDrawData();

        //        // update buffer
        //        UpdateBuffers(drawData);

        //        // draw buffer
        //        RenderCommandLists(drawData);

        //    }
        //}

        //void RenderCommandLists(ImDrawDataPtr drawData)
        //{
        //    //var c = mesh.GetVertexCount();
        //    //List<Vector2> debug_lines = new List<Vector2>(3000);
        //    //for (int i = 0; i < c; i++)
        //    //{
        //    //    var vv = PositionColorVertex.Transform(mesh.GetVertex(i));
        //    //    debug_lines.Add(new Vector2(vv.X, vv.Y));
        //    //}
        //    ////Graphics.Line(debug_lines.ToArray());
        //    //{
        //    //    for (int i = 0; i < drawData.TotalIdxCount; i += 3)
        //    //    {
        //    //        var vd1 = _indexData[i + 0];
        //    //        var vd2 = _indexData[i + 1];
        //    //        var vd3 = _indexData[i + 2];

        //    //        var vdd1 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd1, _Vert_Size_).ToArray();
        //    //        var vdd2 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd2, _Vert_Size_).ToArray();
        //    //        var vdd3 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd3, _Vert_Size_).ToArray();

        //    //        var pcv1 = PositionColorVertex.Transform(vdd1);
        //    //        var pcv2 = PositionColorVertex.Transform(vdd2);
        //    //        var pcv3 = PositionColorVertex.Transform(vdd3);
        //    //        //Graphics.SetColor(pcv1.Color);
        //    //        //Graphics.Polygon(DrawMode.Fill, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
        //    //        mesh.SetVertex(vd1, vdd1);
        //    //        mesh.SetVertex(vd2, vdd2);
        //    //        mesh.SetVertex(vd3, vdd3);
        //    //        //Graphics.Polygon(DrawMode.Line, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
        //    //        //var data = _vertexData[];
        //    //    }
        //    //}

        //    Graphics.SetColor(Color.White);
        //    int vtxOffset = 0;
        //    int idxOffset = 0;
        //    for (int n = 0; n < drawData.CmdListsCount; n++)
        //    {
        //        ImDrawListPtr cmdList = drawData.CmdListsRange[n];

        //        for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
        //        {
        //            ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

        //            if (!_loadedTextures.TryGetValue(drawCmd.TextureId, out var img))
        //                throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");

        //            Graphics.SetScissor(
        //                (int)drawCmd.ClipRect.X,
        //                (int)drawCmd.ClipRect.Y,
        //                (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
        //                (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
        //            );
        //            mesh.SetTexture(img);

        //            // set range
        //            //drawCmd.ClipRect;

        //            {
        //                for (int i = idxOffset; i < idxOffset + (int)drawCmd.ElemCount; i += 3)
        //                {
        //                    var vd1 = _indexData[ i + 0 ];
        //                    var vd2 = _indexData[ i + 1 ];
        //                    var vd3 = _indexData[ i + 2 ];

        //                    var vdd1 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd1, _Vert_Size_).ToArray();
        //                    var vdd2 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd2, _Vert_Size_).ToArray();
        //                    var vdd3 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd3, _Vert_Size_).ToArray();

        //                    //var pcv1 = PositionColorVertex.Transform(vdd1);
        //                    //var pcv2 = PositionColorVertex.Transform(vdd2);
        //                    //var pcv3 = PositionColorVertex.Transform(vdd3);
        //                    //Graphics.SetColor(pcv1.Color);
        //                    //Graphics.Polygon(DrawMode.Fill, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
        //                    mesh.SetVertex(vd1, vdd1);
        //                    mesh.SetVertex(vd2, vdd2);
        //                    mesh.SetVertex(vd3, vdd3);
        //                    //Graphics.Polygon(DrawMode.Line, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
        //                    //var data = _vertexData[];
        //                }
        //            }


        //            //mesh.SetDrawRange(idxOffset, (int)drawCmd.ElemCount);
        //            mesh.SetDrawRange((int)(idxOffset + drawCmd.IdxOffset), (int)drawCmd.ElemCount);
        //            //mesh.SetDrawRange(vtxOffset, (int)cmdList.VtxBuffer.Size);
        //            //mesh.SetDrawRange(idxOffset, (int)drawCmd.ElemCount / 3);
        //            Graphics.Draw(mesh);

        //            //                    foreach (var pass in effect.CurrentTechnique.Passes)
        //            //                    {
        //            //                        pass.Apply();

        //            //#pragma warning disable CS0618 // // FNA does not expose an alternative method.
        //            //                        _graphicsDevice.DrawIndexedPrimitives(
        //            //                            primitiveType: PrimitiveType.TriangleList,
        //            //                            baseVertex: vtxOffset,
        //            //                            minVertexIndex: 0,
        //            //                            numVertices: cmdList.VtxBuffer.Size,
        //            //                            startIndex: idxOffset,
        //            //                            primitiveCount: (int)drawCmd.ElemCount / 3
        //            //                        );
        //            //#pragma warning restore CS0618
        //            //                    }

        //            idxOffset += (int)drawCmd.ElemCount;
        //        }
        //        vtxOffset += cmdList.VtxBuffer.Size;
        //    }


        //    Graphics.SetScissor();
        //}

    }
}
