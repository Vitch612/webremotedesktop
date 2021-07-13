using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;



namespace SoundCapture
{
    public interface IWaveProvider
    {
        /// <summary>
        /// Gets the WaveFormat of this WaveProvider.
        /// </summary>
        /// <value>The wave format.</value>
        WaveFormat WaveFormat { get; }

        /// <summary>
        /// Fill the specified buffer with wave data.
        /// </summary>
        /// <param name="buffer">The buffer to fill of wave data.</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>the number of bytes written to the buffer.</returns>
        int Read(byte[] buffer, int offset, int count);
    }

    public class IgnoreDisposeStream : Stream
    {
        /// <summary>
        /// The source stream all other methods fall through to
        /// </summary>
        public Stream SourceStream { get; private set; }

        /// <summary>
        /// If true the Dispose will be ignored, if false, will pass through to the SourceStream
        /// Set to true by default
        /// </summary>
        public bool IgnoreDispose { get; set; }

        /// <summary>
        /// Creates a new IgnoreDisposeStream
        /// </summary>
        /// <param name="sourceStream">The source stream</param>
        public IgnoreDisposeStream(Stream sourceStream)
        {
            SourceStream = sourceStream;
            IgnoreDispose = true;
        }

        /// <summary>
        /// Can Read
        /// </summary>
        public override bool CanRead {get{return SourceStream.CanRead;}}

        /// <summary>
        /// Can Seek
        /// </summary>
        public override bool CanSeek {get{return SourceStream.CanSeek;}}

        /// <summary>
        /// Can write to the underlying stream
        /// </summary>
        public override bool CanWrite {get{return SourceStream.CanWrite;}}

        /// <summary>
        /// Flushes the underlying stream
        /// </summary>
        public override void Flush()
        {
            SourceStream.Flush();
        }

        /// <summary>
        /// Gets the length of the underlying stream
        /// </summary>
        public override long Length {get{return SourceStream.Length;}}

        /// <summary>
        /// Gets or sets the position of the underlying stream
        /// </summary>
        public override long Position
        {
            get
            {
                return SourceStream.Position;
            }
            set
            {
                SourceStream.Position = value;
            }
        }

        /// <summary>
        /// Reads from the underlying stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return SourceStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Seeks on the underlying stream
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return SourceStream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the underlying stream
        /// </summary>
        public override void SetLength(long value)
        {
            SourceStream.SetLength(value);
        }

        /// <summary>
        /// Writes to the underlying stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            SourceStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Dispose - by default (IgnoreDispose = true) will do nothing,
        /// leaving the underlying stream undisposed
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!IgnoreDispose)
            {
                SourceStream.Dispose();
                SourceStream = null;
            }
        }
    }

    public static class BufferHelpers
    {
        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bytesRequired"></param>
        /// <returns></returns>
        public static byte[] Ensure(byte[] buffer, int bytesRequired)
        {
            if (buffer == null || buffer.Length < bytesRequired)
            {
                buffer = new byte[bytesRequired];
            }
            return buffer;
        }

        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="samplesRequired"></param>
        /// <returns></returns>
        public static float[] Ensure(float[] buffer, int samplesRequired)
        {
            if (buffer == null || buffer.Length < samplesRequired)
            {
                buffer = new float[samplesRequired];
            }
            return buffer;
        }
    }

    public interface IWaveBuffer
    {
        /// <summary>
        /// Gets the byte buffer.
        /// </summary>
        /// <value>The byte buffer.</value>
        byte[] ByteBuffer { get; }

        /// <summary>
        /// Gets the float buffer.
        /// </summary>
        /// <value>The float buffer.</value>
        float[] FloatBuffer { get; }

        /// <summary>
        /// Gets the short buffer.
        /// </summary>
        /// <value>The short buffer.</value>
        short[] ShortBuffer { get; }

        /// <summary>
        /// Gets the int buffer.
        /// </summary>
        /// <value>The int buffer.</value>
        int[] IntBuffer { get; }

        /// <summary>
        /// Gets the max size in bytes of the byte buffer..
        /// </summary>
        /// <value>Maximum number of bytes in the buffer.</value>
        int MaxSize { get; }

        /// <summary>
        /// Gets the byte buffer count.
        /// </summary>
        /// <value>The byte buffer count.</value>
        int ByteBufferCount { get; }

        /// <summary>
        /// Gets the float buffer count.
        /// </summary>
        /// <value>The float buffer count.</value>
        int FloatBufferCount { get; }

        /// <summary>
        /// Gets the short buffer count.
        /// </summary>
        /// <value>The short buffer count.</value>
        int ShortBufferCount { get; }

        /// <summary>
        /// Gets the int buffer count.
        /// </summary>
        /// <value>The int buffer count.</value>
        int IntBufferCount { get; }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 2)]
    public class WaveBuffer : IWaveBuffer
    {
        /// <summary>
        /// Number of Bytes
        /// </summary>
        [FieldOffset(0)]
        public int numberOfBytes;
        [FieldOffset(8)]
        private byte[] byteBuffer;
        [FieldOffset(8)]
        private float[] floatBuffer;
        [FieldOffset(8)]
        private short[] shortBuffer;
        [FieldOffset(8)]
        private int[] intBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveBuffer"/> class.
        /// </summary>
        /// <param name="sizeToAllocateInBytes">The number of bytes. The size of the final buffer will be aligned on 4 Bytes (upper bound)</param>
        public WaveBuffer(int sizeToAllocateInBytes)
        {
            int aligned4Bytes = sizeToAllocateInBytes % 4;
            sizeToAllocateInBytes = (aligned4Bytes == 0) ? sizeToAllocateInBytes : sizeToAllocateInBytes + 4 - aligned4Bytes;
            // Allocating the byteBuffer is co-allocating the floatBuffer and the intBuffer
            byteBuffer = new byte[sizeToAllocateInBytes];
            numberOfBytes = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveBuffer"/> class binded to a specific byte buffer.
        /// </summary>
        /// <param name="bufferToBoundTo">A byte buffer to bound the WaveBuffer to.</param>
        public WaveBuffer(byte[] bufferToBoundTo)
        {
            BindTo(bufferToBoundTo);
        }

        /// <summary>
        /// Binds this WaveBuffer instance to a specific byte buffer.
        /// </summary>
        /// <param name="bufferToBoundTo">A byte buffer to bound the WaveBuffer to.</param>
        public void BindTo(byte[] bufferToBoundTo)
        {
            /* WaveBuffer assumes the caller knows what they are doing. We will let this pass
             * if ( (bufferToBoundTo.Length % 4) != 0 )
            {
                throw new ArgumentException("The byte buffer to bound must be 4 bytes aligned");
            }*/
            byteBuffer = bufferToBoundTo;
            numberOfBytes = 0;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="NAudio.Wave.WaveBuffer"/> to <see cref="System.Byte"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator byte[](WaveBuffer waveBuffer)
        {
            return waveBuffer.byteBuffer;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="NAudio.Wave.WaveBuffer"/> to <see cref="System.Single"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator float[](WaveBuffer waveBuffer)
        {
            return waveBuffer.floatBuffer;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="NAudio.Wave.WaveBuffer"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator int[](WaveBuffer waveBuffer)
        {
            return waveBuffer.intBuffer;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="NAudio.Wave.WaveBuffer"/> to <see cref="System.Int16"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator short[](WaveBuffer waveBuffer)
        {
            return waveBuffer.shortBuffer;
        }

        /// <summary>
        /// Gets the byte buffer.
        /// </summary>
        /// <value>The byte buffer.</value>
        public byte[] ByteBuffer
        {
            get { return byteBuffer; }
        }

        /// <summary>
        /// Gets the float buffer.
        /// </summary>
        /// <value>The float buffer.</value>
        public float[] FloatBuffer
        {
            get { return floatBuffer; }
        }

        /// <summary>
        /// Gets the short buffer.
        /// </summary>
        /// <value>The short buffer.</value>
        public short[] ShortBuffer
        {
            get { return shortBuffer; }
        }

        /// <summary>
        /// Gets the int buffer.
        /// </summary>
        /// <value>The int buffer.</value>
        public int[] IntBuffer
        {
            get { return intBuffer; }
        }


        /// <summary>
        /// Gets the max size in bytes of the byte buffer..
        /// </summary>
        /// <value>Maximum number of bytes in the buffer.</value>
        public int MaxSize
        {
            get { return byteBuffer.Length; }
        }

        /// <summary>
        /// Gets or sets the byte buffer count.
        /// </summary>
        /// <value>The byte buffer count.</value>
        public int ByteBufferCount
        {
            get { return numberOfBytes; }
            set
            {
                numberOfBytes = CheckValidityCount("ByteBufferCount", value, 1);
            }
        }
        /// <summary>
        /// Gets or sets the float buffer count.
        /// </summary>
        /// <value>The float buffer count.</value>
        public int FloatBufferCount
        {
            get { return numberOfBytes / 4; }
            set
            {
                numberOfBytes = CheckValidityCount("FloatBufferCount", value, 4);
            }
        }
        /// <summary>
        /// Gets or sets the short buffer count.
        /// </summary>
        /// <value>The short buffer count.</value>
        public int ShortBufferCount
        {
            get { return numberOfBytes / 2; }
            set
            {
                numberOfBytes = CheckValidityCount("ShortBufferCount", value, 2);
            }
        }
        /// <summary>
        /// Gets or sets the int buffer count.
        /// </summary>
        /// <value>The int buffer count.</value>
        public int IntBufferCount
        {
            get { return numberOfBytes / 4; }
            set
            {
                numberOfBytes = CheckValidityCount("IntBufferCount", value, 4);
            }
        }

        /// <summary>
        /// Clears the associated buffer.
        /// </summary>
        public void Clear()
        {
            Array.Clear(byteBuffer, 0, byteBuffer.Length);
        }

        /// <summary>
        /// Copy this WaveBuffer to a destination buffer up to ByteBufferCount bytes.
        /// </summary>
        public void Copy(Array destinationArray)
        {
            Array.Copy(byteBuffer, destinationArray, numberOfBytes);
        }

        /// <summary>
        /// Checks the validity of the count parameters.
        /// </summary>
        /// <param name="argName">Name of the arg.</param>
        /// <param name="value">The value.</param>
        /// <param name="sizeOfValue">The size of value.</param>
        private int CheckValidityCount(string argName, int value, int sizeOfValue)
        {
            int newNumberOfBytes = value * sizeOfValue;
            if ((newNumberOfBytes % 4) != 0)
            {
                throw new ArgumentOutOfRangeException(argName, String.Format("{0} cannot set a count ({1}) that is not 4 bytes aligned ", argName, newNumberOfBytes));
            }

            if (value < 0 || value > (byteBuffer.Length / sizeOfValue))
            {
                throw new ArgumentOutOfRangeException(argName, String.Format("{0} cannot set a count that exceed max count {1}", argName, byteBuffer.Length / sizeOfValue));
            }
            return newNumberOfBytes;
        }
    }

    public class SampleToWaveProvider16 : IWaveProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private float[] sourceBuffer;

        /// <summary>
        /// Converts from an ISampleProvider (IEEE float) to a 16 bit PCM IWaveProvider.
        /// Number of channels and sample rate remain unchanged.
        /// </summary>
        /// <param name="sourceProvider">The input source provider</param>
        public SampleToWaveProvider16(ISampleProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input source provider must be IEEE float", "sourceProvider");
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input source provider must be 32 bit", "sourceProvider");

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 16, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            volume = 1.0f;
        }

        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="destBuffer">The destination buffer</param>
        /// <param name="offset">Offset into the destination buffer</param>
        /// <param name="numBytes">Number of bytes read</param>
        /// <returns>Number of bytes read.</returns>
        public int Read(byte[] destBuffer, int offset, int numBytes)
        {
            int samplesRequired = numBytes / 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplesRequired);
            int sourceSamples = sourceProvider.Read(sourceBuffer, 0, samplesRequired);
            var destWaveBuffer = new WaveBuffer(destBuffer);

            int destOffset = offset / 2;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                float sample32 = sourceBuffer[sample] * volume;
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(sample32 * 32767);
            }

            return sourceSamples * 2;
        }

        /// <summary>
        /// <see cref="IWaveProvider.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat { get { return waveFormat; } }

        /// <summary>
        /// Volume of this channel. 1.0 = full scale
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }
    }

    public class WaveFileWriter : Stream
    {
        private Stream outStream;
        private readonly BinaryWriter writer;
        private long dataSizePos;
        private long factSampleCountPos;
        private long dataChunkSize;
        private readonly WaveFormat format;
        private readonly string filename;

        /// <summary>
        /// Creates a 16 bit Wave File from an ISampleProvider
        /// BEWARE: the source provider must not return data indefinitely
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="sourceProvider">The source sample provider</param>
        public static void CreateWaveFile16(string filename, ISampleProvider sourceProvider)
        {
            CreateWaveFile(filename, new SampleToWaveProvider16(sourceProvider));
        }

        /// <summary>
        /// Creates a Wave file by reading all the data from a WaveProvider
        /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <param name="sourceProvider">The source WaveProvider</param>
        public static void CreateWaveFile(string filename, IWaveProvider sourceProvider)
        {
            using (var writer = new WaveFileWriter(filename, sourceProvider.WaveFormat))
            {
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }
                    // Write will throw exception if WAV file becomes too large
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
        
        /// <summary>
        /// Writes to a stream by reading all the data from a WaveProvider
        /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="outStream">The stream the method will output to</param>
        /// <param name="sourceProvider">The source WaveProvider</param>
        public static void WriteWavFileToStream(Stream outStream, IWaveProvider sourceProvider)
        {
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(outStream), sourceProvider.WaveFormat)) 
            {
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while(true) 
                {
                    var bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) 
                    {
                        // end of source provider
                        outStream.Flush();
                        break;
                    }

                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
        
        /// <summary>
        /// WaveFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriter(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write((int)0); // placeholder
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);

            CreateFactChunk();
            WriteDataChunkHeader();
        }

        /// <summary>
        /// Creates a new WaveFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public WaveFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format)
        {
            this.filename = filename;
        }

        private void WriteDataChunkHeader()
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            dataSizePos = outStream.Position;
            writer.Write((int)0); // placeholder
        }

        private void CreateFactChunk()
        {
            if (HasFactChunk())
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fact"));
                writer.Write((int)4);
                factSampleCountPos = outStream.Position;
                writer.Write((int)0); // number of samples
            }
        }

        private bool HasFactChunk()
        {
            return format.Encoding != WaveFormatEncoding.Pcm && 
                format.BitsPerSample != 0;
        }

        /// <summary>
        /// The wave file name or null if not applicable
        /// </summary>
        public string Filename { get{return filename;}}

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length { get{return dataChunkSize;}}

        /// <summary>
        /// Total time (calculated from Length and average bytes per second)
        /// </summary>
        public TimeSpan TotalTime {get{return TimeSpan.FromSeconds((double)Length / WaveFormat.AverageBytesPerSecond);}}

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat {get{return format;}}

        /// <summary>
        /// Returns false: Cannot read from a WaveFileWriter
        /// </summary>
        public override bool CanRead {get{return false;}}

        /// <summary>
        /// Returns true: Can write to a WaveFileWriter
        /// </summary>
        public override bool CanWrite {get{return true;}}

        /// <summary>
        /// Returns false: Cannot seek within a WaveFileWriter
        /// </summary>
        public override bool CanSeek {get {return false;}}

        /// <summary>
        /// Read is not supported for a WaveFileWriter
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriter");
        }

        /// <summary>
        /// Seek is not supported for a WaveFileWriter
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriter");
        }

        /// <summary>
        /// SetLength is not supported for WaveFileWriter
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriter");
        }

        /// <summary>
        /// Gets the Position in the WaveFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get {return dataChunkSize;}
            set { throw new InvalidOperationException("Repositioning a WaveFileWriter is not supported"); }
        }

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        [Obsolete("Use Write instead")]
        public void WriteData(byte[] data, int offset, int count)
        {
            Write(data, offset, count);
        }

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] data, int offset, int count)
        {
            if (outStream.Length + count > UInt32.MaxValue)
                throw new ArgumentException("WAV file too large", "count");
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        private readonly byte[] value24 = new byte[3]; // keep this around to save us creating it every time
        
        /// <summary>
        /// Writes a single sample to the Wave file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write((Int16)(Int16.MaxValue * sample));
                dataChunkSize += 2;
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
                value24[0] = value[1];
                value24[1] = value[2];
                value24[2] = value[3];
                writer.Write(value24);
                dataChunkSize += 3;
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                writer.Write(UInt16.MaxValue * (Int32)sample);
                dataChunkSize += 4;
            }
            else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                writer.Write(sample);
                dataChunkSize += 4;
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes 32 bit floating point samples to the Wave file
        /// They will be converted to the appropriate bit depth depending on the WaveFormat of the WAV file
        /// </summary>
        /// <param name="samples">The buffer containing the floating point samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of floating point samples to write</param>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        [Obsolete("Use WriteSamples instead")]
        public void WriteData(short[] samples, int offset, int count)
        {
            WriteSamples(samples, offset, count);
        }


        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteSamples(short[] samples, int offset, int count)
        {
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(samples[sample + offset]);
                }
                dataChunkSize += (count * 2);
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    var value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)samples[sample + offset]);
                    value24[0] = value[1];
                    value24[1] = value[2];
                    value24[2] = value[3];
                    writer.Write(value24);
                }
                dataChunkSize += (count * 3);
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(UInt16.MaxValue * (Int32)samples[sample + offset]);
                }
                dataChunkSize += (count * 4);
            }
            // IEEE float data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write((float)samples[sample + offset] / (float)(Int16.MaxValue + 1));
                }
                dataChunkSize += (count * 4);
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Ensures data is written to disk
        /// Also updates header, so that WAV file will be valid up to the point currently written
        /// </summary>
        public override void Flush()
        {
            var pos = writer.BaseStream.Position;
            UpdateHeader(writer);
            writer.BaseStream.Position = pos;
        }

        #region IDisposable Members

        /// <summary>
        /// Actually performs the close,making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (outStream != null)
                {
                    try
                    {
                        UpdateHeader(writer);
                    }
                    finally
                    {
                        // in a finally block as we don't want the FileStream to run its disposer in
                        // the GC thread if the code above caused an IOException (e.g. due to disk full)
                        outStream.Close(); // will close the underlying base stream
                        outStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the header with file size information
        /// </summary>
        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            writer.Flush();
            UpdateRiffChunk(writer);
            UpdateFactChunk(writer);
            UpdateDataChunk(writer);
        }

        private void UpdateDataChunk(BinaryWriter writer)
        {
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write((UInt32)dataChunkSize);
        }

        private void UpdateRiffChunk(BinaryWriter writer)
        {
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((UInt32)(outStream.Length - 8));
        }

        private void UpdateFactChunk(BinaryWriter writer)
        {
            if (HasFactChunk())
            {
                int bitsPerSample = (format.BitsPerSample * format.Channels);
                if (bitsPerSample != 0)
                {
                    writer.Seek((int)factSampleCountPos, SeekOrigin.Begin);
                    
                    writer.Write((int)((dataChunkSize * 8) / bitsPerSample));
                }
            }
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this WaveFileWriter
        /// </summary>
        ~WaveFileWriter()
        {
            System.Diagnostics.Debug.Assert(false, "WaveFileWriter was not disposed");
            Dispose(false);
        }

        #endregion
    }

}
