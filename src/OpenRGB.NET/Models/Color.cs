using System;
using OpenRGB.NET.Utils;

namespace OpenRGB.NET
{
    /// <summary>
    ///     Represents a color.
    /// </summary>
    public readonly struct Color
    {
        /// <summary>
        /// Creates a color.
        /// </summary>
        /// <param name="r">The Red component</param>
        /// <param name="g">The Green component</param>
        /// <param name="b">The Blue component</param>
        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        ///     Red value of the color.
        /// </summary>
        public byte R { get; }

        /// <summary>
        ///     Green value of the color.
        /// </summary>
        public byte G { get; }

        /// <summary>
        ///     Blue value of the color.
        /// </summary>
        public byte B { get; }

        private static Color ReadFrom(ref SpanReader reader)
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var _ = reader.ReadByte();

            return new Color(r, g, b);
        }

        internal static Color[] ReadManyFrom(ref SpanReader reader, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 0.");

            if (count == 0)
                return Array.Empty<Color>();

            var colors = new Color[count];

            for (var i = 0; i < count; i++)
                colors[i] = ReadFrom(ref reader);

            return colors;
        }

        internal void WriteTo(ref SpanWriter writer)
        {
            writer.WriteByte(R);
            writer.WriteByte(G);
            writer.WriteByte(B);
            writer.WriteByte(0);
        }
    }
}