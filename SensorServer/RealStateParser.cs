using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WSNUtil;

namespace SensorServer
{
    class RealStateParser
    {
        private DateTime _StartTime;
        private DateTime _EndTime;
        private static char[] CommentIndicators = new char[] { '#', ';' };
        private List<Motion> _Motions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="realStateFile">The order in which motions are entered IS IMPORTANT!</param>
        /// <param name="pollingDelay"></param>
        public RealStateParser(string realStateFile)
        {
            StreamReader Reader = new StreamReader(realStateFile);
            _StartTime = _GetStartTime(Reader);
            Reader.BaseStream.Position = 0;
            Reader.DiscardBufferedData();
            _Motions = _GetMotions(Reader, true);
            Reader.Close();
        }

        public ObjectEstimate GetState(DateTime time)
        {
            if (time < _StartTime)
                return null;
            if (time > _EndTime)
                return null;

            foreach (Motion m in _Motions)
            {
                if (m.StartTime + m.Duration < time)
                {
                    continue;
                }
                System.Diagnostics.Debug.Assert(time >= m.StartTime);
                if (m.NatureOfMotion == "LINEAR")
                {
                    float VelocityX = (m.EndX - m.StartX) / (float)m.Duration.TotalSeconds;
                    float VelocityY = (m.EndY - m.StartY) / (float)m.Duration.TotalSeconds;
                    float TimeMovingInMotion = (float)(time - m.StartTime).TotalSeconds;
                    float PosX = m.StartX + VelocityX * TimeMovingInMotion;
                    float PosY = m.StartY + VelocityY * TimeMovingInMotion;
                    ObjectEstimate RealState = new ObjectEstimate(PosX, PosY, VelocityX, VelocityY);
                    return RealState;
                }
                else if (m.NatureOfMotion == "CURVED")
                {
                    throw new NotImplementedException("CURVED motions are not yet implemented, use LINEAR motions in the meanwhile");
                }
            }

            return null;
        }

        private DateTime _GetStartTime(StreamReader file)
        {
            while (!file.EndOfStream)
            {
                string CurrLine = file.ReadLine();
                string[] EqualsSplit = CurrLine.Split('=');
                if (EqualsSplit.Length != 2)
                    continue;
                if (EqualsSplit[0].Trim().ToUpper() != "STARTTIME")
                    continue;
                string DateInput = EqualsSplit[1].Split(CommentIndicators)[0].Trim();
                DateTime DateOutput;
                if (DateTime.TryParse(DateInput, out DateOutput))
                    return DateOutput;
            }
            throw new Exception("StartTime entry (e.g. \"StartTime = 2014/09/03 13:37:05.220\") not found in RealState file.");
        }

        private List<Motion> _GetMotions(StreamReader file, bool setEndTime)
        {
            DateTime CurrTime = _StartTime;
            List<Motion> MotionsToReturn = new List<Motion>();

            while (!file.EndOfStream)
            {
                string CurrLine = file.ReadLine();
                string[] CommaSplit = CurrLine.Split(',');
                if (CommaSplit.Length != 6)
                    continue;

                string NatureOfMotion = CommaSplit[0].Trim().ToUpper();
                if (NatureOfMotion != "LINEAR" && NatureOfMotion != "CURVED")
                    continue;
                float StartX, StartY, EndX, EndY;
                if (float.TryParse(CommaSplit[1].Trim(), out StartX) == false)
                    continue;
                if (float.TryParse(CommaSplit[2].Trim(), out StartY) == false)
                    continue;
                if (float.TryParse(CommaSplit[3].Trim(), out EndX) == false)
                    continue;
                if (float.TryParse(CommaSplit[4].Trim(), out EndY) == false)
                    continue;
                float Duration;
                if (float.TryParse(CommaSplit[5].Split(CommentIndicators)[0].Trim(), out Duration) == false)
                    continue;
                TimeSpan DurationTimeSpan = new TimeSpan(0, 0, 0, (int)Duration, (int)((Duration * 1000) % 1000));

                Motion NewMotion = new Motion(StartX, StartY, EndX, EndY, CurrTime, DurationTimeSpan, NatureOfMotion);
                CurrTime = CurrTime + DurationTimeSpan;
                MotionsToReturn.Add(NewMotion);
            }
            if (setEndTime)
                _EndTime = CurrTime;

            return MotionsToReturn;
        }

        /// <summary>
        /// A Motion is an atomic movement made by an object. This could movement in a straight line from point A to B, or circular motion from point A to B.
        /// Speed is ASSUMED CONSTANT.
        /// </summary>
        private class Motion
        {
            public float StartX { get; private set; }
            public float StartY { get; private set; }
            public float EndX { get; private set; }
            public float EndY { get; private set; }
            public DateTime StartTime { get; private set; }
            public TimeSpan Duration { get; private set; }
            public string NatureOfMotion { get; private set; } //LINEAR, CURVED

            public Motion(float startX, float startY, float endX, float endY, DateTime startTime, TimeSpan duration, string natureOfMotion)
            {
                StartX = startX;
                StartY = startY;
                EndX = endX;
                EndY = endY;
                StartTime = startTime;
                Duration = duration;
                NatureOfMotion = natureOfMotion.Trim().ToUpper();
            }
        }
    }
}
