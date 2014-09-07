using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;
using System.Drawing;

namespace DisplayServer
{
    class StateDrawer
    {
        private List<ObjectEstimate> _RawData; //List of raw data positions to draw
        private List<ObjectEstimate> _StateEstimate; //List of estimated object positions to draw
        private List<ObjectEstimate> _AdditionalStateInfo; //List of addition object positions to draw
        private List<ObjectEstimate> _RealState; //List of real object positions to draw
        private List<ObjectEstimate> _TrilateratedEstimate; //List of all valid trilateration results
        private List<Sensor> _Sensors; //List of sensors to draw (both their locations and FoVs)
        public float LowerXBound { get; private set; } //The minimum value in the X axis for the real world measurements
        public float UpperXBound { get; private set; } //The maximum value in the X axis for the real world measurements
        public float LowerYBound { get; private set; } //The minimum value in the Y axis for the real world measurements
        public float UpperYBound { get; private set; } //The maximum value in the Y axis for the real world measurements
        private float _LengthX; //Length of the x dimension in the real world (i.e. UpperXBound + LowerXBound)
        private float _LengthY; //Length of the y dimension in the real world (i.e. UpperYBound + LowerYBound)
        private float _RatioX; //Ratio of real world upper/lower x bound and bitmap width
        private float _RatioY; //Ratio of real world upper/lower y bound and bitmap height
        public int Width { get; private set; } //Width of the bitmap
        public int Height { get; private set; } //Height of the bitmap
        private bool _Draw1To1;
        private bool _DrawRealPosition;
        private bool _DrawAdditionalInfo;
        private bool _DrawEstimatedPosition;
        private bool _DrawTrilateratedPosition;
        private bool _DrawMeasurements;
        private bool _DrawSensors;

        public StateDrawer(List<ObjectEstimate> rawData, List<ObjectEstimate> stateEstimate, List<ObjectEstimate> additionalStateInfo, List<ObjectEstimate> realState, List<ObjectEstimate> trilatereatedEstimate, int width, int height, string csvFile, string iniFile)
        {
            _Draw1To1 = Variables.GetDrawDisplayServerDrawXY1To1(iniFile);
            _DrawRealPosition = Variables.GetDisplayServerDrawReal(iniFile);
            _DrawAdditionalInfo = Variables.GetDisplayServerDrawAdditional(iniFile);
            _DrawEstimatedPosition = Variables.GetDisplayServerDrawEsimated(iniFile);
            _DrawTrilateratedPosition = Variables.GetDisplayServerDrawTrilaterated(iniFile);
            _DrawMeasurements = Variables.GetDisplayServerDrawMeasurements(iniFile);
            _DrawSensors = Variables.GetDisplayServerDrawSensors(iniFile);

            Width = width;
            Height = height;

            if (_DrawMeasurements)
                _RawData = rawData;
            else
                _RawData = new List<ObjectEstimate>();
            if (_DrawEstimatedPosition)
                _StateEstimate = stateEstimate;
            else
                _StateEstimate = new List<ObjectEstimate>();
            if (_DrawAdditionalInfo)
                _AdditionalStateInfo = additionalStateInfo;
            else
                _AdditionalStateInfo = new List<ObjectEstimate>();
            if (_DrawRealPosition)
                _RealState = realState;
            else
                _RealState = new List<ObjectEstimate>();
            if (_DrawTrilateratedPosition)
                _TrilateratedEstimate = trilatereatedEstimate;
            else
                _TrilateratedEstimate = new List<ObjectEstimate>();
            if (_DrawSensors)
                _Sensors = Variables.GetSensorConfig(csvFile);
            else
                _Sensors = new List<Sensor>();

            LowerXBound = -1;
            UpperXBound = 1;
            LowerYBound = -1;
            UpperYBound = 1;
            _SetAllBounds();
        }

        public void SetStates(List<ObjectEstimate> rawData, List<ObjectEstimate> stateEstimate, List<ObjectEstimate> additionalStateInfo, List<ObjectEstimate> realState, List<ObjectEstimate> trilatereatedEstimate)
        {
            _RawData = rawData;
            _StateEstimate = stateEstimate;
            _AdditionalStateInfo = additionalStateInfo;
            _RealState = realState;
            _TrilateratedEstimate = trilatereatedEstimate;

            _SetAllBounds();
        }

        public Bitmap DrawState()
        {
            Bitmap Bmp = new Bitmap(Width, Height);
            Graphics Gfx = Graphics.FromImage(Bmp);
            Gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            _DrawBackground(Gfx, Brushes.WhiteSmoke);
            _DrawAxes(Gfx);
            _DrawLegend(Gfx);
            if (_DrawSensors)
                foreach (Sensor Sen in _Sensors)
                    _DrawSensor(Gfx, Sen);
            if (_DrawAdditionalInfo)
                foreach (ObjectEstimate Obj in _AdditionalStateInfo)
                    _DrawObject(Gfx, Obj, Brushes.Yellow, 15f);
            if (_DrawTrilateratedPosition)
                foreach (ObjectEstimate Obj in _TrilateratedEstimate)
                    _DrawObject(Gfx, Obj, Brushes.Blue, 15f);
            if (_DrawRealPosition)
                foreach (ObjectEstimate Obj in _RealState)
                    _DrawObject(Gfx, Obj, Brushes.Green, 15f);
            if (_DrawMeasurements)
                foreach (ObjectEstimate Obj in _RawData)
                    _DrawObject(Gfx, Obj, Brushes.Gray, 8f);
            if (_DrawEstimatedPosition)
                foreach (ObjectEstimate Obj in _StateEstimate)
                    _DrawObject(Gfx, Obj, Brushes.Purple, 15f);

            return Bmp;
        }

        private Image Legend = Image.FromFile("./Legend.png");
        private void _DrawLegend(Graphics gfx)
        {
            gfx.DrawImage(Legend, new Point(0, 0));
        }

        private void _DrawBackground(Graphics gfx, Brush color)
        {
            gfx.FillRectangle(color, 0, 0, Width, Height);
        }

        private void _DrawAxes(Graphics gfx)
        {
            // Draw the axes lines
            Pen PenAxes = new Pen(Color.Black, 1);
            // X Axis
            gfx.DrawLine(PenAxes, new PointF(0f, _RatioY * (UpperYBound)), new PointF(Width, _RatioY * (UpperYBound)));
            // Y Axis
            gfx.DrawLine(PenAxes, new PointF(_RatioX * (-LowerXBound), 0f), new PointF(_RatioX * (-LowerXBound), Height));
            PenAxes.Dispose();

            // Draw the coordinates labels
            Font FontAxes = new Font(new FontFamily("Lucida Console"), 11, FontStyle.Regular);
            Brush BrushAxes = Brushes.Black;
            gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            // Center point
            gfx.DrawString("(0,0)", FontAxes, BrushAxes, new PointF(_RatioX * (-LowerXBound), _RatioY * (UpperYBound) + 4));
            // Right of X Axis
            string ToWrite = string.Format("({0},0)", (int)UpperXBound);
            gfx.DrawString(ToWrite, FontAxes, BrushAxes, new PointF(Width - ToWrite.Length * 10, _RatioY * (UpperYBound) - 18));
            // Left of X Axis
            ToWrite = string.Format("({0},0)", (int)LowerXBound);
            gfx.DrawString(ToWrite, FontAxes, BrushAxes, new PointF(0, _RatioY * (UpperYBound) - 18));
            // Top of Y Axis
            ToWrite = string.Format("(0,{0})", (int)UpperYBound);
            gfx.DrawString(ToWrite, FontAxes, BrushAxes, new PointF(_RatioX * (-LowerXBound) - ToWrite.Length * 10, 4));
            // Bottom of Y Axis
            ToWrite = string.Format("(0,{0})", (int)LowerYBound);
            gfx.DrawString(ToWrite, FontAxes, BrushAxes, new PointF(_RatioX * (-LowerXBound) - ToWrite.Length * 10, Height - 20));
        }

        private void _DrawSensor(Graphics gfx, Sensor toDraw)
        {
            Pen PenSensor = new Pen(Color.Red, 1);
            Brush BrushSensor = Brushes.Red;
            const float CircleWidth  = 10f;
            const float CircleHeight = CircleWidth;
            _DrawCircle(gfx, toDraw.X, toDraw.Y, CircleWidth, CircleHeight, BrushSensor);

            PenSensor.DashPattern = new float[] { 5f, 15f };
            PenSensor.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            // Draw the right FoV line
            PointF StartPoint = new PointF(_RatioX * (-LowerXBound + toDraw.X), _RatioY * (UpperYBound - toDraw.Y));
            float xPos1 = _RatioX * (-LowerXBound + toDraw.X + toDraw.Distance * (float)Math.Cos((toDraw.Phi - toDraw.Theta / 2) * Math.PI / 180));
            float yPos1 = _RatioY * (UpperYBound - toDraw.Y - toDraw.Distance * (float)Math.Sin((toDraw.Phi - toDraw.Theta / 2) * Math.PI / 180));
            PointF EndPoint1 = new PointF(xPos1, yPos1);
            gfx.DrawLine(PenSensor, StartPoint, EndPoint1);

            //Draw the left FoV line
            float xPos2 = _RatioX * (-LowerXBound + toDraw.X + toDraw.Distance * (float)Math.Cos((toDraw.Phi + toDraw.Theta / 2) * Math.PI / 180));
            float yPos2 = _RatioY * (UpperYBound - toDraw.Y - toDraw.Distance * (float)Math.Sin((toDraw.Phi + toDraw.Theta / 2) * Math.PI / 180));
            PointF EndPoint2 = new PointF(xPos2, yPos2);
            gfx.DrawLine(PenSensor, StartPoint, EndPoint2);
        }

        private void _DrawObject(Graphics gfx, ObjectEstimate toDraw, Brush brush, float circleRadius)
        {
            _DrawCircle(gfx, toDraw.X, toDraw.Y, circleRadius, circleRadius, brush);
        }

        private void _DrawCircle(Graphics gfx, float x, float y, float circleWidth, float circleHeight, Brush brushCircle)
        {
            //gfx.DrawArc(pen, new RectangleF(new PointF(_RatioX * (-LowerXBound + x) - circleWidth / 2, _RatioY * (UpperYBound - y) - circleHeight / 2), new SizeF(circleWidth, circleHeight)), 0, 360);
            gfx.FillEllipse(brushCircle, new RectangleF(new PointF(_RatioX * (-LowerXBound + x) - circleWidth / 2, _RatioY * (UpperYBound - y) - circleHeight / 2), new SizeF(circleWidth, circleHeight)));
        }

        /// <summary>
        /// Find the lowest and highest values for each dimensions, useful for when determing dimensions of container objects for drawing
        /// </summary>
        /// <param name="RawData"></param>
        /// <param name="StateEstimate"></param>
        private void _SetAllBounds() //TODO: Set bounds to be a minimum of a percentage of the opposing bound rather than +-30
        {
            _SetLowerXBound();
            _SetUpperXBound();
            _SetLowerYBound();
            _SetUpperYBound();
            if (_Draw1To1) //Make X and Y bounds equal to their greater counterparts.
            {
                UpperXBound = UpperXBound < UpperYBound ? UpperYBound : UpperXBound;
                UpperYBound = UpperYBound < UpperXBound ? UpperXBound : UpperYBound;
                LowerXBound = LowerXBound < LowerYBound ? LowerYBound : LowerXBound;
                LowerYBound = LowerYBound < LowerXBound ? LowerXBound : LowerYBound;
            }
            _LengthX = UpperXBound - LowerXBound;
            _LengthY = UpperYBound - LowerYBound;
            _RatioX = Width / _LengthX;
            _RatioY = Height / _LengthY;
        }

        const int BORDER = 30;
        private void _SetLowerXBound()
        {
            foreach (ObjectEstimate est in _RawData)
                if (est.X - BORDER < LowerXBound)
                    LowerXBound = est.X - BORDER;
            foreach (ObjectEstimate est in _StateEstimate)
                if (est.X - BORDER < LowerXBound)
                    LowerXBound = est.X - BORDER;
            foreach (ObjectEstimate est in _AdditionalStateInfo)
                if (est.X - BORDER < LowerXBound)
                    LowerXBound = est.X - BORDER;
            foreach (ObjectEstimate est in _RealState)
                if (est.X - BORDER < LowerXBound)
                    LowerXBound = est.X - BORDER;
            foreach (ObjectEstimate est in _TrilateratedEstimate)
                if (est.X - BORDER < LowerXBound)
                    LowerXBound = est.X - BORDER;
            foreach (Sensor Sen in _Sensors)
                if (Sen.X - BORDER < LowerXBound)
                    LowerXBound = Sen.X - BORDER;
        }

        private void _SetUpperXBound()
        {
            foreach (ObjectEstimate est in _RawData)
                if (est.X + BORDER > UpperXBound)
                    UpperXBound = est.X + BORDER;
            foreach (ObjectEstimate est in _StateEstimate)
                if (est.X + BORDER > UpperXBound)
                    UpperXBound = est.X + BORDER;
            foreach (ObjectEstimate est in _AdditionalStateInfo)
                if (est.X + BORDER > UpperXBound)
                    UpperXBound = est.X + BORDER;
            foreach (ObjectEstimate est in _RealState)
                if (est.X + BORDER > UpperXBound)
                    UpperXBound = est.X + BORDER;
            foreach (ObjectEstimate est in _TrilateratedEstimate)
                if (est.X + BORDER > UpperXBound)
                    UpperXBound = est.X + BORDER;
            foreach (Sensor Sen in _Sensors)
                if (Sen.X + BORDER > UpperXBound)
                    UpperXBound = Sen.X + BORDER;
        }

        private void _SetLowerYBound()
        {
            foreach (ObjectEstimate est in _RawData)
                if (est.Y - BORDER < LowerYBound)
                    LowerYBound = est.Y - BORDER;
            foreach (ObjectEstimate est in _StateEstimate)
                if (est.Y - BORDER < LowerYBound)
                    LowerYBound = est.Y - BORDER;
            foreach (ObjectEstimate est in _AdditionalStateInfo)
                if (est.Y - BORDER < LowerYBound)
                    LowerYBound = est.Y - BORDER;
            foreach (ObjectEstimate est in _RealState)
                if (est.Y - BORDER < LowerYBound)
                    LowerYBound = est.Y - BORDER;
            foreach (ObjectEstimate est in _TrilateratedEstimate)
                if (est.Y - BORDER < LowerYBound)
                    LowerYBound = est.Y - BORDER;
            foreach (Sensor Sen in _Sensors)
                if (Sen.Y - BORDER < LowerYBound)
                    LowerYBound = Sen.Y - BORDER;
        }

        private void _SetUpperYBound()
        {
            foreach (ObjectEstimate est in _RawData)
                if (est.Y + BORDER > UpperYBound)
                    UpperYBound = est.Y + BORDER;
            foreach (ObjectEstimate est in _StateEstimate)
                if (est.Y + BORDER > UpperYBound)
                    UpperYBound = est.Y + BORDER;
            foreach (ObjectEstimate est in _AdditionalStateInfo)
                if (est.Y + BORDER > UpperYBound)
                    UpperYBound = est.Y + BORDER;
            foreach (ObjectEstimate est in _RealState)
                if (est.Y + BORDER > UpperYBound)
                    UpperYBound = est.Y + BORDER;
            foreach (ObjectEstimate est in _TrilateratedEstimate)
                if (est.Y + BORDER > UpperYBound)
                    UpperYBound = est.Y + BORDER;
            foreach (Sensor Sen in _Sensors)
                if (Sen.Y + BORDER > UpperYBound)
                    UpperYBound = Sen.Y + BORDER;
        }
    }
}