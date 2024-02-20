using LibAPNG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class APngLoader
    {
        public static Bitmap GetAPng(Stream stream)
        {
            TimeSpan[] delays;
            MemoryStream[] pngBinaries;
            fcTLChunk[] fcTLChunks;
            List<Frame> frames;
            IHDRChunk IHDRChunk;
            using (var ms = SeekableStream.Seekablize(stream, long.MaxValue, leaveOpen: true))
            {
                var p = ms.Position;
                Frame defaultImage;
                List<OtherChunk> otherChunks;

                // check file signature.
                if (!Helper.IsBytesEqual(ms.ReadBytes(Frame.Signature.Length), Frame.Signature))
                {
                    //throw new Exception("File signature incorrect.");
                    ms.Position = p;
                    (ms as SeekableStream)?.StopBuffering();
                    return new Bitmap(ms);
                }

                // Read IHDR chunk.
                IHDRChunk = new IHDRChunk(ms);
                if (IHDRChunk.ChunkType != "IHDR")
                    throw new Exception("IHDR chunk must located before any other chunks.");

                var acTLChunk = default(acTLChunk);
                defaultImage = new Frame();
                frames = new List<Frame>();

                // Now let's loop in chunks
                Chunk chunk;
                Frame frame = null;
                otherChunks = new List<OtherChunk>();
                bool isIDATAlreadyParsed = false;
                do
                {
                    if (ms.Position == ms.Length)
                        throw new Exception("IEND chunk expected.");

                    chunk = new Chunk(ms);

                    switch (chunk.ChunkType)
                    {
                        case "IHDR":
                            throw new Exception("Only single IHDR is allowed.");

                        case "acTL":
                            acTLChunk = new acTLChunk(chunk);
                            break;

                        case "IDAT":
                            // To be an APNG, acTL must located before any IDAT and fdAT.
                            if (acTLChunk == null)
                            {
                                ms.Position = p;
                                (ms as SeekableStream)?.StopBuffering();
                                return new Bitmap(ms);
                            }

                            // Only default image has IDAT.
                            defaultImage.IHDRChunk = IHDRChunk;
                            defaultImage.AddIDATChunk(new IDATChunk(chunk));
                            isIDATAlreadyParsed = true;
                            break;

                        case "fcTL":
                            if (frame != null && frame.IDATChunks.Count == 0)
                                throw new Exception("One frame must have only one fcTL chunk.");

                            // IDAT already parsed means this fcTL is used by FRAME IMAGE.
                            if (isIDATAlreadyParsed)
                            {
                                // register current frame object and build a new frame object
                                // for next use
                                if (frame != null)
                                    frames.Add(frame);
                                frame = new Frame
                                {
                                    IHDRChunk = IHDRChunk,
                                    fcTLChunk = new fcTLChunk(chunk)
                                };
                            }
                            // Otherwise this fcTL is used by the DEFAULT IMAGE.
                            else
                            {
                                defaultImage.fcTLChunk = new fcTLChunk(chunk);
                            }
                            break;
                        case "fdAT":
                            // fdAT is only used by frame image.
                            if (frame == null || frame.fcTLChunk == null)
                                throw new Exception("fcTL chunk expected.");

                            frame.AddIDATChunk(new fdATChunk(chunk).ToIDATChunk());
                            break;

                        case "IEND":
                            // register last frame object
                            if (frame != null)
                                frames.Add(frame);

                            if (defaultImage.IDATChunks.Count != 0)
                                defaultImage.IENDChunk = new IENDChunk(chunk);
                            foreach (Frame f in frames)
                            {
                                f.IENDChunk = new IENDChunk(chunk);
                            }
                            break;

                        default:
                            otherChunks.Add(new OtherChunk(chunk));
                            break;
                    }
                } while (chunk.ChunkType != "IEND");

                // We have one more thing to do:
                // If the default image if part of the animation,
                // we should insert it into frames list.
                if (defaultImage.fcTLChunk != null)
                {
                    frames.Insert(0, defaultImage);
                }

                // Now we should apply every chunk in otherChunks to every frame.
                frames.ForEach(f => otherChunks.ForEach(f.AddOtherChunk));

                pngBinaries = new MemoryStream[frames.Count];
                delays = new TimeSpan[frames.Count];
                fcTLChunks = new fcTLChunk[frames.Count];
                try
                {
                    for (var i = 0; i < delays.Length; i++)
                    {
                        var frameOfPng = frames[i];
                        var fctl = frameOfPng.fcTLChunk;

                        pngBinaries[i] = frameOfPng.GetStream();
                        fcTLChunks[i] = fctl;

                        TimeSpan delay;
                        var n = fctl.DelayNum;
                        if (n <= 0)
                        {
                            delay = TimeSpan.FromMilliseconds(50);
                        }
                        else
                        {
                            var d = fctl.DelayDen;
                            if (d <= 0) d = 100;
                            delay = TimeSpan.FromSeconds((double)n / d);
                        }
                        delays[i] = delay;
                    }
                }
                catch
                {
                    for (var i = 0; i < delays.Length; i++)
                    {
                        pngBinaries[i]?.Dispose();
                    }
                    throw;
                }

            }

            return new BitmapEx(new APngProvider(pngBinaries, fcTLChunks, IHDRChunk), delays);
        }

        private class APngProvider : IEnumerator<Bitmap>
        {
            MemoryStream[] binaries;
            fcTLChunk[] fcTLChunks;
            private int frameCount;
            private int currentIndex;
            private bool currentDrawn;
            private Bitmap preBitmap;
            private readonly int width, height;
            public APngProvider(MemoryStream[] binaries, fcTLChunk[] fcTLChunks, IHDRChunk ihdr)
            {
                this.binaries = binaries;
                frameCount = binaries.Length;

                this.fcTLChunks = fcTLChunks;

                width = ihdr.Width;
                height = ihdr.Height;

                currentIndex = -1;
                currentDrawn = true;
            }

            public Bitmap Current
            {
                get
                {
                    var binary = binaries[currentIndex];
                    if (binary == null)
                    {
                        throw new Exception("Current can called only one time.");
                    }

                    Bitmap raw;
                    using (var s = binary)
                    {
                        raw = new Bitmap(binary);
                        s.Close();
                    }
                    binaries[currentIndex] = null;
                    fcTLChunk fctl = fcTLChunks[currentIndex];
                    if (currentIndex == 0 || IsIndependentFrame(fctl))
                    {
                        try
                        {
                            RemoveAlpha(ref raw);

                            /*
                            if (NeedCanvas(fctl))
                            {
                                var temp = raw;
                                try
                                {
                                    raw = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                    try
                                    {
                                        using (var g = Graphics.FromImage(raw))
                                        {
                                            var x = (int)fctl.XOffset;
                                            var y = (int)fctl.YOffset;
                                            if (x > 0 || y > 0 || x + raw.Width < width || y + raw.Height < height) g.FillRectangle(Brushes.White, 0, 0, width, height);
                                            g.DrawImageUnscaled(temp, x, y);
                                        }
                                    }
                                    catch
                                    {
                                        raw.Dispose();
                                        throw;
                                    }
                                }
                                finally
                                {
                                    temp.Dispose();
                                }
                            }
                            */

                            var next = currentIndex + 1;
                            fcTLChunk nextFctl;
                            if (next < frameCount && !IsIndependentFrame(nextFctl = fcTLChunks[next]))
                            {
                                var disposeOp = fctl.DisposeOp;
                                if (disposeOp == DisposeOps.APNGDisposeOpNone)
                                {
                                    preBitmap = ImageLoader.CreateNewBitmap(raw, removeAlpha: false);
                                }
                                else
                                {
                                    preBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                    if (NeedInitialize(nextFctl))
                                    {
                                        try
                                        {
                                            using (var g = Graphics.FromImage(preBitmap))
                                            {
                                                g.FillRectangle(Brushes.White, 0, 0, width, height);
                                            }
                                        }
                                        catch
                                        {
                                            preBitmap.Dispose();
                                            preBitmap = null;
                                            throw;
                                        }
                                    }
                                }
                            }
                            currentDrawn = true;
                            return raw;
                        }
                        catch
                        {
                            raw.Dispose();
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            var bmp = preBitmap;
                            preBitmap = null;
                            try
                            {
                                var disposeOp = fctl.DisposeOp;
                                var next = currentIndex + 1;
                                fcTLChunk nextFctl;
                                if (disposeOp == DisposeOps.APNGDisposeOpPrevious && next < frameCount && !IsIndependentFrame(nextFctl = fcTLChunks[next]))
                                {
                                    if (NeedInitialize(nextFctl))
                                    {
                                        preBitmap = ImageLoader.CreateNewBitmap(bmp, removeAlpha: false);
                                    }
                                    else
                                    {
                                        preBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                    }
                                }

                                if (fctl.BlendOp == BlendOps.APNGBlendOpSource)
                                {
                                    RemoveAlpha(ref raw);
                                }

                                using (var g = Graphics.FromImage(bmp)) g.DrawImageUnscaled(raw, (int)fctl.XOffset, (int)fctl.YOffset);

                                if (disposeOp != DisposeOps.APNGDisposeOpPrevious && next < frameCount && !IsIndependentFrame(nextFctl = fcTLChunks[next]))
                                {
                                    if (NeedInitialize(nextFctl))
                                    {
                                        preBitmap = ImageLoader.CreateNewBitmap(bmp, removeAlpha: false);
                                        if (disposeOp == DisposeOps.APNGDisposeOpBackground && (nextFctl.BlendOp == BlendOps.APNGBlendOpOver || !BoundsOf(nextFctl).Contains(BoundsOf(fctl))))
                                        {
                                            using (var g = Graphics.FromImage(preBitmap))
                                            {
                                                g.FillRectangle(Brushes.White, BoundsOf(fctl));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        preBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                    }
                                }
                            }
                            catch
                            {
                                bmp.Dispose();
                                if (preBitmap != null)
                                {
                                    preBitmap.Dispose();
                                    preBitmap = null;
                                }
                                throw;
                            }
                            currentDrawn = true;
                            return bmp;
                        }
                        finally
                        {
                            raw.Dispose();
                        }
                    }
                }
            }

            bool IsIndependentFrame(fcTLChunk fctl) => fctl.BlendOp == BlendOps.APNGBlendOpSource && fctl.Width == width && fctl.Height == height && fctl.XOffset == 0 && fctl.YOffset == 0;
            //bool NeedCanvas(fcTLChunk fctl) => !(fctl.Width == width && fctl.Height == height && fctl.XOffset == 0 && fctl.YOffset == 0);
            bool NeedInitialize(fcTLChunk fctl)
            {
                if (fctl.BlendOp == BlendOps.APNGBlendOpOver) return true;
                var x = fctl.XOffset; if (x > 0) return true;
                var y = fctl.YOffset; if (y > 0) return true;
                return x + fctl.Width < width || y + fctl.Height < height;
            }
            static Rectangle BoundsOf(fcTLChunk fctl) => new Rectangle((int)fctl.XOffset, (int)fctl.YOffset, (int)fctl.Width, (int)fctl.Height);

            static void RemoveAlpha(ref Bitmap bitmap)
            {
                var pf = bitmap.PixelFormat;
                if (pf == System.Drawing.Imaging.PixelFormat.Format32bppArgb || pf == System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                    || pf == System.Drawing.Imaging.PixelFormat.Format32bppRgb)
                {
                    var temp = bitmap;
                    bitmap = ImageLoader.CreateNewBitmap(bitmap, removeAlpha: true);
                    temp.Dispose();
                }
            }

            object System.Collections.IEnumerator.Current => Current as object;

            public void Dispose()
            {
                foreach (var stream in binaries) stream?.Dispose();
                preBitmap?.Dispose();
            }

            public bool MoveNext()
            {
                // 順次呼び出し以外も行う場合次のフレームが新規フレームなら描かないという処理を入れると高速化する
                if (!currentDrawn)
                {
                    var dummy = Current;
                }
                currentDrawn = false;
                return ++currentIndex < frameCount;
            }

            public void Reset()
            {
                currentDrawn = false;
                currentIndex = 0;
            }
        }

        private class IHDRChunk : Chunk
        {
            public IHDRChunk(byte[] chunkBytes)
                : base(chunkBytes)
            {
            }

            public IHDRChunk(Stream ms)
                : base(ms)
            {
            }

            public IHDRChunk(Chunk chunk)
                : base(chunk)
            {
            }

            public int Width { get; private set; }

            public int Height { get; private set; }

            public byte BitDepth { get; private set; }

            public byte ColorType { get; private set; }

            public byte CompressionMethod { get; private set; }

            public byte FilterMethod { get; private set; }

            public byte InterlaceMethod { get; private set; }

            protected override void ParseData(MemoryStream ms)
            {
                Width = Helper.ConvertEndian(ms.ReadInt32());
                Height = Helper.ConvertEndian(ms.ReadInt32());
                BitDepth = Convert.ToByte(ms.ReadByte());
                ColorType = Convert.ToByte(ms.ReadByte());
                CompressionMethod = Convert.ToByte(ms.ReadByte());
                FilterMethod = Convert.ToByte(ms.ReadByte());
                InterlaceMethod = Convert.ToByte(ms.ReadByte());
            }
        }

#pragma warning disable IDE1006 // 大文字開始の命名規則違反を無視。ライブラリの差し替えで名前変更のデメリットが大きいため。
        private class acTLChunk : Chunk
        {
            public acTLChunk(byte[] bytes)
                : base(bytes)
            {
            }

            public acTLChunk(Stream ms)
                : base(ms)
            {
            }

            public acTLChunk(Chunk chunk)
                : base(chunk)
            {
            }

            public uint NumFrames { get; private set; }

            public uint NumPlays { get; private set; }

            protected override void ParseData(MemoryStream ms)
            {
                NumFrames = Helper.ConvertEndian(ms.ReadUInt32());
                NumPlays = Helper.ConvertEndian(ms.ReadUInt32());
            }
        }

        private class fcTLChunk : Chunk
        {
            public fcTLChunk(byte[] bytes)
                : base(bytes)
            {
            }

            public fcTLChunk(Stream ms)
                : base(ms)
            {
            }

            public fcTLChunk(Chunk chunk)
                : base(chunk)
            {
            }

            /// <summary>
            ///     Sequence number of the animation chunk, starting from 0
            /// </summary>
            public uint SequenceNumber { get; private set; }

            /// <summary>
            ///     Width of the following frame
            /// </summary>
            public uint Width { get; private set; }

            /// <summary>
            ///     Height of the following frame
            /// </summary>
            public uint Height { get; private set; }

            /// <summary>
            ///     X position at which to render the following frame
            /// </summary>
            public uint XOffset { get; private set; }

            /// <summary>
            ///     Y position at which to render the following frame
            /// </summary>
            public uint YOffset { get; private set; }

            /// <summary>
            ///     Frame delay fraction numerator
            /// </summary>
            public ushort DelayNum { get; private set; }

            /// <summary>
            ///     Frame delay fraction denominator
            /// </summary>
            public ushort DelayDen { get; private set; }

            /// <summary>
            ///     Type of frame area disposal to be done after rendering this frame
            /// </summary>
            public DisposeOps DisposeOp { get; private set; }

            /// <summary>
            ///     Type of frame area rendering for this frame
            /// </summary>
            public BlendOps BlendOp { get; private set; }

            protected override void ParseData(MemoryStream ms)
            {
                SequenceNumber = Helper.ConvertEndian(ms.ReadUInt32());
                Width = Helper.ConvertEndian(ms.ReadUInt32());
                Height = Helper.ConvertEndian(ms.ReadUInt32());
                XOffset = Helper.ConvertEndian(ms.ReadUInt32());
                YOffset = Helper.ConvertEndian(ms.ReadUInt32());
                DelayNum = Helper.ConvertEndian(ms.ReadUInt16());
                DelayDen = Helper.ConvertEndian(ms.ReadUInt16());
                DisposeOp = (DisposeOps)ms.ReadByte();
                BlendOp = (BlendOps)ms.ReadByte();
            }
        }

        private class fdATChunk : Chunk
        {
            public fdATChunk(byte[] bytes)
                : base(bytes)
            {
            }

            public fdATChunk(MemoryStream ms)
                : base(ms)
            {
            }

            public fdATChunk(Chunk chunk)
                : base(chunk)
            {
            }

            public uint SequenceNumber { get; private set; }

            public byte[] FrameData { get; private set; }

            protected override void ParseData(MemoryStream ms)
            {
                SequenceNumber = Helper.ConvertEndian(ms.ReadUInt32());
                FrameData = ms.ReadBytes((int)Length - 4);
            }

            public IDATChunk ToIDATChunk()
            {
                uint newCrc;
                using (var msCrc = new MemoryStream())
                {
                    msCrc.WriteBytes(new[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' });
                    msCrc.WriteBytes(FrameData);

                    newCrc = CrcHelper.Calculate(msCrc.ToArray());
                }

                using (var ms = new MemoryStream())
                {
                    ms.WriteUInt32(Helper.ConvertEndian(Length - 4));
                    ms.WriteBytes(new[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' });
                    ms.WriteBytes(FrameData);
                    ms.WriteUInt32(Helper.ConvertEndian(newCrc));
                    ms.Position = 0;

                    return new IDATChunk(ms);
                }
            }
        }

        private class Chunk
        {
            internal Chunk()
            {
                Length = 0;
                ChunkType = String.Empty;
                ChunkData = null;
                Crc = 0;
            }

            internal Chunk(byte[] bytes)
            {
                var ms = new MemoryStream(bytes);
                Length = Helper.ConvertEndian(ms.ReadUInt32());
                ChunkType = Encoding.ASCII.GetString(ms.ReadBytes(4));
                ChunkData = ms.ReadBytes((int)Length);
                Crc = Helper.ConvertEndian(ms.ReadUInt32());

                if (ms.Position != ms.Length)
                    throw new Exception("Chunk length not correct.");
                if (Length != ChunkData.Length)
                    throw new Exception("Chunk data length not correct.");

                ParseData(new MemoryStream(ChunkData));
            }

            internal Chunk(Stream ms)
            {
                Length = Helper.ConvertEndian(ms.ReadUInt32());
                ChunkType = Encoding.ASCII.GetString(ms.ReadBytes(4));
                ChunkData = ms.ReadBytes((int)Length);
                Crc = Helper.ConvertEndian(ms.ReadUInt32());

                ParseData(new MemoryStream(ChunkData));
            }

            internal Chunk(Chunk chunk)
            {
                Length = chunk.Length;
                ChunkType = chunk.ChunkType;
                ChunkData = chunk.ChunkData;
                Crc = chunk.Crc;

                ParseData(new MemoryStream(ChunkData));
            }

            public uint Length { get; set; }

            public string ChunkType { get; set; }

            public byte[] ChunkData { get; set; }

            public uint Crc { get; set; }

            /// <summary>
            ///     Get raw data of the chunk
            /// </summary>
            public byte[] RawData
            {
                get
                {
                    var ms = new MemoryStream();
                    ms.WriteUInt32(Helper.ConvertEndian(Length));
                    ms.WriteBytes(Encoding.ASCII.GetBytes(ChunkType));
                    ms.WriteBytes(ChunkData);
                    ms.WriteUInt32(Helper.ConvertEndian(Crc));

                    return ms.ToArray();
                }
            }

            /// <summary>
            ///     Modify the ChunkData part.
            /// </summary>
            public void ModifyChunkData(int postion, byte[] newData)
            {
                Array.Copy(newData, 0, ChunkData, postion, newData.Length);

                using (var msCrc = new MemoryStream())
                {
                    msCrc.WriteBytes(Encoding.ASCII.GetBytes(ChunkType));
                    msCrc.WriteBytes(ChunkData);

                    Crc = CrcHelper.Calculate(msCrc.ToArray());
                }
            }

            /// <summary>
            ///     Modify the ChunkData part.
            /// </summary>
            public void ModifyChunkData(int postion, uint newData)
            {
                ModifyChunkData(postion, BitConverter.GetBytes(newData));
            }

            protected virtual void ParseData(MemoryStream ms)
            {
            }
        }

        /// <summary>
        ///     Describe a single frame.
        /// </summary>
        private class Frame
        {
            public static byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            private List<IDATChunk> idatChunks = new List<IDATChunk>();
            private List<OtherChunk> otherChunks = new List<OtherChunk>();

            /// <summary>
            ///     Gets or Sets the acTL chunk
            /// </summary>
            public IHDRChunk IHDRChunk { get; set; }

            /// <summary>
            ///     Gets or Sets the fcTL chunk
            /// </summary>
            public fcTLChunk fcTLChunk { get; set; }

            /// <summary>
            ///     Gets or Sets the IEND chunk
            /// </summary>
            public IENDChunk IENDChunk { get; set; }

            /// <summary>
            ///     Gets or Sets the other chunks
            /// </summary>
            public List<OtherChunk> OtherChunks
            {
                get { return otherChunks; }
                set { otherChunks = value; }
            }

            /// <summary>
            ///     Gets or Sets the IDAT chunks
            /// </summary>
            public List<IDATChunk> IDATChunks
            {
                get { return idatChunks; }
                set { idatChunks = value; }
            }

            /// <summary>
            ///     Add an Chunk to end end of existing list.
            /// </summary>
            public void AddOtherChunk(OtherChunk chunk)
            {
                otherChunks.Add(chunk);
            }

            /// <summary>
            ///     Add an IDAT Chunk to end end of existing list.
            /// </summary>
            public void AddIDATChunk(IDATChunk chunk)
            {
                idatChunks.Add(chunk);
            }

            /// <summary>
            ///     Gets the frame as PNG FileStream.
            /// </summary>
            public MemoryStream GetStream()
            {
                var ihdrChunk = new IHDRChunk(IHDRChunk);
                if (fcTLChunk != null)
                {
                    // Fix frame size with fcTL data.
                    ihdrChunk.ModifyChunkData(0, Helper.ConvertEndian(fcTLChunk.Width));
                    ihdrChunk.ModifyChunkData(4, Helper.ConvertEndian(fcTLChunk.Height));
                }

                // Write image data
                var ms = new MemoryStream();
                try
                {
                    ms.WriteBytes(Signature);
                    ms.WriteBytes(ihdrChunk.RawData);
                    otherChunks.ForEach(o => ms.WriteBytes(o.RawData));
                    idatChunks.ForEach(i => ms.WriteBytes(i.RawData));
                    ms.WriteBytes(IENDChunk.RawData);

                    ms.Position = 0;
                    return ms;
                }
                catch
                {
                    ms.Close();
                    ms.Dispose();
                    throw;
                }
            }
        }

        private class IDATChunk : Chunk
        {
            public IDATChunk(byte[] bytes)
                : base(bytes)
            {
            }

            public IDATChunk(Stream ms)
                : base(ms)
            {
            }

            public IDATChunk(Chunk chunk)
                : base(chunk)
            {
            }
        }

        private class IENDChunk : Chunk
        {
            public IENDChunk(byte[] bytes)
                : base(bytes)
            {
            }

            public IENDChunk(Stream ms)
                : base(ms)
            {
            }

            public IENDChunk(Chunk chunk)
                : base(chunk)
            {
            }
        }

        private class OtherChunk : Chunk
        {
            public OtherChunk(byte[] bytes)
                : base(bytes)
            {
            }

            public OtherChunk(Stream ms)
                : base(ms)
            {
            }

            public OtherChunk(Chunk chunk)
                : base(chunk)
            {
            }

            protected override void ParseData(MemoryStream ms)
            {
            }
        }
#pragma warning restore IDE1006
    }
}
