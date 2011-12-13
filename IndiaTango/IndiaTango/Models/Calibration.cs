using System;
using ProtoBuf;

namespace IndiaTango.Models
{
    [ProtoContract]
    public class Calibration
    {
        public Calibration() { } //For Protobuf

        public Calibration(DateTime timestamp, float preHigh, float preLow, float postHigh, float postLow)
        {
            TimeStamp = timestamp;
            PreHigh = preHigh;
            PreLow = preLow;
            PostHigh = postHigh;
            PostLow = postLow;
        }

        [ProtoMember(1)]
        public DateTime TimeStamp { get; private set; }

        [ProtoMember(2)]
        public float PreHigh { get; private set; }
        public float PreSpan { get { return PreHigh; } }
        [ProtoMember(3)]
        public float PreLow { get; private set; }
        public float PreOffset { get { return PreLow; } }

        [ProtoMember(4)]
        public float PostHigh { get; private set; }
        public float PostSpan { get { return PostHigh; } }
        [ProtoMember(5)]
        public float PostLow { get; private set; }
        public float PostOffset { get { return PostLow; } }

        public override string ToString()
        {
            return string.Format("Calibration: {0} Pre: [{1} -{2}] Post: [{3} - {4}] [Offset - Span]", TimeStamp.ToString("yyyy/MM/dd"), PreOffset, PreSpan, PostOffset, PostSpan);
        }
    }
}
