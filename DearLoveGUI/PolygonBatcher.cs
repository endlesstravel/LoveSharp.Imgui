using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Love;
using static Love.Misc.MeshUtils;

namespace DearLoveGUI
{
    class SuperArray<T>
    {
        public T[] data = new T[0];
        public void CheckCapacity(int num)
        {
            if (data.Length < num)
            {
                var oldArray = data;
                data = new T[num];
                Array.Copy(oldArray, data, oldArray.Length);
            }
        }
    }

    //class PolygonBatcher
    //{
    //    Mesh mesh;
    //    int verticesLength = 0;
    //    int indicesLength = 0;

    //    Texture lastTexture = null;
    //    bool isDrawing = false;

    //    public int DrawCallCount { get; private set; }

    //    readonly SuperArray<uint> indexBuffer = new SuperArray<uint>();
    //    readonly SuperArray<byte> vertexBuffer = new SuperArray<byte>();

    //    public PolygonBatcher(int vertexCount)
    //    {
    //        mesh = Graphics.NewMesh(PositionColorVertex.VertexInfo.formatList, vertexCount, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
    //        indexBuffer.CheckCapacity(vertexCount);
    //        indexBuffer.CheckCapacity(vertexCount * 3 * Marshal.SizeOf<PositionColorVertex>());
    //    }

    //    unsafe public void UpdateBuffer(void* vtxBufferData, int vtxByteLen, void* idxBuffer, int idxByteLen)
    //    {
    //        // overlimit, flush them
    //        if (vtxByteLen > )
    //        {

    //        }
    //    }

    //    public void Draw(Texture texture, float[] vertices, float[] uvs, int numVertices, int[] indices)
    //    {
    //        var numIndices = indices.Length;
    //        if (texture != lastTexture)
    //        {
    //            Flush();
    //            lastTexture = texture;
    //            mesh.SetTexture(texture);
    //        }

    //        if (this.verticesLength + numVertices >= this.maxVerticesLength || this.indicesLength + numIndices > this.maxIndicesLength)
    //        {
    //            Flush();

    //            // overflow ..... to split ...
    //            //if (numVertices >= this.maxVerticesLength || numIndices > this.maxIndicesLength)
    //            //{
    //            //    int lenVertices = numVertices;
    //            //    int lenIndices = indices.Length;

    //            //    // full bufer
    //            //    int maxVV = maxVerticesLength - 2;
    //            //    int maxII = maxIndicesLength - 6;

    //            //    var verticesA = GenCopyed(vertices, 0, maxVV);
    //            //    var uvsA = GenCopyed(uvs, 0, maxVV);
    //            //    var indicesA = GenCopyed(indices, 0, maxII);
    //            //    Draw(texture, verticesA, uvsA, maxVV, indicesA, color, darkColor, vertexEffect);

    //            //    // remain
    //            //    var verticesB = GenCopyed(vertices, maxVV, lenVertices - maxVV);
    //            //    var uvsB = GenCopyed(uvs, maxVV, lenVertices - maxVV);
    //            //    var indicesB = GenCopyed(indices, maxII, lenIndices - maxII);
    //            //    Draw(texture, verticesB, uvsB, lenVertices - maxVV, indicesB, color, darkColor, vertexEffect);
    //            //    return;
    //            //}
    //        }



    //        var indexStart = this.indicesLength;
    //        var offset = this.verticesLength;
    //        var indexEnd = indexStart + numIndices;
    //        var meshIndices = this.indexBuffer;
    //        for (int i = 0; indexStart < indexEnd; i++)
    //        {
    //            meshIndices[indexStart] = (uint)(indices[i] + offset);
    //            indexStart = indexStart + 1;
    //        }

    //        this.indicesLength = this.indicesLength + numIndices;

    //        var vertexStart = this.verticesLength;
    //        var vertexEnd = vertexStart + numVertices;
    //        var vertex = this.vertex;


    //        {
    //            for (int v = 0; vertexStart < vertexEnd; vertexStart += 1, v += 2)
    //            {
    //                vertex.X = vertices[v + 0];
    //                vertex.Y = vertices[v + 1];
    //                vertex.U = uvs[v + 0];
    //                vertex.V = uvs[v + 1];
    //                vertex.Color = color;

    //                //mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.VertexInfo.GetData(new VertexPositionColorTextureColor[] { vertex }));
    //                mesh.SetVertex(vertexStart, PositionColorVertex.TransformToBytesByBuffer(ref vertex));
    //                //mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.TransformToBytesByCopy(ref vertex));
    //            }
    //        }


    //        this.verticesLength = this.verticesLength + numVertices;
    //    }

    //    T[] GenCopyed<T>(T[] src, int sourceIndex, int len)
    //    {
    //        T[] subArray = new T[len];
    //        Array.Copy(src, sourceIndex, subArray, 0, subArray.Length);
    //        return subArray;
    //    }

    //    public void Flush()
    //    {
    //        if (this.verticesLength == 0) return;
    //        var indicesArray = this.indexBuffer.ToArray();
    //        mesh.SetVertexMap(indicesArray);
    //        ////if (this.indicesLength < Mathf.CeilToInt(indicesArray.Length * 0.75f)) // optimization code
    //        //if (this.indicesLength < indicesArray.Length) // optimization code
    //        //{
    //        //    uint[] subArray = new uint[this.indicesLength];
    //        //    Array.Copy(indicesArray, subArray, subArray.Length);
    //        //    mesh.SetVertexMap(indicesArray);
    //        //}
    //        //else
    //        //{
    //        //    mesh.SetVertexMap(indicesArray);
    //        //}
    //        mesh.SetDrawRange(0, this.indicesLength);

    //        var oldShader = Graphics.GetShader();
    //        Graphics.SetShader();
    //        Graphics.Draw(mesh);
    //        Graphics.SetShader(oldShader);

    //        this.verticesLength = 0;
    //        this.indicesLength = 0;
    //        this.drawCalls++;
    //    }

    //    public void Stop()
    //    {
    //        if (this.isDrawing == false)
    //            throw new Exception("PolygonBatcher is not drawing. Call PolygonBatcher:begin() first.");

    //        if (this.verticesLength > 0)
    //            this.Flush();

    //        lastTexture = null;
    //        isDrawing = false;
    //    }
    //}

    [StructLayout(LayoutKind.Explicit)]
    struct PositionColorVertex
    {
        [FieldOffset(0), Name("VertexPosition")] public float X;
        [FieldOffset(4), Name("VertexPosition")] public float Y;

        [FieldOffset(8), Name("VertexTexCoord")] public float U;
        [FieldOffset(12), Name("VertexTexCoord")] public float V;

        [FieldOffset(16), Name("VertexColor")] public byte R;
        [FieldOffset(17), Name("VertexColor")] public byte G;
        [FieldOffset(18), Name("VertexColor")] public byte B;
        [FieldOffset(19), Name("VertexColor")] public byte A;

        [FieldOffset(0)] byte b_x1;
        [FieldOffset(1)] byte b_x2;
        [FieldOffset(2)] byte b_x3;
        [FieldOffset(3)] byte b_x4;
        [FieldOffset(4)] byte b_y1;
        [FieldOffset(5)] byte b_y2;
        [FieldOffset(6)] byte b_y3;
        [FieldOffset(7)] byte b_y4;
        [FieldOffset(8)] byte b_u1;
        [FieldOffset(9)] byte b_u2;
        [FieldOffset(10)] byte b_u3;
        [FieldOffset(11)] byte b_u4;
        [FieldOffset(12)] byte b_v1;
        [FieldOffset(13)] byte b_v2;
        [FieldOffset(14)] byte b_v3;
        [FieldOffset(15)] byte b_v4;

        public readonly static Info<PositionColorVertex> VertexInfo = Parse<PositionColorVertex>();


        public Color Color
        {
            get => new Color(R, G, B, A);
            set
            {
                this.R = value.r;
                this.G = value.g;
                this.B = value.b;
                this.A = value.a;
            }
        }
        //public static byte[] TransformToBytesByCopy(ref VertexPositionColorTextureColor vpcc)
        //{
        //    unsafe
        //    {
        //        fixed (void * pSrc = &vpcc)
        //        {
        //            fixed(void* pDest = transform_buffer)
        //            {
        //                Buffer.MemoryCopy(pSrc, pDest, transform_buffer.Length, transform_buffer.Length);
        //            }
        //        }
        //    }
        //    return transform_buffer;
        //}


        public static PositionColorVertex Transform(byte[] data)
        {
            PositionColorVertex vpcc = new PositionColorVertex();
            vpcc.b_x1 = data[00];
            vpcc.b_x2 = data[01];
            vpcc.b_x3 = data[02];
            vpcc.b_x4 = data[03];

            vpcc.b_y1 = data[04];
            vpcc.b_y2 = data[05];
            vpcc.b_y3 = data[06];
            vpcc.b_y4 = data[07];

            vpcc.b_u1 = data[08];
            vpcc.b_u2 = data[09];
            vpcc.b_u3 = data[10];
            vpcc.b_u4 = data[11];

            vpcc.b_v1 = data[12];
            vpcc.b_v2 = data[13];
            vpcc.b_v3 = data[14];
            vpcc.b_v4 = data[15];

            //vpcc.R = data[16];
            //vpcc.G = data[17];
            //vpcc.B = data[18];
            //vpcc.A = data[19];

            //vpcc.R = data[19];
            //vpcc.G = data[18];
            //vpcc.B = data[17];
            //vpcc.A = data[16];

            return vpcc;
        }
        public static byte[] transform_buffer = new byte[24];
        public static byte[] TransformToBytesByBuffer(ref PositionColorVertex vpcc)
        {
            transform_buffer[0] = vpcc.b_x1;
            transform_buffer[1] = vpcc.b_x2;
            transform_buffer[2] = vpcc.b_x3;
            transform_buffer[3] = vpcc.b_x4;

            transform_buffer[4] = vpcc.b_y1;
            transform_buffer[5] = vpcc.b_y2;
            transform_buffer[6] = vpcc.b_y3;
            transform_buffer[7] = vpcc.b_y4;

            transform_buffer[8] = vpcc.b_u1;
            transform_buffer[9] = vpcc.b_u2;
            transform_buffer[10] = vpcc.b_u3;
            transform_buffer[11] = vpcc.b_u4;

            transform_buffer[12] = vpcc.b_v1;
            transform_buffer[13] = vpcc.b_v2;
            transform_buffer[14] = vpcc.b_v3;
            transform_buffer[15] = vpcc.b_v4;

            transform_buffer[16] = vpcc.R;
            transform_buffer[17] = vpcc.G;
            transform_buffer[18] = vpcc.B;
            transform_buffer[19] = vpcc.A;

            return transform_buffer;
        }
        public static byte[] TransformToBytes(ref PositionColorVertex vpcc)
        {
            return new byte[]
            {
                vpcc.b_x1,
                vpcc.b_x2,
                vpcc.b_x3,
                vpcc.b_x4,
                vpcc.b_y1,
                vpcc.b_y2,
                vpcc.b_y3,
                vpcc.b_y4,
                vpcc.b_u1,
                vpcc.b_u2,
                vpcc.b_u3,
                vpcc.b_u4,
                vpcc.b_v1,
                vpcc.b_v2,
                vpcc.b_v3,
                vpcc.b_v4,

                vpcc.R, vpcc.G, vpcc.B, vpcc.A,
            };
        }

        public PositionColorVertex(float x, float y, float u, float v, Color c)
        {
            b_x1 = 0;
            b_x2 = 0;
            b_x3 = 0;
            b_x4 = 0;
            b_y1 = 0;
            b_y2 = 0;
            b_y3 = 0;
            b_y4 = 0;
            b_u1 = 0;
            b_u2 = 0;
            b_u3 = 0;
            b_u4 = 0;
            b_v1 = 0;
            b_v2 = 0;
            b_v3 = 0;
            b_v4 = 0;

            X = x;
            Y = y;
            U = u;
            V = v;
            R = c.r;
            G = c.g;
            B = c.b;
            A = c.a;
        }
    }
}
