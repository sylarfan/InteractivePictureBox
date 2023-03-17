using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool useInteract = false;

        public event MousePanImageEventHandler OnMouseInteractive;

        [DefaultValue(false)]
        [DisplayName("使用交互")]
        public bool UseInteract
        {
            get { return useInteract; }
            set
            {
                if (useInteract)
                {
                    Restore();
                }
                useInteract = value;
            }
        }

        public PictureBoxEx()
            : base()
        {
            //DoubleBuffered = true;
        }

        //https://stackoverflow.com/questions/6624406/how-to-make-label-transparent-without-any-flickering-at-load-time
        //double buffered
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
        //        return cp;
        //    }
        //}


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
            var tempPointF = TranslatePoint(new PointF(actualPoint.X, actualPoint.Y));
            PointF tempPoint;
            switch (SizeMode)
            {
                case PictureBoxSizeMode.StretchImage:
                    tempPoint = TranslateStretchImageMousePosition(tempPointF);
                    break;
                case PictureBoxSizeMode.CenterImage:
                    tempPoint = TranslateCenterImageMousePosition(tempPointF);
                    break;
                case PictureBoxSizeMode.Zoom:
                    tempPoint = TranslateZoomMousePosition(tempPointF);
                    break;
                case PictureBoxSizeMode.Normal:
                case PictureBoxSizeMode.AutoSize:
                default:
                    tempPoint = tempPointF;
                    break;
            }
            return new Point((int)tempPoint.X, (int)tempPoint.Y);
        }

        public Point GetImagePointCenter(Point point)
        {
            var tempPointF = TranslatePoint(new PointF(point.X, point.Y));
            PointF tempPoint;
            switch (SizeMode)
            {
                case PictureBoxSizeMode.StretchImage:
                    tempPoint = TranslateStretchImageMousePosition(tempPointF);
                    break;
                case PictureBoxSizeMode.CenterImage:
                    tempPoint = TranslateCenterImageMousePosition(tempPointF);
                    break;
                case PictureBoxSizeMode.Zoom:
                    tempPoint = TranslateZoomMousePosition(tempPointF);
                    break;
                case PictureBoxSizeMode.Normal:
                case PictureBoxSizeMode.AutoSize:
                default:
                    tempPoint = tempPointF;
                    break;
            }
            var size = Image.Size;
            return new Point((int)tempPoint.X - size.Width / 2 + 1, (int)tempPoint.Y - size.Height / 2 + 1);
        }



        public void Pan(float offsetX, float offsetY)
        {
            service.Pan(offsetX, offsetY);
            Invalidate();
        }

        public void Zoom(float scale, PointF zoomCenter)
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
            //set_Image has internal Invalidate();
            //https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/PictureBox.cs,594784cfbb39d6e8
        }



        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Image != null)
            {
                var canMove = false;
                if (OnMouseInteractive != null)
                {
                    var earg = new PanZoomImageEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta, MouseAction.Pan);
                    OnMouseInteractive(this, earg);
                    canMove = !earg.Cancel;
                }
                else if (useInteract)
                {
                    canMove = true;
                }
                if (canMove)
                {
                    isMoving = true;
                    prevPoint = e.Location;
                }
                if (!Focused)
                {
                    Focus();
                }
            }
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
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isMoving && useInteract)
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
            //if (!Focused)
            //{
            //    Focus();
            //}
            base.OnMouseMove(e);
        }



        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Image != null && useInteract)
            {
                var point = TranslatePoint(new PointF(e.Location.X, e.Location.Y));
                var canZoom = false;
                if (OnMouseInteractive != null)
                {
                    var earg = new PanZoomImageEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta, MouseAction.Zoom);
                    OnMouseInteractive(this, earg);
                    canZoom = !earg.Cancel;
                }
                else
                {
                    canZoom = true;
                }
                if (canZoom)
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
            }
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            if (useInteract)
            {
                service.ApplyTransform(pe.Graphics);
            }
            base.OnPaint(pe);
        }


        #region TranslatePosition
        //https://www.codeproject.com/Articles/20923/Mouse-Position-over-Image-in-a-PictureBox


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

        protected PointF TranslateCenterImageMousePosition(PointF coordinates)
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

        protected PointF TranslateStretchImageMousePosition(PointF coordinates)
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
            return new PointF(newX, newY);
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

        protected PointF TranslateZoomMousePosition(PointF coordinates)
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
            return new PointF(newX, newY);
        }


        #endregion

    }


    internal class TransformService
    {
        private float scale;
        private float scaleOffsetX;
        private float scaleOffsetY;
        private float translateX;
        private float translateY;

        private bool matrixChanged = false;

        private Matrix matrixInvert = new Matrix();
        private readonly Matrix matrix = new Matrix();
        private readonly Point[] singlePoint = new Point[1];
        private readonly PointF[] singlePointF = new PointF[1];

        public float ScaleMax { get; set; } = 1000;

        public float Scale { get { return scale; } }

        public TransformService()
        {
            scale = 1f;
            scaleOffsetX = scaleOffsetY = translateX = translateY = 0f;
        }

        public void Pan(float offsetX, float offsetY)
        {
            matrixChanged = true;
            translateX += offsetX;
            translateY += offsetY;
        }

        public void Zoom(float zoomFactor, PointF zoomCenter)
        {
            matrixChanged = true;
            var oldScale = scale;
            scale = CoerceZoom(scale * zoomFactor);

            var deltaScale = scale - oldScale;
            float deltaX = -zoomCenter.X * deltaScale;
            float deltaY = -zoomCenter.Y * deltaScale;

            scaleOffsetX += deltaX;
            scaleOffsetY += deltaY;
        }

        public void Restore()
        {
            scale = 1;
            scaleOffsetX = scaleOffsetY = translateX = translateY = 0f;
            matrixChanged = true;
        }

        public void ApplyTransform(Graphics gs)
        {
            EnsureMatrix();
            gs.Transform = matrix;
        }

        #region Translate
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
        #endregion



        private void EnsureMatrix()
        {
            if (matrixChanged)
            {
                matrix.Reset();
                matrix.Scale(scale, scale, MatrixOrder.Append);
                matrix.Translate(scaleOffsetX, scaleOffsetY, MatrixOrder.Append);
                matrix.Translate(translateX, translateY, MatrixOrder.Append);

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
            if (zoom > ScaleMax)
            {
                zoom = ScaleMax;
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

    public delegate void MousePanImageEventHandler(object sender, PanZoomImageEventArgs e);

    public class PanZoomImageEventArgs : CancelEventArgs
    {

        // 参数:
        //   button:
        //     System.Windows.Forms.MouseButtons 值之一，它指示曾按下的是哪个鼠标按钮。
        //
        //   clicks:
        //     鼠标按钮曾被按下的次数。
        //
        //   x:
        //     鼠标单击的 x 坐标（以像素为单位）。
        //
        //   y:
        //     鼠标单击的 y 坐标（以像素为单位）。
        //
        //   delta:
        //     鼠标轮已转动的制动器数的有符号计数。
        public PanZoomImageEventArgs(MouseButtons button, int clicks, int x, int y, int delta, MouseAction mouseAction)
        {
            Button = button;
            Clicks = clicks;
            X = x;
            Y = y;
            Delta = delta;
            Location = new Point(x, y);
            MouseAction = mouseAction;
        }

        public MouseAction MouseAction { get; private set; }

        //
        // 摘要:
        //     获取曾按下的是哪个鼠标按钮。
        //
        // 返回结果:
        //     System.Windows.Forms.MouseButtons 值之一。
        public MouseButtons Button { get; private set; }
        //
        // 摘要:
        //     获取按下并释放鼠标按钮的次数。
        //
        // 返回结果:
        //     一个 System.Int32，包含按下并释放鼠标按钮的次数。
        public int Clicks { get; private set; }
        //
        // 摘要:
        //     获取鼠标在产生鼠标事件时的 x 坐标。
        //
        // 返回结果:
        //     鼠标的 X 坐标（以像素为单位）。
        public int X { get; private set; }
        //
        // 摘要:
        //     获取鼠标在产生鼠标事件时的 y 坐标。
        //
        // 返回结果:
        //     鼠标的 Y 坐标（以像素为单位）。
        public int Y { get; private set; }
        //
        // 摘要:
        //     获取鼠标轮已转动的制动器数的有符号计数。制动器是鼠标轮的一个凹口。
        //
        // 返回结果:
        //     鼠标轮已转动的制动器数的有符号计数。
        public int Delta { get; private set; }
        //
        // 摘要:
        //     获取鼠标在产生鼠标事件时的位置。
        //
        // 返回结果:
        //     一个 System.Drawing.Point，其中包含鼠标相对于窗体左上角的 x 坐标和 y 坐标（以像素为单位）。
        public Point Location { get; private set; }
    }

    public enum MouseAction
    {
        Pan, Zoom
    }
}
