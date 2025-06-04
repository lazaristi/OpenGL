using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateSphereWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<(int[] vertexIndices, int[] normalIndices)> objFaces;
            List<float[]> objNormals;

            ReadObjDataForSphere(out objVertices, out objFaces, out objNormals);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, objNormals, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<(int[] vertexIndices, int[] normalIndices)> objFaces, List<float[]> objNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            foreach (var face in objFaces)
            {
                var (vIndices, nIndices) = face;

                Vector3D<float>? computedNormal = null;
                if (nIndices.All(i => i == -1))
                {
                    var vertex0 = objVertices[vIndices[0] - 1];
                    var vertex1 = objVertices[vIndices[1] - 1];
                    var vertex2 = objVertices[vIndices[2] - 1];

                    var a = new Vector3D<float>(vertex0[0], vertex0[1], vertex0[2]);
                    var b = new Vector3D<float>(vertex1[0], vertex1[1], vertex1[2]);
                    var c = new Vector3D<float>(vertex2[0], vertex2[1], vertex2[2]);
                    computedNormal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }

                for (int i = 0; i < vIndices.Length; ++i)
                {
                    float[] vertex = objVertices[vIndices[i] - 1];
                    float[] normal = computedNormal.HasValue
                        ? new float[] { computedNormal.Value.X, computedNormal.Value.Y, computedNormal.Value.Z }
                        : objNormals[nIndices[i] - 1]; // OBJ indices are 1-based

                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(vertex);
                    glVertex.AddRange(normal);

                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices[glVertexStringKey] = glVertexIndices.Count;
                    }

                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }
            }
        }

        public static void ReadObjDataForSphere(
            out List<float[]> objVertices,
            out List<(int[] vertexIndices, int[] normalIndices)> objFaces,
            out List<float[]> objNormals)
        {
            objVertices = new List<float[]>();
            objFaces = new List<(int[] vertexIndices, int[] normalIndices)>();
            objNormals = new List<float[]>();

            using (Stream objStream = typeof(ObjResourceReader).Assembly
                .GetManifestResourceStream("Szeminarium1_24_02_17_2.Resources.sphere.obj"))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    string line = objReader.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("v "))
                    {
                        string[] parts = line.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        float[] vertex = Array.ConvertAll(parts, s => float.Parse(s, CultureInfo.InvariantCulture));
                        objVertices.Add(vertex);
                    }
                    else if (line.StartsWith("vn "))
                    {
                        string[] parts = line.Substring(3).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        float[] normal = Array.ConvertAll(parts, s => float.Parse(s, CultureInfo.InvariantCulture));
                        objNormals.Add(normal);
                    }
                    else if (line.StartsWith("f "))
                    {
                        string[] parts = line.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        int vertexCount = parts.Length;

                        List<int> vIndices = new();
                        List<int> nIndices = new();

                        foreach (string part in parts)
                        {
                            string[] tokens = part.Split('/');

                            // Vertex index mindig van
                            int vIdx = int.Parse(tokens[0]);

                            // Normális index kezelése
                            int nIdx = -1;
                            if (tokens.Length >= 3 && !string.IsNullOrEmpty(tokens[2]))
                            {
                                nIdx = int.Parse(tokens[2]);
                            }

                            vIndices.Add(vIdx);
                            nIndices.Add(nIdx);
                        }

                        // Triangulate using fan triangulation
                        for (int i = 1; i < vertexCount - 1; ++i)
                        {
                            int[] triV = new int[] { vIndices[0], vIndices[i], vIndices[i + 1] };
                            int[] triN = new int[] { nIndices[0], nIndices[i], nIndices[i + 1] };
                            objFaces.Add((triV, triN));
                        }
                    }
                }
            }
        }
    }
}