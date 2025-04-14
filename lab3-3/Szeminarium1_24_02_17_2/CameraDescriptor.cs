using Silk.NET.Maths;
using System;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        public Vector3D<float> Position { get; set; } = new Vector3D<float>(0, 1, 3);
        public float Yaw { get; set; } = -90f;
        public float Pitch { get; set; } = 0f;
        public float MovementSpeed { get; set; } = 0.1f;
        public float RotationSpeed { get; set; } = 5f;

        public Vector3D<float> Front
        {
            get
            {
                double radYaw = Yaw * Math.PI / 180;
                double radPitch = Pitch * Math.PI / 180;
                float x = (float)(Math.Cos(radYaw) * Math.Cos(radPitch));
                float y = (float)(Math.Sin(radPitch));
                float z = (float)(Math.Sin(radYaw) * Math.Cos(radPitch));
                return Vector3D.Normalize(new Vector3D<float>(x, y, z));
            }
        }

        public Vector3D<float> Up { get; set; } = new Vector3D<float>(0, 1, 0);
        public Vector3D<float> Right => Vector3D.Normalize(Vector3D.Cross(Front, Up));

        public void ProcessKeyboard(MovementDirection direction, float deltaTime)
        {
            float velocity = MovementSpeed * deltaTime;
            switch (direction)
            {
                case MovementDirection.Forward:
                    Position += Front * velocity;
                    break;
                case MovementDirection.Backward:
                    Position -= Front * velocity;
                    break;
                case MovementDirection.Left:
                    Position -= Right * velocity;
                    break;
                case MovementDirection.Right:
                    Position += Right * velocity;
                    break;
                case MovementDirection.Up:
                    Position += Up * velocity;
                    break;
                case MovementDirection.Down:
                    Position -= Up * velocity;
                    break;
            }
        }

        public enum MovementDirection { Forward, Backward, Left, Right, Up, Down }
    }
}
