using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        private double DistanceToOrigin = 30;

        private double AngleToZYPlane = 0;

        private double AngleToZXPlane = Math.PI/2;


        private bool sideView = true;
        private Vector3D<float> rectanglePosition = new Vector3D<float>(0, 0, 0);

        /// <summary>
        /// Gets the position of the camera.
        /// </summary>
        public Vector3D<float> Position
        {
            get
            {
                if(sideView)
                {
                    return GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane);
                }
                else
                {
                    return new Vector3D<float>(rectanglePosition.X,-3,rectanglePosition.Z);
                }
                
            }
        }

        public void updateRectPos(Vector3D<float> rectPos)
        {
            rectanglePosition = rectPos;
        }

        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                if(sideView)
                {
                    return Vector3D.Normalize(GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane + Math.PI / 2));

                }
                else
                {
                    return Vector3D<float>.UnitZ;
                }
            }
        }

        /// <summary>
        /// Gets the target point of the camera view.
        /// </summary>
        public Vector3D<float> Target
        {
            get
            {
                if (sideView)
                {
                    return Vector3D<float>.Zero;

                }
                else
                {
                    return new Vector3D<float>(rectanglePosition.X, rectanglePosition.Y+50, rectanglePosition.Z);
                }
            }
        }

        public void toggleView()
        {
            sideView = !sideView;
        }

        

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}
