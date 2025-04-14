using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;


namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static IWindow window;
        private static GL Gl;
        private static ImGuiController imGuiController;
        private static uint program;
        private static List<GlCube> rubikCubes = new();
        private static List<Matrix4X4<float>> rubikCubeMatrices = new();
        private static IInputContext inputContext;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";

        // Fény paraméterek
        private static Vector3 lightColor = new Vector3(1f, 1f, 1f);
        private static Vector3 lightPosition = new Vector3(2f, 2f, 2f);
        private static float shininess = 32f;

        private static List<int> topFaceCubeIndices = new();
        private static bool topFaceAnimating = false;
        private static float topFaceCurrentAngle = 0f;
        private static float topFaceTargetAngle = 0f;
        private static float topFaceRotationSpeed = (float)(Math.PI / 2);

        static void Main(string[] args)
        {
            WindowOptions options = WindowOptions.Default;
            options.Title = "Rubik's Cube";
            options.Size = new Vector2D<int>(1000, 800);
            options.PreferredDepthBufferBits = 24;

            window = Window.Create(options);
            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;
            window.Run();
        }

        private static void Window_Load()
        {
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            imGuiController = new ImGuiController(Gl, window, inputContext);

            Gl.ClearColor(System.Drawing.Color.LightGray);
            SetUpObjects();
            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            float deltaTime = 1f;
            switch (key)
            {
                case Key.Up:
                    cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Forward, deltaTime);
                    break;
                case Key.Down:
                    cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Backward, deltaTime);
                    break;
                case Key.Left:
                    cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Left, deltaTime);
                    break;
                case Key.Right:
                    cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Right, deltaTime);
                    break;
                case Key.A:
                    cameraDescriptor.Yaw -= cameraDescriptor.RotationSpeed;
                    break;
                case Key.D:
                    cameraDescriptor.Yaw += cameraDescriptor.RotationSpeed;
                    break;
                case Key.W:
                    cameraDescriptor.Pitch += cameraDescriptor.RotationSpeed;
                    cameraDescriptor.Pitch = Math.Clamp(cameraDescriptor.Pitch, -89f, 89f);
                    break;
                case Key.S:
                    cameraDescriptor.Pitch -= cameraDescriptor.RotationSpeed;
                    cameraDescriptor.Pitch = Math.Clamp(cameraDescriptor.Pitch, -89f, 89f);
                    break;
                case Key.Q:
                    cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Down, deltaTime);
                    break;
                case Key.E:
                    cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Up, deltaTime);
                    break;
                case Key.Space:
                    if (!topFaceAnimating)
                    {
                        topFaceTargetAngle = topFaceCurrentAngle + (float)(Math.PI / 2);
                        topFaceAnimating = true;
                    }
                    break;
                case Key.Backspace:
                    if (!topFaceAnimating)
                    {
                        topFaceTargetAngle = topFaceCurrentAngle - (float)(Math.PI / 2);
                        topFaceAnimating = true;
                    }
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            imGuiController.Update((float)deltaTime);

            if (topFaceAnimating)
            {
                float angleStep = topFaceRotationSpeed * (float)deltaTime;
                if (topFaceCurrentAngle < topFaceTargetAngle)
                {
                    topFaceCurrentAngle += angleStep;
                    if (topFaceCurrentAngle >= topFaceTargetAngle)
                    {
                        topFaceCurrentAngle = topFaceTargetAngle;
                        topFaceAnimating = false;
                    }
                }
                else if (topFaceCurrentAngle > topFaceTargetAngle)
                {
                    topFaceCurrentAngle -= angleStep;
                    if (topFaceCurrentAngle <= topFaceTargetAngle)
                    {
                        topFaceCurrentAngle = topFaceTargetAngle;
                        topFaceAnimating = false;
                    }
                }
            }
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            // Fény paraméterek beállítása
            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, lightPosition);
            SetUniform3(ViewPositionVariableName, new Vector3(cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            SetViewMatrix();
            SetProjectionMatrix();

            for (int i = 0; i < rubikCubes.Count; i++)
            {
                Matrix4X4<float> model = rubikCubeMatrices[i];

                if (topFaceCubeIndices.Contains(i))
                {
                    Matrix4X4<float> center = Matrix4X4.CreateTranslation(0, 0.35f, 0);
                    Matrix4X4<float> invCenter = Matrix4X4.CreateTranslation(0, -0.35f, 0);
                    Matrix4X4<float> rotation = Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0, 1, 0), topFaceCurrentAngle);
                    Matrix4X4<float> faceRotation = center * rotation * invCenter;
                    model = model * faceRotation;
                }

                SetModelMatrix(model);
                Gl.BindVertexArray(rubikCubes[i].Vao);
                Gl.DrawElements(GLEnum.Triangles, rubikCubes[i].IndexArrayLength, GLEnum.UnsignedInt, null);
            }
            Gl.BindVertexArray(0);

            RenderImGui();
            imGuiController.Render();
        }

        private static void RenderImGui()
        {
            ImGuiNET.ImGui.Begin("Fény és kamera beállítások");

            // Fény színének beállítása
            ImGuiNET.ImGui.Text("Fény színe:");
            Vector3 lightColorVec = new Vector3(lightColor.X, lightColor.Y, lightColor.Z);
            if (ImGuiNET.ImGui.ColorEdit3("RGB", ref lightColorVec))
            {
                lightColor = new Vector3(lightColorVec.X, lightColorVec.Y, lightColorVec.Z);
            }

            // Fény pozíciójának beállítása 
            ImGuiNET.ImGui.Text("Fény pozíciója:");
            Vector3 lightPosVec = new Vector3(lightPosition.X, lightPosition.Y, lightPosition.Z);
            if (ImGuiNET.ImGui.InputFloat3("XYZ", ref lightPosVec))
            {
                lightPosition = new Vector3(lightPosVec.X, lightPosVec.Y, lightPosVec.Z);
            }

            // Fényesség beállítása
            ImGuiNET.ImGui.SliderFloat("Fenyesseg", ref shininess, 5f, 100f);

            // Kamera vezérlés gombok
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Kamera vezerlés:");

            if (ImGuiNET.ImGui.Button("Elore"))
                cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Forward, 1f);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Hatra"))
                cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Backward, 1f);

            if (ImGuiNET.ImGui.Button("Balra"))
                cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Left, 1f);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Jobbra"))
                cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Right, 1f);

            if (ImGuiNET.ImGui.Button("Fel"))
                cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Up, 1f);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Le"))
                cameraDescriptor.ProcessKeyboard(CameraDescriptor.MovementDirection.Down, 1f);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Forgatás:");
            if (ImGuiNET.ImGui.Button("Balra forgat"))
                cameraDescriptor.Yaw -= cameraDescriptor.RotationSpeed;
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Jobbra forgat"))
                cameraDescriptor.Yaw += cameraDescriptor.RotationSpeed;

            if (ImGuiNET.ImGui.Button("Fel forgat"))
            {
                cameraDescriptor.Pitch += cameraDescriptor.RotationSpeed;
                cameraDescriptor.Pitch = Math.Clamp(cameraDescriptor.Pitch, -89f, 89f);
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Le forgat"))
            {
                cameraDescriptor.Pitch -= cameraDescriptor.RotationSpeed;
                cameraDescriptor.Pitch = Math.Clamp(cameraDescriptor.Pitch, -89f, 89f);
            }

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Rubik-kocka forgatás:");
            if (ImGuiNET.ImGui.Button("Felso lap balra") && !topFaceAnimating)
            {
                topFaceTargetAngle = topFaceCurrentAngle + (float)(Math.PI / 2);
                topFaceAnimating = true;
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Felso lap jobbra") && !topFaceAnimating)
            {
                topFaceTargetAngle = topFaceCurrentAngle - (float)(Math.PI / 2);
                topFaceAnimating = true;
            }

            ImGuiNET.ImGui.End();
        }

        private static unsafe void SetUpObjects()
        {
            float[] grey = new float[] { 0.1f, 0.1f, 0.1f, 1.0f };
            float[] front = new float[] { 0.0f, 0.7f, 0.0f, 1.0f };
            float[] back = new float[] { 0.0f, 0.0f, 0.7f, 1.0f };
            float[] left = new float[] { 0.8f, 0.4f, 0.0f, 1.0f };
            float[] right = new float[] { 0.7f, 0.0f, 0.0f, 1.0f };
            float[] top = new float[] { 0.9f, 0.9f, 0.9f, 1.0f };
            float[] bottom = new float[] { 0.7f, 0.7f, 0.0f, 1.0f };

            float size = 0.33f;
            float offset = 0.35f;
            int index = 0;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        bool isFront = z == 1, isBack = z == -1,
                             isLeft = x == -1, isRight = x == 1,
                             isTop = y == 1, isBottom = y == -1;

                        float[] f1 = isTop ? top : grey,
                                f2 = isFront ? front : grey,
                                f3 = isLeft ? left : grey,
                                f4 = isBottom ? bottom : grey,
                                f5 = isBack ? back : grey,
                                f6 = isRight ? right : grey;

                        var cube = GlCube.CreateCubeWithFaceColors(Gl, f1, f2, f3, f4, f5, f6);
                        rubikCubes.Add(cube);

                        Matrix4X4<float> model =
                            Matrix4X4.CreateScale(size) *
                            Matrix4X4.CreateTranslation(x * offset, y * offset, z * offset);
                        rubikCubeMatrices.Add(model);

                        if (y == 1)
                        {
                            topFaceCubeIndices.Add(index);
                        }
                        index++;
                    }
                }
            }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> matrix)
        {
            int loc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&matrix);
        }

        private static unsafe void SetViewMatrix()
        {
            Matrix4X4<float> view = Matrix4X4.CreateLookAt(
                cameraDescriptor.Position,
                cameraDescriptor.Position + cameraDescriptor.Front,
                cameraDescriptor.Up
            );
            int loc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&view);
        }

        private static unsafe void SetProjectionMatrix()
        {
            Matrix4X4<float> proj = Matrix4X4.CreatePerspectiveFieldOfView(
                (float)Math.PI / 4f,
                window.Size.X / (float)window.Size.Y,
                0.1f,
                100f
            );
            int loc = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&proj);
        }

        private static void LinkProgram()
        {
            // Vertex shader
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 vPos;
                layout (location = 1) in vec4 vCol;
                layout (location = 2) in vec3 vNormal;

                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProjection;

                out vec4 outCol;
                out vec3 outNormal;
                out vec3 outWorldPosition;
                
                void main()
                {
                    outCol = vCol;
                    outNormal = mat3(transpose(inverse(uModel))) * vNormal;
                    outWorldPosition = vec3(uModel * vec4(vPos, 1.0));
                    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
                }";

            // Fragment shader
            string fragmentShaderSource = @"
                #version 330 core
                out vec4 FragColor;

                uniform vec3 uLightColor;
                uniform vec3 uLightPos;
                uniform vec3 uViewPos;
                uniform float uShininess;
                
                in vec4 outCol;
                in vec3 outNormal;
                in vec3 outWorldPosition;

                void main()
                {
                    // Ambient
                    float ambientStrength = 0.1;
                    vec3 ambient = ambientStrength * uLightColor;
                    
                    // Diffuse 
                    vec3 norm = normalize(outNormal);
                    vec3 lightDir = normalize(uLightPos - outWorldPosition);
                    float diff = max(dot(norm, lightDir), 0.0);
                    vec3 diffuse = diff * uLightColor;
                    
                    // Specular
                    float specularStrength = 0.5;
                    vec3 viewDir = normalize(uViewPos - outWorldPosition);
                    vec3 reflectDir = reflect(-lightDir, norm);  
                    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
                    vec3 specular = specularStrength * spec * uLightColor;  
                    
                    vec3 result = (ambient + diffuse + specular) * outCol.rgb;
                    FragColor = vec4(result, outCol.a);
                }";

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vshader, vertexShaderSource);
            Gl.CompileShader(vshader);

            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fshader, fragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 value)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1) return;
            Gl.Uniform3(location, value);
        }

        private static unsafe void SetUniform1(string uniformName, float value)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1) return;
            Gl.Uniform1(location, value);
        }

        private static void Window_Closing()
        {
            foreach (var cube in rubikCubes)
                cube.ReleaseGlCube();
        }
    }
}