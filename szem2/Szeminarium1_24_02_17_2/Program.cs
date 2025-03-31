using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static IWindow window;
        private static GL Gl;
        private static uint program;
        private static List<GlCube> rubikCubes = new();
        private static List<Matrix4X4<float>> rubikCubeMatrices = new();
        private static IInputContext inputContext;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        out vec4 outCol;
        void main()
        {
            outCol = vCol;
            gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
        }";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        in vec4 outCol;
        void main()
        {
            FragColor = outCol;
        }";

        static void Main(string[] args)
        {
            WindowOptions options = WindowOptions.Default;
            options.Title = "Rubik's Cube";
            options.Size = new Vector2D<int>(800, 600);
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
            cameraDescriptor.IncreaseDistance();
            cameraDescriptor.IncreaseDistance();
            cameraDescriptor.IncreaseDistance();
            cameraDescriptor.IncreaseZXAngle();
            cameraDescriptor.IncreaseZXAngle();

            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.LightGray);
            SetUpObjects();
            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left: cameraDescriptor.DecreaseZYAngle(); break;
                case Key.Right: cameraDescriptor.IncreaseZYAngle(); break;
                case Key.Up: cameraDescriptor.DecreaseDistance(); break;
                case Key.Down: cameraDescriptor.IncreaseDistance(); break;
                case Key.U: cameraDescriptor.IncreaseZXAngle(); break;
                case Key.D: cameraDescriptor.DecreaseZXAngle(); break;
            }
        }

        private static void Window_Update(double deltaTime) { }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            for (int i = 0; i < rubikCubes.Count; i++)
            {
                SetModelMatrix(rubikCubeMatrices[i]);
                Gl.BindVertexArray(rubikCubes[i].Vao);
                Gl.DrawElements(GLEnum.Triangles, rubikCubes[i].IndexArrayLength, GLEnum.UnsignedInt, null);
            }
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetUpObjects()
        {
            float[] grey = [0.1f, 0.1f, 0.1f, 1.0f];
            float[] front = [0.0f, 0.7f, 0.0f, 1.0f];  // Green
            float[] back = [0.0f, 0.0f, 0.7f, 1.0f];   // Blue
            float[] left = [0.8f, 0.4f, 0.0f, 1.0f];   // Orange
            float[] right = [0.7f, 0.0f, 0.0f, 1.0f];  // Red
            float[] top = [0.9f, 0.9f, 0.9f, 1.0f];    // White
            float[] bottom = [0.7f, 0.7f, 0.0f, 1.0f]; // Yellow

            float size = 0.33f; 
            float offset = 0.35f; 

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
            var view = Matrix4X4.CreateLookAt(
                cameraDescriptor.Position,
                cameraDescriptor.Target,
                cameraDescriptor.UpVector
            );
            int loc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&view);
        }

        private static unsafe void SetProjectionMatrix()
        {
            var proj = Matrix4X4.CreatePerspectiveFieldOfView(
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
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);

            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Window_Closing()
        {
            foreach (var cube in rubikCubes)
                cube.ReleaseGlCube();
        }
    }
}