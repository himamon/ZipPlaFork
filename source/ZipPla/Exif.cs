using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public class Exif
    {
        private ushort tag;
        public ushort Tag { get { return tag; } }
        private uint value;
        public uint Value { get { return value; } }

        public static Exif[] GetAll(Stream stream)
        {
            try
            {
                using (var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true))
                {
                    if (reader.ReadUInt32() != 0xe1ffd8ff) return null;
                    var app1Size = reader.ReadUInt16();
                    if (new string(reader.ReadChars(4)) != "Exif") return null;
                    if (reader.ReadUInt16() != 0) return null;
                    bool bigEndian;
                    switch (new string(reader.ReadChars(2)))
                    {
                        case "MM": bigEndian = true; break;
                        case "II": bigEndian = false; break;
                        default: return null;
                    }
                    if (convertEndian(reader.ReadUInt16(), bigEndian) != 0x002a) return null;
                    if (convertEndian(reader.ReadUInt32(), bigEndian) != 0x00000008) return null;
                    var tagCount = convertEndian(reader.ReadUInt16(), bigEndian);
                    var result = new Exif[tagCount];
                    for (var i = 0; i < tagCount; i++)
                    {
                        var tag = convertEndian(reader.ReadUInt16(), bigEndian);
                        //var type = convertEndian(reader.ReadUInt16(), bigEndian);
                        reader.ReadUInt16();
                        //var valueCount = convertEndian(reader.ReadUInt32(), bigEndian);
                        reader.ReadUInt32();
                        var value = convertEndian(reader.ReadUInt32(), bigEndian);
                        result[i] = new Exif { tag = tag, value = value };
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        private static ushort convertEndian(ushort x, bool reverse)
        {
            if (reverse) return (ushort)(x << 8 | x >> 8);
            else return x;
        }

        private static uint convertEndian(uint x, bool reverse)
        {
            if (reverse) return x << 24 | (x & 0xff00) << 8 | (x & 0xff0000) >> 8 | x >> 24;
            else return x;
        }
    }
}
