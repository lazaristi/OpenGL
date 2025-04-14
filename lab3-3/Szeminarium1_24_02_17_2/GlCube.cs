using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Szeminarium1_24_02_17_2
{
    internal class GlCube
    {
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }

        private GL Gl;

        private GlCube(uint vao, uint vertices, uint colors, uint indeces, uint indexArrayLength, GL gl)
        {
            this.Vao = vao;
            this.Vertices = vertices;
            this.Colors = colors;
            this.Indices = indeces;
            this.IndexArrayLength = indexArrayLength;
            this.Gl = gl;
        }

        public static unsafe GlCube CreateCubeWithFaceColors(GL Gl, float[] face1Color, float[] face2Color, float[] face3Color, float[] face4Color, float[] face5Color, float[] face6Color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[] {
                -0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                0.5f, 0.5f, -0.5f, 0f, 1f, 0f,
                -0.5f, 0.5f, -0.5f, 0f, 1f, 0f,

                -0.5f, 0.5f, 0.5f, 0f, 0f, 1f,
                -0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                0.5f, 0.5f, 0.5f, 0f, 0f, 1f,

                -0.5f, 0.5f, 0.5f, -1f, 0f, 0f,
                -0.5f, 0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, 0.5f, -1f, 0f, 0f,

                -0.5f, -0.5f, 0.5f, 0f, -1f, 0f,
                0.5f, -0.5f, 0.5f, 0f, -1f, 0f,
                0.5f, -0.5f, -0.5f, 0f, -1f, 0f,
                -0.5f, -0.5f, -0.5f, 0f, -1f, 0f,

                0.5f, 0.5f, -0.5f, 0f, 0f, -1f,
                -0.5f, 0.5f, -0.5f, 0f, 0f, -1f,
                -0.5f, -0.5f, -0.5f, 0f, 0f, -1f,
                0.5f, -0.5f, -0.5f, 0f, 0f, -1f,

                0.5f, 0.5f, 0.5f, 1f, 0f, 0f,
                0.5f, 0.5f, -0.5f, 1f, 0f, 0f,
                0.5f, -0.5f, -0.5f, 1f, 0f, 0f,
                0.5f, -0.5f, 0.5f, 1f, 0f, 0f
            };

            // Színek összeállítása
            List<float> colorsList = new List<float>();
            for (int i = 0; i < 4; i++) colorsList.AddRange(face1Color);
            for (int i = 0; i < 4; i++) colorsList.AddRange(face2Color);
            for (int i = 0; i < 4; i++) colorsList.AddRange(face3Color);
            for (int i = 0; i < 4; i++) colorsList.AddRange(face4Color);
            for (int i = 0; i < 4; i++) colorsList.AddRange(face5Color);
            for (int i = 0; i < 4; i++) colorsList.AddRange(face6Color);

            float[] colorArray = colorsList.ToArray();

            uint[] indexArray = new uint[] {
                0, 1, 2, 0, 2, 3,       // Top
                4, 5, 6, 4, 6, 7,       // Front
                8, 9, 10, 10, 11, 8,    // Left
                12, 14, 13, 12, 15, 14, // Bottom
                17, 16, 19, 17, 19, 18, // Back
                20, 22, 21, 20, 23, 22   // Right
            };


            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(2);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)indexArray.Length;

            return new GlCube(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        internal void ReleaseGlCube()
        {
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
        }
    }
}