using System;
using System.IO;

namespace SoundCapture
{
    /// <summary>
    /// Convert PCM audio data to PCM format
    /// The data received through the method write is assumed as PCM audio data. 
    /// This data is converted to MP3 format and written to the result stream. 
    /// <seealso cref="AudioWriter"/>
    /// <seealso cref="yeti.mp3"/>
    /// </summary>
    public class Mp3Writer:BinaryWriter
    {
        private WaveFormat m_InputDataFormat;
        private bool closed = false;
        private BE_CONFIG m_Mp3Config = null;
        private uint m_hLameStream = 0;
        private uint m_InputSamples = 0;
        private uint m_OutBufferSize = 0;
        private byte[] m_InBuffer = null;
        private int m_InBufferPos = 0;
        private byte[] m_OutBuffer = null;

        /// <summary>
        /// Create a Mp3Writer with the default MP3 format
        /// </summary>
        /// <param name="output">Stream that will hold the MP3 resulting data</param>
        /// <param name="inputDataFormat">PCM format of input data</param>
        public Mp3Writer(Stream output, WaveFormat inputDataFormat)
            : this(output, inputDataFormat, new BE_CONFIG(inputDataFormat))
        {
        }


        

        /// <summary>
        /// Create a Mp3Writer with specific MP3 format
        /// </summary>
        /// <param name="output">Stream that will hold the MP3 resulting data</param>
        /// <param name="inputDataFormat">PCM format of input data</param>
        /// <param name="mp3Config">Desired MP3 config</param>
        public Mp3Writer(Stream output, WaveFormat inputDataFormat, BE_CONFIG mp3Config)
            : base(output, System.Text.Encoding.ASCII)
        {
            try
            {
                m_InputDataFormat = inputDataFormat;
                m_Mp3Config = mp3Config;
                uint lameResult = LameEnc.beInitStream(m_Mp3Config, ref m_InputSamples, ref m_OutBufferSize, ref m_hLameStream);
                if (lameResult != LameEnc.BE_ERR_SUCCESSFUL)
                {
                    throw new ApplicationException(string.Format("Lame_encDll.beInitStream failed with the error code {0}", lameResult));
                }
                m_InBuffer = new byte[m_InputSamples * 2]; //Input buffer is expected as short[]
                m_OutBuffer = new byte[m_OutBufferSize];
            }
            catch
            {
                base.Close();
                throw;
            }
        }

        /// <summary>
        /// MP3 Config of final data
        /// </summary>
        public BE_CONFIG Mp3Config
        {
            get
            {
                return m_Mp3Config;
            }
        }

        protected int GetOptimalBufferSize()
        {
            return m_InBuffer.Length;
        }

        protected override void Dispose(bool disposing)
        {
            Close();
        }

        public override void Close()
        {
            if (!closed)
            {
                try
                {
                    uint encodedSize = 0;
                    if (m_InBufferPos > 0)
                    {
                        if (LameEnc.EncodeChunk(m_hLameStream, m_InBuffer, 0, (uint)m_InBufferPos, m_OutBuffer, ref encodedSize) == LameEnc.BE_ERR_SUCCESSFUL)
                        {
                            if (encodedSize > 0)
                            {
                                base.Write(m_OutBuffer, 0, (int)encodedSize);
                            }
                        }
                    }
                    encodedSize = 0;
                    if (LameEnc.beDeinitStream(m_hLameStream, m_OutBuffer, ref encodedSize) == LameEnc.BE_ERR_SUCCESSFUL)
                    {
                        if (encodedSize > 0)
                        {
                            base.Write(m_OutBuffer, 0, (int)encodedSize);
                        }
                    }
                }
                finally
                {
                    LameEnc.beCloseStream(m_hLameStream);
                }
            }
            closed = true;
            //Purposefully leaving the stream open since it gets passed in, 
            //its the responsibility of the calling class to manage its own resources.
            base.Flush();
        }


        /// <summary>
        /// Send to the compressor an array of bytes.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <param name="index">Start position</param>
        /// <param name="count">Bytes to process. The optimal size, to avoid buffer copy, is a multiple of <see cref="AudioWriter.OptimalBufferSize"/></param>
        public override void Write(byte[] buffer, int index, int count)
        {
            uint encodedSize = 0;
            while (count > 0)
            {
                uint lameResult;
                if (m_InBufferPos > 0)
                {
                    int toCopy = Math.Min(count, m_InBuffer.Length - m_InBufferPos);
                    Buffer.BlockCopy(buffer, index, m_InBuffer, m_InBufferPos, toCopy);
                    m_InBufferPos += toCopy;
                    index += toCopy;
                    count -= toCopy;
                    if (m_InBufferPos >= m_InBuffer.Length)
                    {
                        m_InBufferPos = 0;
                        if ((lameResult = LameEnc.EncodeChunk(m_hLameStream, m_InBuffer, m_OutBuffer, ref encodedSize)) == LameEnc.BE_ERR_SUCCESSFUL)
                        {
                            if (encodedSize > 0)
                            {
                                base.Write(m_OutBuffer, 0, (int)encodedSize);
                            }
                        }
                        else
                        {
                            throw new ApplicationException(string.Format("Lame_encDll.EncodeChunk failed with the error code {0}", lameResult));
                        }
                    }
                }
                else
                {
                    if (count >= m_InBuffer.Length)
                    {
                        if ((lameResult = LameEnc.EncodeChunk(m_hLameStream, buffer, index, (uint)m_InBuffer.Length, m_OutBuffer, ref encodedSize)) == LameEnc.BE_ERR_SUCCESSFUL)
                        {
                            if (encodedSize > 0)
                            {
                                base.Write(m_OutBuffer, 0, (int)encodedSize);
                            }
                        }
                        else
                        {
                            throw new ApplicationException(string.Format("Lame_encDll.EncodeChunk failed with the error code {0}", lameResult));
                        }
                        count -= m_InBuffer.Length;
                        index += m_InBuffer.Length;
                    }
                    else
                    {
                        Buffer.BlockCopy(buffer, index, m_InBuffer, 0, count);
                        m_InBufferPos = count;
                        index += count;
                        count = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Send to the compressor an array of bytes.
        /// </summary>
        /// <param name="buffer">The optimal size, to avoid buffer copy, is a multiple of <see cref="AudioWriter.OptimalBufferSize"/></param>
        public override void Write(byte[] buffer)
        {
            this.Write(buffer, 0, buffer.Length);
        }
    }
}