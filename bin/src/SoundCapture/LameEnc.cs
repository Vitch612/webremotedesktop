using System;
using System.Runtime.InteropServices;

namespace SoundCapture
{
    /// <summary>
    /// Lame_enc DLL functions
    /// </summary>
    public class LameEnc
    {
        static bool is32bit = IntPtr.Size == 4;
        //Error codes
        public const uint BE_ERR_SUCCESSFUL = 0;
        public const uint BE_ERR_INVALID_FORMAT = 1;
        public const uint BE_ERR_INVALID_FORMAT_PARAMETERS = 2;
        public const uint BE_ERR_NO_MORE_HANDLES = 3;
        public const uint BE_ERR_INVALID_HANDLE = 4;

        /// <summary>
        /// This function is the first to call before starting an encoding stream.
        /// </summary>
        /// <param name="pbeConfig">Encoder settings</param>
        /// <param name="dwSamples">Receives the number of samples (not bytes, each sample is a SHORT) to send to each beEncodeChunk() on return.</param>
        /// <param name="dwBufferSize">Receives the minimun number of bytes that must have the output(result) buffer</param>
        /// <param name="phbeStream">Receives the stream handle on return</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        public static uint beInitStream(BE_CONFIG pbeConfig, ref uint dwSamples, ref uint dwBufferSize, ref uint phbeStream)
        {
            return is32bit 
                ? Lame86.beInitStream(pbeConfig, ref dwSamples, ref dwBufferSize, ref phbeStream) 
                : Lame64.beInitStream(pbeConfig, ref dwSamples, ref dwBufferSize, ref phbeStream);
        }

        /// <summary>
        /// Encodes a chunk of samples. Please note that if you have set the output to 
        /// generate mono MP3 files you must feed beEncodeChunk() with mono samples
        /// </summary>
        /// <param name="hbeStream">Handle of the stream.</param>
        /// <param name="nSamples">Number of samples to be encoded for this call. 
        /// This should be identical to what is returned by beInitStream(), 
        /// unless you are encoding the last chunk, which might be smaller.</param>
        /// <param name="pInSamples">Array of 16-bit signed samples to be encoded. 
        /// These should be in stereo when encoding a stereo MP3 
        /// and mono when encoding a mono MP3</param>
        /// <param name="pOutput">Buffer where to write the encoded data. 
        /// This buffer should be at least of the minimum size returned by beInitStream().</param>
        /// <param name="pdwOutput">Returns the number of bytes of encoded data written. 
        /// The amount of data written might vary from chunk to chunk</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        public static uint beEncodeChunk(uint hbeStream, uint nSamples, short[] pInSamples, [In, Out] byte[] pOutput, ref uint pdwOutput)
        {
            return is32bit
                ? Lame86.beEncodeChunk(hbeStream, nSamples, pInSamples, pOutput, ref pdwOutput)
                : Lame64.beEncodeChunk(hbeStream, nSamples, pInSamples, pOutput, ref pdwOutput);
        }

        /// <summary>
        /// Encodes a chunk of samples. Please note that if you have set the output to 
        /// generate mono MP3 files you must feed beEncodeChunk() with mono samples
        /// </summary>
        /// <param name="hbeStream">Handle of the stream.</param>
        /// <param name="nSamples">Number of samples to be encoded for this call. 
        /// This should be identical to what is returned by beInitStream(), 
        /// unless you are encoding the last chunk, which might be smaller.</param>
        /// <param name="pSamples">Pointer at the 16-bit signed samples to be encoded. 
        /// InPtr is used to pass any type of array without need of make memory copy, 
        /// then gaining in performance. Note that nSamples is not the number of bytes,
        /// but samples (is sample is a SHORT)</param>
        /// <param name="pOutput">Buffer where to write the encoded data. 
        /// This buffer should be at least of the minimum size returned by beInitStream().</param>
        /// <param name="pdwOutput">Returns the number of bytes of encoded data written. 
        /// The amount of data written might vary from chunk to chunk</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        protected static uint beEncodeChunk(uint hbeStream, uint nSamples, IntPtr pSamples, [In, Out] byte[] pOutput, ref uint pdwOutput)
        {
            return is32bit
                ? Lame86.beEncodeChunk(hbeStream, nSamples, pSamples, pOutput, ref pdwOutput)
                : Lame64.beEncodeChunk(hbeStream, nSamples, pSamples, pOutput, ref pdwOutput);
        }

        /// <summary>
        /// Encodes a chunk of samples. Samples are contained in a byte array
        /// </summary>
        /// <param name="hbeStream">Handle of the stream.</param>
        /// <param name="buffer">Bytes to encode</param>
        /// <param name="index">Position of the first byte to encode</param>
        /// <param name="nBytes">Number of bytes to encode (not samples, samples are two byte lenght)</param>
        /// <param name="pOutput">Buffer where to write the encoded data.
        /// This buffer should be at least of the minimum size returned by beInitStream().</param>
        /// <param name="pdwOutput">Returns the number of bytes of encoded data written. 
        /// The amount of data written might vary from chunk to chunk</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        public static uint EncodeChunk(uint hbeStream, byte[] buffer, int index, uint nBytes, byte[] pOutput, ref uint pdwOutput)
        {
            uint res;
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr;
                if (is32bit)
                {
                    ptr = (IntPtr)(handle.AddrOfPinnedObject().ToInt32() + index);
                }
                else
                {
                    ptr = (IntPtr)(handle.AddrOfPinnedObject().ToInt64() + index);
                }
                res = beEncodeChunk(hbeStream, nBytes / 2/*Samples*/, ptr, pOutput, ref pdwOutput);
            }
            finally
            {
                handle.Free();
            }
            return res;
        }
        /// <summary>
        /// Encodes a chunk of samples. Samples are contained in a byte array
        /// </summary>
        /// <param name="hbeStream">Handle of the stream.</param>
        /// <param name="buffer">Bytes to encode</param>
        /// <param name="pOutput">Buffer where to write the encoded data.
        /// This buffer should be at least of the minimum size returned by beInitStream().</param>
        /// <param name="pdwOutput">Returns the number of bytes of encoded data written. 
        /// The amount of data written might vary from chunk to chunk</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        public static uint EncodeChunk(uint hbeStream, byte[] buffer, byte[] pOutput, ref uint pdwOutput)
        {
            return EncodeChunk(hbeStream, buffer, 0, (uint)buffer.Length, pOutput, ref pdwOutput);
        }
        /// <summary>
        /// This function should be called after encoding the last chunk in order to flush 
        /// the encoder. It writes any encoded data that still might be left inside the 
        /// encoder to the output buffer. This function should NOT be called unless 
        /// you have encoded all of the chunks in your stream.
        /// </summary>
        /// <param name="hbeStream">Handle of the stream.</param>
        /// <param name="pOutput">Where to write the encoded data. This buffer should be 
        /// at least of the minimum size returned by beInitStream().</param>
        /// <param name="pdwOutput">Returns number of bytes of encoded data written.</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        public static uint beDeinitStream(uint hbeStream, [In, Out] byte[] pOutput, ref uint pdwOutput)
        {
            return is32bit
                ? Lame86.beDeinitStream(hbeStream, pOutput, ref pdwOutput)
                : Lame64.beDeinitStream(hbeStream, pOutput, ref pdwOutput);
        }

        /// <summary>
        /// Last function to be called when finished encoding a stream. 
        /// Should unlike beDeinitStream() also be called if the encoding is canceled.
        /// </summary>
        /// <param name="hbeStream">Handle of the stream.</param>
        /// <returns>On success: BE_ERR_SUCCESSFUL</returns>
        public static uint beCloseStream(uint hbeStream)
        {
            return is32bit
                ? Lame86.beCloseStream(hbeStream)
                : Lame64.beCloseStream(hbeStream);
        }

        /// <summary>
        /// Returns information like version numbers (both of the DLL and encoding engine), 
        /// release date and URL for lame_enc's homepage. 
        /// All this information should be made available to the user of your product 
        /// through a dialog box or something similar.
        /// </summary>
        /// <param name="pbeVersion"Where version number, release date and URL for homepage 
        /// is returned.</param>
        public static void beVersion([Out] BE_VERSION pbeVersion)
        {
            if (is32bit)
            {
                Lame86.beVersion(pbeVersion);
            }
            else
            {
                Lame64.beVersion(pbeVersion);
            }
        }

        public static void beWriteVBRHeader(string pszMP3FileName)
        {
            if (is32bit)
            {
                Lame86.beWriteVBRHeader(pszMP3FileName);
            }
            else
            {
                Lame64.beWriteVBRHeader(pszMP3FileName);
            }
        }

        public static uint beEncodeChunkFloatS16NI(uint hbeStream, uint nSamples, [In]float[] buffer_l, [In]float[] buffer_r, [In, Out]byte[] pOutput, ref uint pdwOutput)
        {
            return is32bit
                ? Lame86.beEncodeChunkFloatS16NI(hbeStream, nSamples, buffer_l, buffer_r, pOutput, ref pdwOutput)
                : Lame64.beEncodeChunkFloatS16NI(hbeStream, nSamples, buffer_l, buffer_r, pOutput, ref pdwOutput);
        }

        public static uint beFlushNoGap(uint hbeStream, [In, Out]byte[] pOutput, ref uint pdwOutput)
        {
            return is32bit
                ? Lame86.beFlushNoGap(hbeStream, pOutput, ref pdwOutput)
                : Lame64.beFlushNoGap(hbeStream, pOutput, ref pdwOutput);
        }

        public static uint beWriteInfoTag(uint hbeStream, string lpszFileName)
        {
            return is32bit
                ? Lame86.beWriteInfoTag(hbeStream, lpszFileName)
                : Lame64.beWriteInfoTag(hbeStream, lpszFileName);
        }
    }
}