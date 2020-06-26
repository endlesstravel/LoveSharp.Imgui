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
        readonly private Dictionary<IntPtr, Texture> _loadedTextures = new Dictionary<IntPtr, Texture>();
        readonly private Dictionary<Texture, IntPtr> _loadedTexturesCacheImage = new Dictionary<Texture, IntPtr>();
        readonly private Dictionary<string, IntPtr> _loadedTexturesCachePath = new Dictionary<string, IntPtr>();

        private int _textureId;
        private IntPtr? _fontTextureId;

        static public Image DefaultPathImageLoader(string path)
            => Graphics.NewImage(path);

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
        Func<string, Image>  imageLoader;
        public ImGuiRenderer(string fontPath, float fontSize, Func<string, Image> imageLoader = null)
        {
            // 加载器
            this.imageLoader = imageLoader ?? DefaultPathImageLoader;

            // 建立 context
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            SetupInput();

            // 中文支持
            ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPath, fontSize, null, ImGui.GetIO().Fonts.GetGlyphRangesChineseFull());
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
        public virtual IntPtr BindTexture(Texture texture)
        {
            var id = new IntPtr(_textureId++);
            _loadedTextures.Add(id, texture);
            return id;
        }

        public IntPtr Texture(string path)
        {
            if (_loadedTexturesCachePath.TryGetValue(path, out var imgPtr) == false)
            {
                imgPtr = BindTexture(imageLoader(path));
                _loadedTexturesCachePath[path] = imgPtr;
            }
            return imgPtr;
        }

        public IntPtr Texture(Texture tex)
        {
            if (_loadedTexturesCacheImage.TryGetValue(tex, out var imgPtr) == false)
            {
                imgPtr = BindTexture(tex);
                _loadedTexturesCacheImage[tex] = imgPtr;
            }
            return imgPtr;
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

            // key mapping
            foreach (var ikey in _keys_map)
            {
                io.KeyMap[(int)ikey.Key] = (int)(ikey.Value);
            }
            //io.ConfigFlags |= ImGui.;
            //io.Fonts.AddFontDefault();
            //io.Fonts.AddFontFromFileTTF("G:/font/方正准圆_GBK.TTF", 16.0f);
        }

        public void TextInput(string text)
        {
            ImGui.GetIO().AddInputCharactersUTF8(text);
        }

      
        void UpdateInput(float dt)
        {
            var io = ImGui.GetIO();

            // dt
            io.DeltaTime = dt;

            // key
            {
                foreach (KeyConstant ks in Enum.GetValues(typeof(KeyConstant)))
                {
                    bool downFlag = Keyboard.IsDown(ks);
                    io.KeysDown[(int)ks] = downFlag;
                }

                foreach (var ikey in _keys_map)
                {
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

                    var mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo, _vertexData,
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






    }
}
