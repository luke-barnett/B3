using System;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// Object to represent a calibration point
    /// </summary>
    [ProtoContract]
    public class Calibration
    {
        public Calibration() { } //For Protobuf

        public Calibration(DateTime timestamp, float prePoint1, float prePoint2, float prePoint3, float postPoint1, float postPoint2, float postPoint3)
        {
            TimeStamp = timestamp;
            PreCalibrationPoint1 = prePoint1;
            PreCalibrationPoint2 = prePoint2;
            PreCalibrationPoint3 = prePoint3;

            PostCalibrationPoint1 = postPoint1;
            PostCalibrationPoint2 = postPoint2;
            PostCalibrationPoint3 = postPoint3;
        }

        [ProtoMember(1)]
        public DateTime TimeStamp { get; private set; }

        [ProtoMember(2)]
        public float PreCalibrationPoint1 { get; private set; }
        [ProtoMember(3)]
        public float PreCalibrationPoint2 { get; private set; }
        [ProtoMember(6)]
        public float PreCalibrationPoint3 { get; private set; }

        [ProtoMember(4)]
        public float PostCalibrationPoint1 { get; private set; }
        [ProtoMember(5)]
        public float PostCalibrationPoint2 { get; private set; }
        [ProtoMember(7)]
        public float PostCalibrationPoint3 { get; private set; }

        public override string ToString()
        {
            return string.Format("Calibration: {0} Pre: [{1} - {2} - {3}] Post: [{4} - {5} - {6}] [Point1 - Point2 - Point3]", TimeStamp.ToString("yyyy/MM/dd"), PreCalibrationPoint1, PreCalibrationPoint2, PreCalibrationPoint3, PostCalibrationPoint1, PostCalibrationPoint2, PostCalibrationPoint3);
        }
    }
}
