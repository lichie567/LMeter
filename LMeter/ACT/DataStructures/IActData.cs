using System;

namespace LMeter.Act.DataStructures
{
    public interface IActData<T>
    {
        static readonly Random Random = new();

        static abstract string[] TextTags { get; }
        static abstract T GetTestData();

        string GetFormattedString(string format, string numberFormat, bool emptyIfZero);
    }
}
