using System;
using System.Windows.Media;

/// <summary>
/// Taken from
/// http://stackoverflow.com/questions/3315088/how-do-i-serialize-a-system-windows-media-color-object-to-an-srgb-string
/// </summary>
[Serializable]
public struct Colour
{
    public byte A;
    public byte R;
    public byte G;
    public byte B;

    public Colour(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public Colour(Color color)
        : this(color.A, color.R, color.G, color.B)
    {
    }

    public static implicit operator Colour(Color color)
    {
        return new Colour(color);
    }

    public static implicit operator Color(Colour colour)
    {
        return Color.FromArgb(colour.A, colour.R, colour.G, colour.B);
    }
}