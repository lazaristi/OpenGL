using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static ImGuiController imGuiController;
        private static ModelObjectDescriptor cube;
        private static CameraDescriptor camera = new CameraDescriptor();
        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";
        private const string AmbientStrengthName = "uAmbientStrength";
        private const string DiffuseStrengthName = "uDiffuseStrength";
        private const string SpecularStrengthName = "uSpecularStrength";

        private static float shininess = 50;
        private static Vector3 ambientStrength = new Vector3(0.1f);
        private static Vector3 diffuseStrength = new Vector3(0.3f);
        private static Vector3 specularStrength = new Vector3(0.6f);
        private static Vector3 backgroundColor = new Vector3(1f, 1f, 1f);

        private static int selectedFaceIndex = 0;
        private static readonly string[] faceNames = new[] { "Top", "Front", "Left", "Bottom", "Back", "Right" };
        private static readonly Vector4[] faceColors = new Vector4[]
        {
            new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 1.0f, 0.0f, 1.0f)
        };

        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Vector2D<int>(800, 600);

            graphicWindow = Window.Create(windowOptions);
            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;
            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            cube.Dispose();
            Gl.DeleteProgram(program);
            imGuiController.Dispose();
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();
            var inputContext = graphicWindow.CreateInput();

            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            graphicWindow.FramebufferResize += s => Gl.Viewport(s);

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);
            cube = ModelObjectDescriptor.CreateCube(Gl);

            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1f);
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;
            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                return resStreamReader.ReadToEnd();
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left: camera.DecreaseZYAngle(); break;
                case Key.Right: camera.IncreaseZYAngle(); break;
                case Key.Down: camera.IncreaseDistance(); break;
                case Key.Up: camera.DecreaseDistance(); break;
                case Key.U: camera.IncreaseZXAngle(); break;
                case Key.D: camera.DecreaseZXAngle(); break;
                case Key.Space: cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled; break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, new Vector3(0f, 1.2f, 0f));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);
            SetUniform3(AmbientStrengthName, ambientStrength);
            SetUniform3(DiffuseStrengthName, diffuseStrength);
            SetUniform3(SpecularStrengthName, specularStrength);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2),
                (float)graphicWindow.Size.X / graphicWindow.Size.Y, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            var modelMatrixCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(modelMatrixCenterCube);
            DrawModelObject(cube);

            Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
            Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            Matrix4X4<float> roty = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeLocalAngle);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
            Matrix4X4<float> rotGlobalY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeGlobalYAngle);
            Matrix4X4<float> dimondCubeModelMatrix = diamondScale * rotx * rotz * roty * trans * rotGlobalY;
            SetModelMatrix(dimondCubeModelMatrix);
            DrawModelObject(cube);

            RenderUI();

            imGuiController.Render();
        }

        private static void RenderUI()
        {
            ImGuiNET.ImGui.Begin("Lighting Controls", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize);

            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Illumination Strengths:");
            ImGuiNET.ImGui.SliderFloat("Ambient R", ref ambientStrength.X, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Ambient G", ref ambientStrength.Y, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Ambient B", ref ambientStrength.Z, 0, 1);

            ImGuiNET.ImGui.SliderFloat("Diffuse R", ref diffuseStrength.X, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Diffuse G", ref diffuseStrength.Y, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Diffuse B", ref diffuseStrength.Z, 0, 1);

            ImGuiNET.ImGui.SliderFloat("Specular R", ref specularStrength.X, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Specular G", ref specularStrength.Y, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Specular B", ref specularStrength.Z, 0, 1);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Background Color:");
            ImGuiNET.ImGui.ColorEdit3("BG Color", ref backgroundColor);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Center Cube Face Color:");
            ImGuiNET.ImGui.Combo("Face", ref selectedFaceIndex, faceNames, faceNames.Length);

            Vector4 currentColor = faceColors[selectedFaceIndex];
            if (ImGuiNET.ImGui.ColorEdit4("Color", ref currentColor))
            {
                faceColors[selectedFaceIndex] = currentColor;
                UpdateCubeFaceColor(selectedFaceIndex, currentColor);
            }

            ImGuiNET.ImGui.End();
        }

        private static unsafe void UpdateCubeFaceColor(int faceIndex, Vector4 newColor)
        {
            if (faceIndex < 0 || faceIndex >= 6) return;

            int colorStartIndex = faceIndex * 4 * 4;

            Gl.BindBuffer(GLEnum.ArrayBuffer, cube.Colors);

            for (int i = 0; i < 4; i++)
            {
                int byteOffset = (colorStartIndex + i * 4) * sizeof(float);
                Gl.BufferSubData(GLEnum.ArrayBuffer, (IntPtr)byteOffset,
                    (ReadOnlySpan<float>)new[] { newColor.X, newColor.Y, newColor.Z, newColor.W });
            }

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1) return;

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4.Invert(modelMatrixWithoutTranslation, out var modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location != -1) Gl.Uniform1(location, uniformValue);
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location != -1) Gl.Uniform3(location, uniformValue);
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location != -1) Gl.UniformMatrix4(location, 1, false, (float*)&mx);
        }
    }
}