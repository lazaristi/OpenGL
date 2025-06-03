using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class ModelObjectDescriptor:IDisposable
    {
        private bool disposedValue;

        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint? Texture { get; private set; } = new uint?();
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }

        private GL Gl;

        public unsafe static ModelObjectDescriptor CreateCube(GL Gl)
        {
            // counter clockwise is front facing
            float[] vertexArray = new float[] {
                // top face
                -0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                0.5f, 0.5f, -0.5f, 0f, 1f, 0f,
                -0.5f, 0.5f, -0.5f, 0f, 1f, 0f, 

                // front face
                -0.5f, 0.5f, 0.5f, 0f, 0f, 1f,
                -0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                0.5f, 0.5f, 0.5f, 0f, 0f, 1f,

                // left face
                -0.5f, 0.5f, 0.5f, -1f, 0f, 0f,
                -0.5f, 0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, 0.5f, -1f, 0f, 0f,

                // bottom face
                -0.5f, -0.5f, 0.5f, 0f, -1f, 0f,
                0.5f, -0.5f, 0.5f,0f, -1f, 0f,
                0.5f, -0.5f, -0.5f,0f, -1f, 0f,
                -0.5f, -0.5f, -0.5f,0f, -1f, 0f,

                // back face
                0.5f, 0.5f, -0.5f, 0f, 0f, -1f,
                -0.5f, 0.5f, -0.5f,0f, 0f, -1f,
                -0.5f, -0.5f, -0.5f,0f, 0f, -1f,
                0.5f, -0.5f, -0.5f,0f, 0f, -1f,

                // right face
                0.5f, 0.5f, 0.5f, 1f, 0f, 0f,
                0.5f, 0.5f, -0.5f,1f, 0f, 0f,
                0.5f, -0.5f, -0.5f,1f, 0f, 0f,
                0.5f, -0.5f, 0.5f,1f, 0f, 0f,
            };

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,

                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,

                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            return CreateObjectDescriptorFromArrays(Gl, vertexArray, colorArray, indexArray);

        }

        public unsafe static ModelObjectDescriptor CreateSkyBox(GL Gl)
        {
            // counter clockwise is front facing
            // vx, vy, vz, nx, ny, nz, tu, tv
            float[] vertexArray = new float[] {
                // top face
                -0.5f, 0.5f, 0.5f, 0f, -1f, 0f, 1f/4f, 0f/3f,
                0.5f, 0.5f, 0.5f, 0f, -1f, 0f, 2f/4f, 0f/3f,
                0.5f, 0.5f, -0.5f, 0f, -1f, 0f, 2f/4f, 1f/3f,
                -0.5f, 0.5f, -0.5f, 0f, -1f, 0f, 1f/4f, 1f/3f,

                // front face
                -0.5f, 0.5f, 0.5f, 0f, 0f, -1f, 1, 1f/3f,
                -0.5f, -0.5f, 0.5f, 0f, 0f, -1f, 4f/4f, 2f/3f,
                0.5f, -0.5f, 0.5f, 0f, 0f, -1f, 3f/4f, 2f/3f,
                0.5f, 0.5f, 0.5f, 0f, 0f, -1f,  3f/4f, 1f/3f,

                // left face
                -0.5f, 0.5f, 0.5f, 1f, 0f, 0f, 0, 1f/3f,
                -0.5f, 0.5f, -0.5f, 1f, 0f, 0f,1f/4f, 1f/3f,
                -0.5f, -0.5f, -0.5f, 1f, 0f, 0f, 1f/4f, 2f/3f,
                -0.5f, -0.5f, 0.5f, 1f, 0f, 0f, 0f/4f, 2f/3f,

                // bottom face
                -0.5f, -0.5f, 0.5f, 0f, 1f, 0f, 1f/4f, 1f,
                0.5f, -0.5f, 0.5f,0f, 1f, 0f, 2f/4f, 1f,
                0.5f, -0.5f, -0.5f,0f, 1f, 0f, 2f/4f, 2f/3f,
                -0.5f, -0.5f, -0.5f,0f, 1f, 0f, 1f/4f, 2f/3f,

                // back face
                0.5f, 0.5f, -0.5f, 0f, 0f, 1f, 2f/4f, 1f/3f,
                -0.5f, 0.5f, -0.5f, 0f, 0f, 1f, 1f/4f, 1f/3f,
                -0.5f, -0.5f, -0.5f,0f, 0f, 1f, 1f/4f, 2f/3f,
                0.5f, -0.5f, -0.5f,0f, 0f, 1f, 2f/4f, 2f/3f,

                // right face
                0.5f, 0.5f, 0.5f, -1f, 0f, 0f, 3f/4f, 1f/3f,
                0.5f, 0.5f, -0.5f,-1f, 0f, 0f, 2f/4f, 1f/3f,
                0.5f, -0.5f, -0.5f, -1f, 0f, 0f, 2f/4f, 2f/3f,
                0.5f, -0.5f, 0.5f, -1f, 0f, 0f, 3f/4f, 2f/3f,
            };

            float[] colorArray = new float[] {
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] {
                0, 2, 1,
                0, 3, 2,

                4, 6, 5,
                4, 7, 6,

                8, 10, 9,
                10, 8, 11,

                12, 13, 14,
                12, 14, 15,

                17, 19, 16,
                17, 18, 19,

                20, 21, 22,
                20, 22, 23
            };

            var skyboxImage = ReadTextureImage("skybox.png");

            return CreateObjectDescriptorFromArrays(Gl, vertexArray, colorArray, indexArray, skyboxImage);
        }

        private static unsafe ModelObjectDescriptor CreateObjectDescriptorFromArrays(GL Gl, float[] vertexArray, float[] colorArray, uint[] indexArray,
            ImageResult textureImage = null)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            // 0 is position
            // 2 is normals
            // 3 is texture
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint offsetTexture = offsetNormals + 3 * sizeof(float);
            uint vertexSize = offsetTexture + (textureImage == null ? 0u : 2 * sizeof(float));

            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexture);
            Gl.EnableVertexAttribArray(3);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);


            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            // 1 is color
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            uint? texture = new uint?();

            if (textureImage != null)
            {
                // set texture
                // create texture
                texture = Gl.GenTexture();

                // activate texture 0
                Gl.ActiveTexture(TextureUnit.Texture0);
                // bind texture
                Gl.BindTexture(TextureTarget.Texture2D, texture.Value);
                // Here we use "result.Width" and "result.Height" to tell OpenGL about how big our texture is.
                Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)textureImage.Width,
                    (uint)textureImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (ReadOnlySpan<byte>)textureImage.Data.AsSpan());
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                // unbinde texture
                Gl.BindTexture(TextureTarget.Texture2D, 0);
            }

            return new ModelObjectDescriptor() { Vao = vao, Vertices = vertices, Colors = colors, Indices = indices, IndexArrayLength = (uint)indexArray.Length, Gl = Gl, Texture = texture };
        }

        private static unsafe ImageResult ReadTextureImage(string textureResource)
        {
            ImageResult result;
            using (Stream skyeboxStream
                = typeof(ModelObjectDescriptor).Assembly.GetManifestResourceStream("GrafikaSzeminarium.Resources." + textureResource))
                result = ImageResult.FromStream(skyeboxStream, ColorComponents.RedGreenBlueAlpha);

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null


                // always unbound the vertex buffer first, so no halfway results are displayed by accident
                Gl.DeleteBuffer(Vertices);
                Gl.DeleteBuffer(Colors);
                Gl.DeleteBuffer(Indices);
                Gl.DeleteVertexArray(Vao);

                disposedValue = true;
            }
        }

        ~ModelObjectDescriptor()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal static ModelObjectDescriptor CreateTeapot(GL Gl)
        {
            List<float[]> objVertices = new List<float[]>();
            List<int[]> objFaces = new List<int[]>();

            string fullResourceName = "GrafikaSzeminarium.Resources.teapot.obj";
            using (var objStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName))
            using (var objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;
                        case "f":
                            int[] face = new int[3];
                            for (int i = 0; i < face.Length; ++i)
                                face[i] = int.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objFaces.Add(face);
                            break;
                        default:
                            throw new Exception("Unhandled obj structure.");
                    }
                }
            }

            List<ObjVertexTransformationData> vertexTransformations = new List<ObjVertexTransformationData>();
            foreach (var objVertex in objVertices)
            {
                vertexTransformations.Add(new ObjVertexTransformationData(
                    new Vector3D<float>(objVertex[0], objVertex[1], objVertex[2]),
                    Vector3D<float>.Zero,
                    0
                    ));
            }

            foreach (var objFace in objFaces)
            {
                var a = vertexTransformations[objFace[0] - 1];
                var b = vertexTransformations[objFace[1] - 1];
                var c = vertexTransformations[objFace[2] - 1];

                var normal = Vector3D.Normalize(Vector3D.Cross(b.Coordinates - a.Coordinates, c.Coordinates - a.Coordinates));

                a.UpdateNormalWithContributionFromAFace(normal);
                b.UpdateNormalWithContributionFromAFace(normal);
                c.UpdateNormalWithContributionFromAFace(normal);
            }


            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            foreach (var vertexTransformation in vertexTransformations)
            {
                glVertices.Add(vertexTransformation.Coordinates.X);
                glVertices.Add(vertexTransformation.Coordinates.Y);
                glVertices.Add(vertexTransformation.Coordinates.Z);

                glVertices.Add(vertexTransformation.Normal.X);
                glVertices.Add(vertexTransformation.Normal.Y);
                glVertices.Add(vertexTransformation.Normal.Z);

                glColors.AddRange([1.0f, 0.0f, 0.0f, 1.0f]);
            }

            List<uint> glIndexArray = new List<uint>();
            foreach (var objFace in objFaces)
            {
                glIndexArray.Add((uint)(objFace[0] - 1));
                glIndexArray.Add((uint)(objFace[1] - 1));
                glIndexArray.Add((uint)(objFace[2] - 1));
            }

            return CreateObjectDescriptorFromArrays(Gl, glVertices.ToArray(), glColors.ToArray(), glIndexArray.ToArray());
        }
    }
}
