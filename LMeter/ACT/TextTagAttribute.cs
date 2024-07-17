using System;

namespace LMeter.Act
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TextTagAttribute(string tagName = "") : Attribute
    {
        string TagName { get; } = tagName;
    }
}