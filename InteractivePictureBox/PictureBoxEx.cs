using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace InteractivePictureBox
{
    public class PictureBoxEx : PictureBox
    {
        private readonly TransformService service = new TransformService();
        private bool isMoving = false;
        private Point prevPoint;

        public Point TranslatePoint(Point actualPoint)
        {
            return service.TranslatePoint(actualPoint);
        }

        public PointF TranslatePoint(PointF actualPoint)
        {
            return service.TranslatePoint(actualPoint);
        }

        public void TranslatePoints(Point[] actualPoints)
        {
            service.TranslatePoints(actualPoints);
        }

        public void TranslatePoints(PointF[] actualPoints)
        {
            service.TranslatePoints(actualPoints);
        }

        public Point GetImagePoint(Point actualPoint)
        {
            var tempPoint = TranslatePoint(actualPoint);
            switch (SizeMode)
            {
                case PictureBoxSizeMode.StretchImage:
                    return TranslateStretchImageMousePosition(tempPoint);
                case PictureBoxSizeMode.CenterImage:
                    return TranslateCenterImageMousePosition(tempPoint);
                case PictureBoxSizeMode.Zoom:
                    return TranslateZoomMousePosition(tempPoint);
                case PictureBoxSizeMode.Normal:
                case PictureBoxSizeMode.AutoSize:
                default:
                    return tempPoint;
            }
        }

        public void Pan(float offsetX, float offsetY)
        {
            service.Pan(offsetX, offsetY);
            Invalidate();
        }

        public void Zoom(float scale, Point zoomCenter)
        {
            service.Zoom(scale, zoomCenter);
            Invalidate();
        }

        public void Restore()
        {
            service.Restore();
            Invalidate();
        }

        public void ShowImage(Image image, bool remainTransform = true)
        {
            if (!remainTransform)
            {
                service.Restore();
            }
            Image = image;
        }



        protected override void OnMouseDown(MouseEventArgs e)
        {
            isMoving = true;
            prevPoint = e.Location;
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isMoving = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);
            //for mouse wheel event can fire
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isMoving)
            {
                if (Image != null)
                {
                    var newPoint = e.Location;
                    var offsetX = newPoint.X - prevPoint.X;
                    var offsetY = newPoint.Y - prevPoint.Y;
                    prevPoint = newPoint;
                    service.Pan(offsetX, offsetY);
                    Invalidate();
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var point = TranslatePoint(new PointF(e.Location.X, e.Location.Y));
            //Debug.WriteLine($"beform translate:{e.Location},after translate:{point}");
            if (Image != null)
            {
                if (e.Delta > 0)
                {
                    service.Zoom(1.2f, point);
                }
                else
                {
                    service.Zoom(1f / 1.2f, point);
                }
                Invalidate();
            }
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            service.ApplyTransform(pe.Graphics);
            base.OnPaint(pe);
        }


        #region TranslatePosition
        protected Point TranslateCenterImageMousePosition(Point coordinates)
        {
            // Test to make sure our image is not null
            if (Image == null) return coordinates;
            // First, get the top location (relative to the top left of the control) 
            // of the image itself
            // To do this, we know that the image is centered, so we get the difference in size 
            // (width and height) of the image to the control
            int diffWidth = Width - Image.Width;
            int diffHeight = Height - Image.Height;
            // We now divide in half to accommodate each side of the image
            diffWidth /= 2;
            diffHeight /= 2;
            // Finally, we subtract this number from the original coordinates
            // In the case that the image is larger than the picture box, this still works
            coordinates.X -= diffWidth;
            coordinates.Y -= diffHeight;
            return coordinates;
        }

        protected Point TranslateStretchImageMousePosition(Point coordinates)
        {
            // test to make sure our image is not null
            if (Image == null) return coordinates;
            // Make sure our control width and height are not 0
            if (Width == 0 || Height == 0) return coordinates;
            // First, get the ratio (image to control) the height and width
            float ratioWidth = (float)Image.Width / Width;
            float ratioHeight = (float)Image.Height / Height;
            // Scale the points by our ratio
            float newX = coordinates.X;
            float newY = coordinates.Y;
            newX *= ratioWidth;
            newY *= ratioHeight;
            return new Point((int)newX, (int)newY);
        }

        protected Point TranslateZoomMousePosition(Point coordinates)
        {
            // test to make sure our image is not null
            if (Image == null) return coordinates;
            // Make sure our control width and height are not 0 and our 
            // image width and height are not 0
            if (Width == 0 || Height == 0 || Image.Width == 0 || Image.Height == 0) return coordinates;
            // This is the one that gets a little tricky. Essentially, need to check 
            // the aspect ratio of the image to the aspect ratio of the control
            // to determine how it is being rendered
            float imageAspect = (float)Image.Width / Image.Height;
            float controlAspect = (float)Width / Height;
            float newX = coordinates.X;
            float newY = coordinates.Y;
            if (imageAspect > controlAspect)
            {
                // This means that we are limited by width, 
                // meaning the image fills up the entire control from left to right
                float ratioWidth = (float)Image.Width / Width;
                newX *= ratioWidth;
                float scale = (float)Width / Image.Width;
                float displayHeight = scale * Image.Height;
                float diffHeight = Height - displayHeight;
                diffHeight /= 2;
                newY -= diffHeight;
                newY /= scale;
            }
            else
            {
                // This means that we are limited by height, 
                // meaning the image fills up the entire control from top to bottom
                float ratioHeight = (float)Image.Height / Height;
                newY *= ratioHeight;
                float scale = (float)Height / Image.Height;
                float displayWidth = scale * Image.Width;
                float diffWidth = Width - displayWidth;
                diffWidth /= 2;
                newX -= diffWidth;
                newX /= scale;
            }
            return new Point((int)newX, (int)newY);
        }
        #endregion

    }


    internal class TransformService
    {
        private float scale;

        private bool matrixChanged = false;

        private Matrix matrixInvert = new Matrix();
        private readonly Matrix matrix = new Matrix();
        private readonly Point[] singlePoint = new Point[1];
        private readonly PointF[] singlePointF = new PointF[1];

        public float ZoomMax { get; set; } = 1000;

        public TransformService()
        {
            scale = 1;
        }

        public void Pan(float offsetX, float offsetY)
        {
            matrixChanged = true;
            matrix.Translate(offsetX, offsetY, MatrixOrder.Append);
        }

        public void Zoom(float newZoom, PointF newZoomCenter)
        {
            matrixChanged = true;
            scale = newZoom;
            matrix.Translate(-newZoomCenter.X, -newZoomCenter.Y, MatrixOrder.Append);
            matrix.Scale(scale, scale, MatrixOrder.Append);
            matrix.Translate(newZoomCenter.X, newZoomCenter.Y, MatrixOrder.Append);
        }

        public void Restore()
        {
            scale = 1;
            matrix.Reset();
            matrixChanged = true;
        }

        public void ApplyTransform(Graphics gs)
        {
            EnsureMatrix();
            gs.Transform = matrix;
        }

        public Point TranslatePoint(Point point)
        {
            EnsureMatrix();
            singlePoint[0] = point;
            matrixInvert.TransformPoints(singlePoint);
            return singlePoint[0];
        }

        public void TranslatePoints(Point[] points)
        {
            EnsureMatrix();
            matrixInvert.TransformPoints(points);
        }

        public PointF TranslatePoint(PointF point)
        {
            EnsureMatrix();
            singlePointF[0] = point;
            matrixInvert.TransformPoints(singlePointF);
            return singlePointF[0];
        }

        public void TranslatePoints(PointF[] points)
        {
            EnsureMatrix();
            matrixInvert.TransformPoints(points);
        }

        private void EnsureMatrix()
        {
            if (matrixChanged)
            {
                matrixInvert = matrix.Clone();
                matrixInvert.Invert();
                matrixChanged = false;
            }
        }

        internal float CoerceZoom(float baseValue)
        {
            var zoom = baseValue;
            if (zoom < 0.1f)
            {
                zoom = 0.1f;
            }
            if (zoom > ZoomMax)
            {
                zoom = ZoomMax;
            }
            if (zoom.IsNanOrInfinity())
            {
                zoom = 1f;
            }
            return zoom;
        }




    }

    internal static class TransformExtensions
    {
        /// <summary>
		/// Subtracts point2 from point1.
		/// </summary>
		public static PointF Substract(this PointF point1, PointF point2)
        {
            return new PointF(point1.X - point2.X, point1.Y - point2.Y);
        }

        public static PointF Add(this PointF point, float x, float y)
        {
            return new PointF(point.X + x, point.Y + y);
        }



        /// <summary>
        /// Returns the middle point between the given points.
        /// </summary>
        /// <param name="point1">A point.</param>
        /// <param name="point2">Another point.</param>
        /// <returns>Halfway between the two given points.</returns>
        public static PointF MiddlePoint(PointF point1, PointF point2)
        {
            return new PointF((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }



        /// <summary>
        /// Determines whether the specified values are not equal with Epsilon approximation.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        ///   <c>True</c> if the specified values are not equal with Epsilon approximation; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNotEqual(this float value1, float value2)
        {
            //// Perform the calculation. Do not call the IsEqual because it is slower.
            var result = Math.Abs(value1 - value2) >= Epsilon;
            return result;
        }

        /// <summary>
        /// An infinitesimal value.
        /// </summary>
        public const float Epsilon = 1E-06F;

        /// <summary>
        /// Gets whether the value is double or infinity.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNanOrInfinity(this float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }


    }
}
