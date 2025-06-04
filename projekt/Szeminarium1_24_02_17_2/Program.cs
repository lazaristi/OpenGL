using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static Vector3D<float> platformPosition = new Vector3D<float>(0, 0, 0);
        private const float platformSpeed = 5.0f;

        private static IWindow window;
        private static IInputContext inputContext;
        private static GL Gl;
        private static ImGuiController controller;
        private static uint program;
        private static GlCube skyBox;
        private static GlCube platform;
        private static GlObject teapot;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static bool wPressed = false;
        private static bool aPressed = false;
        private static bool sPressed = false;
        private static bool dPressed = false;

        private static List<Vector3D<float>> fallingTeapots = new List<Vector3D<float>>();
        private static double timeSinceLastTeapot = 0;
        private static Random random = new Random();
        private const float teapotFallSpeed = 4f;

        private static int score = 0;
        private static int misses = 0;
        private static bool gameOver = false;




        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Projekt";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
                keyboard.KeyUp += Keyboard_KeyReleased;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };
            skyBox = GlCube.CreateInteriorCube(Gl, "skybox1.png");
            float[] platformColor = { 0.2f, 0.6f, 1.0f, 1.0f };
            platform = GlCube.CreatePlatform(Gl, platformColor, 2.0f);


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(GLEnum.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static string ReadShader(string shaderFileName)
        {
            using (Stream shaderStream = typeof(Program).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Shaders." + shaderFileName))
            using (StreamReader shaderReader = new StreamReader(shaderStream))
                return shaderReader.ReadToEnd();
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.W:
                    wPressed = true;
                    break;
                case Key.A:
                    aPressed = true;
                    break;
                case Key.S:
                    sPressed = true;
                    break;
                case Key.D:
                    dPressed = true;
                    break;
                case Key.Space:
                    cameraDescriptor.toggleView();
                    break;
            }
        }

        private static void Keyboard_KeyReleased(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.W:
                    wPressed = false;
                    break;
                case Key.A:
                    aPressed = false;
                    break;
                case Key.S:
                    sPressed = false;
                    break;
                case Key.D:
                    dPressed = false;
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            // Platform mozgatása a nyilakkal
            float speed = (float)(platformSpeed * deltaTime) + 0.05f;
            if (aPressed && platformPosition.X >=-14.6f && !gameOver)
            {
                platformPosition.X -= speed;
            }
            if (dPressed && platformPosition.X <= 14.6f && !gameOver)
            {
                platformPosition.X += speed;
            }
            if (wPressed && platformPosition.Z >= -10.4f && !gameOver)
            {
                platformPosition.Z -= speed;
            }
            if (sPressed && platformPosition.Z <= 10.4f && !gameOver)
            {
                platformPosition.Z += speed;
            }
            cameraDescriptor.updateRectPos(platformPosition);
            controller.Update((float)deltaTime);

            if (!gameOver)
            {
                // Spawn new teapots
                timeSinceLastTeapot += deltaTime;
                if (timeSinceLastTeapot >= 4.0)
                {
                    float x = (float)(random.NextDouble() * 16 - 8); 
                    float z = (float)(random.NextDouble() * 16 - 8);
                    fallingTeapots.Add(new Vector3D<float>(x, 20, z));
                    timeSinceLastTeapot = 0;
                }

                // Update teapot positions
                for (int i = fallingTeapots.Count - 1; i >= 0; i--)
                {
                    var pos = fallingTeapots[i];
                    pos.Y -= (float)(teapotFallSpeed * deltaTime);
                    fallingTeapots[i] = pos;

                    // Collision detection
                    if (pos.Y <= 0)
                    {
                        if (pos.X >= platformPosition.X - 2.5f && pos.X <= platformPosition.X + 2.5f &&
                            pos.Z >= platformPosition.Z - 2.5f && pos.Z <= platformPosition.Z + 2.5f)
                        {
                            score++;
                        }
                        else
                        {
                            misses++;
                            if (misses >= 5) gameOver = true;
                        }
                        fallingTeapots.RemoveAt(i);
                    }
                }
            }
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();

            DrawSkyBox();

            // Platform rajzolása (új)
            DrawPlatform();

            foreach (var pos in fallingTeapots)
            {
                Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(0.01f) *
                                              Matrix4X4.CreateTranslation(pos);
                SetModelMatrix(modelMatrix);
                Gl.BindVertexArray(teapot.Vao);
                Gl.DrawElements(GLEnum.Triangles, teapot.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }



            DrawGameUI();
            controller.Render();

            
        }

        private static void DrawGameUI()
        {
            ImGui.Begin("Game Controls");

            // Toggle View button
            if (ImGui.Button("Toggle View"))
            {
                cameraDescriptor.toggleView();
            }

            // Game stats
            ImGui.Text($"Score: {score}");
            ImGui.Text($"Misses: {misses}/5");

            // Game over message
            if (gameOver)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "GAME OVER!");
                if (ImGui.Button("Restart"))
                {
                    score = 0;
                    misses = 0;
                    fallingTeapots.Clear();
                    gameOver = false;
                }
            }

            ImGui.End();
        }

        private static unsafe void DrawPlatform()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateTranslation(platformPosition);
            SetModelMatrix(modelMatrix);

            Gl.BindVertexArray(platform.Vao);
            Gl.DrawElements(GLEnum.Triangles, platform.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawSkyBox()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(600f);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(skyBox.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 10f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        

        

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }


        private static void SetUpObjects()
        {
            // Csak skybox és platform maradt
            skyBox = GlCube.CreateInteriorCube(Gl, "skybox1.png");

            float[] platformColor = { 0.2f, 0.6f, 1.0f, 1.0f };
            platform = GlCube.CreatePlatform(Gl, platformColor, 2.0f);

            float[] face1Color = [1f, 0f, 0f, 1.0f];
            teapot = ObjResourceReader.CreateSphereWithColor(Gl, face1Color);
        }



        private static void Window_Closing()
        {
            platform.ReleaseGlObject();

        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 1000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}