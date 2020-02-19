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

            // Create and register the texture as an XNA texture
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
            io.Fonts.AddFontDefault();
        }
        void UpdateInput(float dt)
        {
            var io = ImGui.GetIO();

            // dt
            io.DeltaTime = dt;

            // key
            {
                foreach (var ikey in _keys_map)
                {
                    io.KeysDown[(int)ikey.Key] = Keyboard.IsDown(ikey.Value);
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

        #region buffer info
        Mesh mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo.formatList, 1, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);

        private byte[] _vertexData;
        private int _vertexBufferSize;

        private ushort[] _indexData;
        private int _indexBufferSize;
        #endregion
        readonly int _Vert_Size_ = Marshal.SizeOf<ImDrawVert>();
        private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
        {
            if (drawData.TotalVtxCount == 0)
            {
                return;
            }

            // Expand buffers if we need more room
            if (drawData.TotalVtxCount > _vertexBufferSize)
            {
                _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
                _vertexData = new byte[_vertexBufferSize * _Vert_Size_];
                mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo.formatList, _vertexBufferSize, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
            }

            if (drawData.TotalIdxCount > _indexBufferSize)
            {
                _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
                _indexData = new ushort[_indexBufferSize];
            }

            // Copy ImGui's vertices and indices to a set of managed byte arrays
            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[n];

                fixed (void* vtxDstPtr = &_vertexData[vtxOffset * _Vert_Size_])
                fixed (void* idxDstPtr = &_indexData[idxOffset])
                {
                    Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * _Vert_Size_);
                    Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length * sizeof(ushort), cmdList.IdxBuffer.Size * sizeof(ushort));
                }
                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            // Copy the managed byte arrays to the gpu vertex- and index buffers
            mesh.SetVertexMap(Trans(_indexData));
            //mesh.SetVertices(0, _vertexData);
        }

        static uint[] Trans(ushort[] src)
        {
            var len = src.Length;
            uint[] dest = new uint[len];
            for (int i = 0; i < len; i++)
                dest[i] = src[i];

            return dest;
        }
        public void DrawXXXX()
        {
            unsafe
            {
                ImDrawDataPtr drawData = ImGui.GetDrawData();

                // update buffer
                UpdateBuffers(drawData);

                // draw buffer
                RenderCommandLists(drawData);

            }
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


                Graphics.SetScissor();
            }


        }



        void RenderCommandLists(ImDrawDataPtr drawData)
        {
            //var c = mesh.GetVertexCount();
            //List<Vector2> debug_lines = new List<Vector2>(3000);
            //for (int i = 0; i < c; i++)
            //{
            //    var vv = PositionColorVertex.Transform(mesh.GetVertex(i));
            //    debug_lines.Add(new Vector2(vv.X, vv.Y));
            //}
            ////Graphics.Line(debug_lines.ToArray());
            //{
            //    for (int i = 0; i < drawData.TotalIdxCount; i += 3)
            //    {
            //        var vd1 = _indexData[i + 0];
            //        var vd2 = _indexData[i + 1];
            //        var vd3 = _indexData[i + 2];

            //        var vdd1 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd1, _Vert_Size_).ToArray();
            //        var vdd2 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd2, _Vert_Size_).ToArray();
            //        var vdd3 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd3, _Vert_Size_).ToArray();

            //        var pcv1 = PositionColorVertex.Transform(vdd1);
            //        var pcv2 = PositionColorVertex.Transform(vdd2);
            //        var pcv3 = PositionColorVertex.Transform(vdd3);
            //        //Graphics.SetColor(pcv1.Color);
            //        //Graphics.Polygon(DrawMode.Fill, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
            //        mesh.SetVertex(vd1, vdd1);
            //        mesh.SetVertex(vd2, vdd2);
            //        mesh.SetVertex(vd3, vdd3);
            //        //Graphics.Polygon(DrawMode.Line, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
            //        //var data = _vertexData[];
            //    }
            //}

            Graphics.SetColor(Color.White);
            int vtxOffset = 0;
            int idxOffset = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[n];

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

                    // set range
                    //drawCmd.ClipRect;

                    {
                        for (int i = idxOffset; i < idxOffset + (int)drawCmd.ElemCount; i += 3)
                        {
                            var vd1 = _indexData[ i + 0 ];
                            var vd2 = _indexData[ i + 1 ];
                            var vd3 = _indexData[ i + 2 ];

                            var vdd1 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd1, _Vert_Size_).ToArray();
                            var vdd2 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd2, _Vert_Size_).ToArray();
                            var vdd3 = _vertexData.AsSpan().Slice(_Vert_Size_ * vd3, _Vert_Size_).ToArray();

                            //var pcv1 = PositionColorVertex.Transform(vdd1);
                            //var pcv2 = PositionColorVertex.Transform(vdd2);
                            //var pcv3 = PositionColorVertex.Transform(vdd3);
                            //Graphics.SetColor(pcv1.Color);
                            //Graphics.Polygon(DrawMode.Fill, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
                            mesh.SetVertex(vd1, vdd1);
                            mesh.SetVertex(vd2, vdd2);
                            mesh.SetVertex(vd3, vdd3);
                            //Graphics.Polygon(DrawMode.Line, pcv1.X, pcv1.Y, pcv2.X, pcv2.Y, pcv3.X, pcv3.Y);
                            //var data = _vertexData[];
                        }
                    }


                    //mesh.SetDrawRange(idxOffset, (int)drawCmd.ElemCount);
                    mesh.SetDrawRange((int)(idxOffset + drawCmd.IdxOffset), (int)drawCmd.ElemCount);
                    //mesh.SetDrawRange(vtxOffset, (int)cmdList.VtxBuffer.Size);
                    //mesh.SetDrawRange(idxOffset, (int)drawCmd.ElemCount / 3);
                    Graphics.Draw(mesh);

                    //                    foreach (var pass in effect.CurrentTechnique.Passes)
                    //                    {
                    //                        pass.Apply();

                    //#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                    //                        _graphicsDevice.DrawIndexedPrimitives(
                    //                            primitiveType: PrimitiveType.TriangleList,
                    //                            baseVertex: vtxOffset,
                    //                            minVertexIndex: 0,
                    //                            numVertices: cmdList.VtxBuffer.Size,
                    //                            startIndex: idxOffset,
                    //                            primitiveCount: (int)drawCmd.ElemCount / 3
                    //                        );
                    //#pragma warning restore CS0618
                    //                    }

                    idxOffset += (int)drawCmd.ElemCount;
                }
                vtxOffset += cmdList.VtxBuffer.Size;
            }


            Graphics.SetScissor();
        }

    }
}
