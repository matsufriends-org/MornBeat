namespace MornBeat
{
    public readonly struct MornBeatSetInfo
    {
        public readonly MornBeatMemoSo BeatMemo;
        public readonly double StartDspTime;

        public MornBeatSetInfo(MornBeatMemoSo beatMemo, double startDspTime)
        {
            BeatMemo = beatMemo;
            StartDspTime = startDspTime;
        }
    }
}