using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace SoundCapture
{    
    public class CoreAudio
    {
        private readonly Guid GuidEventContext = Guid.NewGuid();

        const int DEVICE_STATE_ACTIVE = 0x00000001;
        const int DEVICE_STATE_DISABLE = 0x00000002;
        const int DEVICE_STATE_NOTPRESENT = 0x00000004;
        const int DEVICE_STATE_UNPLUGGED = 0x00000008;
        const int DEVICE_STATEMASK_ALL = 0x0000000f;

        [DllImport("ole32.Dll")]
        static public extern uint CoCreateInstance(
            ref Guid clsid,
            [MarshalAs(UnmanagedType.IUnknown)] object inner,
            uint context,
            ref Guid uuid,
            [MarshalAs(UnmanagedType.IUnknown)] out object rReturnedComObject);

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(DelegateMixerChange pNotify);
            int UnregisterControlChangeNotify(DelegateMixerChange pNotify);
            int GetChannelCount(ref uint pnChannelCount);
            int SetMasterVolumeLevel(float fLevelDB, Guid pguidEventContext);
            int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
            int GetMasterVolumeLevel(ref float pfLevelDB);
            int GetMasterVolumeLevelScalar(ref float pfLevel);
            int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid pguidEventContext);
            int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid pguidEventContext);
            int GetChannelVolumeLevel(uint nChannel, ref float pfLevelDB);
            int GetChannelVolumeLevelScalar(uint nChannel, ref float pfLevel);
            int SetMute(int bMute, Guid pguidEventContext);
            int GetMute(ref bool pbMute);
            int GetVolumeStepInfo(ref uint pnStep, ref uint pnStepCount);
            int VolumeStepUp(Guid pguidEventContext);
            int VolumeStepDown(Guid pguidEventContext);
            int QueryHardwareSupport(ref uint pdwHardwareSupportMask);
            int GetVolumeRange(ref float pflVolumeMindB, ref float pflVolumeMaxdB, ref float pflVolumeIncrementdB);
        }

        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMMDeviceCollection
        {
            int GetCount(ref uint pcDevices);
            int Item(uint nDevice, ref IntPtr ppDevice);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMMDevice
        {
            [PreserveSig]
            int Activate(
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 dwClsCtx,
                [In] [MarshalAs(UnmanagedType.SysInt)] IntPtr pActivationParams,
                [Out] [MarshalAs(UnmanagedType.SysInt)] out IntPtr ppInterface);
            int OpenPropertyStore(int stgmAccess, ref IntPtr ppProperties);
            int GetId(ref string ppstrId);
            int GetState(ref int pdwState);
        }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, ref IntPtr ppDevices);
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, ref IntPtr ppEndpoint);
            int GetDevice(string pwstrId, ref IntPtr ppDevice);
            int RegisterEndpointNotificationCallback(IntPtr pClient);
            int UnregisterEndpointNotificationCallback(IntPtr pClient);
        }

        public enum AUDCLNT_SHAREMODE:uint
        {
            AUDCLNT_SHAREMODE_SHARED = 0,
            AUDCLNT_SHAREMODE_EXCLUSIVE = 1
        }

        [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioClient
        {
            [PreserveSig]
            int Initialize(
                [In] [MarshalAs(UnmanagedType.I4)] AUDCLNT_SHAREMODE shareMode,
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 streamFlags,
                [In] [MarshalAs(UnmanagedType.U8)] UInt64 bufferDuration,
                [In] [MarshalAs(UnmanagedType.U8)] UInt64 devicePeriod,
                [In] [MarshalAs(UnmanagedType.SysInt)] IntPtr format,
                [In, Optional] [MarshalAs(UnmanagedType.LPStruct)] Guid audioSessionId);
            [PreserveSig]
            int GetBufferSize(
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 size);
            [PreserveSig]
            int GetStreamLatency(
                [Out] [MarshalAs(UnmanagedType.U8)] out UInt64 latency);
            [PreserveSig]
            int GetCurrentPadding(
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 frameCount);
            [PreserveSig]
            int IsFormatSupported(
                [In] [MarshalAs(UnmanagedType.I4)] AUDCLNT_SHAREMODE shareMode,
                [In] [MarshalAs(UnmanagedType.SysInt)] IntPtr format,
                [Out, Optional] out IntPtr closestMatch);
            [PreserveSig]
            int GetMixFormat(
                [Out] [MarshalAs(UnmanagedType.SysInt)] out IntPtr format);
            [PreserveSig]
            int GetDevicePeriod(
                [Out, Optional] [MarshalAs(UnmanagedType.U8)] out UInt64 processInterval,
                [Out, Optional] [MarshalAs(UnmanagedType.U8)] out UInt64 minimumInterval);
            [PreserveSig]
            int Start();
            [PreserveSig]
            int Stop();
            [PreserveSig]
            int Reset();
            [PreserveSig]
            int SetEventHandle(
                [In] [MarshalAs(UnmanagedType.SysInt)] IntPtr handle);
            [PreserveSig]
            int GetService(
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [Out] [MarshalAs(UnmanagedType.SysInt)] out IntPtr ppv);
        }

        [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioCaptureClient
        {
            [PreserveSig]
            int GetBuffer(
                [Out] [MarshalAs(UnmanagedType.SysInt)] out IntPtr ppData,
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 pNumFramesToRead,
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 pdwFlags,
                [Out, Optional] [MarshalAs(UnmanagedType.U8)] out UInt64 pu64DevicePosition,
                [Out, Optional] [MarshalAs(UnmanagedType.U8)] out UInt64 pu64QPCPosition);
            [PreserveSig]
            int ReleaseBuffer(
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 numFramesRead);
            [PreserveSig]
            int GetNextPacketSize(
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 frameCount);
        }

        [Flags]
        enum CLSCTX : uint
        {
            CLSCTX_INPROC_SERVER = 0x1,
            CLSCTX_INPROC_HANDLER = 0x2,
            CLSCTX_LOCAL_SERVER = 0x4,
            CLSCTX_INPROC_SERVER16 = 0x8,
            CLSCTX_REMOTE_SERVER = 0x10,
            CLSCTX_INPROC_HANDLER16 = 0x20,
            CLSCTX_RESERVED1 = 0x40,
            CLSCTX_RESERVED2 = 0x80,
            CLSCTX_RESERVED3 = 0x100,
            CLSCTX_RESERVED4 = 0x200,
            CLSCTX_NO_CODE_DOWNLOAD = 0x400,
            CLSCTX_RESERVED5 = 0x800,
            CLSCTX_NO_CUSTOM_MARSHAL = 0x1000,
            CLSCTX_ENABLE_CODE_DOWNLOAD = 0x2000,
            CLSCTX_NO_FAILURE_LOG = 0x4000,
            CLSCTX_DISABLE_AAA = 0x8000,
            CLSCTX_ENABLE_AAA = 0x10000,
            CLSCTX_FROM_DEFAULT_CONTEXT = 0x20000,
            CLSCTX_INPROC = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER,
            CLSCTX_SERVER = CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER,
            CLSCTX_ALL = CLSCTX_SERVER | CLSCTX_INPROC_HANDLER
        }

        [Flags]
        public enum AUDCLNT_BUFFERFLAGS
        {
            AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY = 0x1,
            AUDCLNT_BUFFERFLAGS_SILENT = 0x2,
            AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR = 0x4
        }

        public enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
            EDataFlow_enum_count
        }

        public enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications,
            ERole_enum_count
        }

        public const UInt32 AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public UInt16 wFormatTag;
            public UInt16 nChannels;
            public UInt32 nSamplesPerSec;
            public UInt32 nAvgBytesPerSec;
            public UInt16 nBlockAlign;
            public UInt16 wBitsPerSample;
            public UInt16 cbSize;
        }

        object oEnumerator = null;
        IMMDeviceEnumerator iMde = null;
        IMMDevice imd = null;
        IAudioClient iAudioClient = null;
        IAudioCaptureClient iAudioCaptureClient = null;
        IAudioEndpointVolume iAudioEndpoint = null;
        System.Threading.Thread recordingthread;
        public Boolean keepgoing;
        public delegate void DelegateMixerChange();
        public delegate void MixerChangedEventHandler();
        double hnsActualDuration;
        public WAVEFORMATEX waveformat;                
        public delegate void notifydatareceived();
        public notifydatareceived notify;
        private Object synclock = new Object();
        private MemoryStream memStream = new MemoryStream();

        private int bytesread = 0;

        public static void logentry(string msg) {
          string appName = "DesktopInteractServer";
          EventSourceCreationData eventData = new EventSourceCreationData(appName, "Application");
          if (!EventLog.SourceExists(appName))
             EventLog.CreateEventSource(eventData);
          EventLog eLog = new EventLog();
          eLog.Source = appName;
          eLog.WriteEntry(msg, EventLogEntryType.Information);
        }

        public byte[] readbytes()
        {
            if (keepgoing)
            {
                lock (synclock)
                {
                    if (memStream.Length > bytesread)
                    {
                        int oldpos = (int) memStream.Position;
                        int toread = (int) memStream.Length - bytesread;
                        memStream.Position = bytesread;
                        byte[] buffer = new byte[toread];
                        memStream.Read(buffer, 0, toread);
                        memStream.Position = oldpos;
                        bytesread += toread;
                        return buffer;
                    }
                }
            }
            return new byte[0];
        }

        public void recordingloop()
        {
            uint packetLength;
            uint numFramesAvailable;
            uint flags;
            ulong junk1;
            ulong junk2;
            byte[] pData;
            uint AUDCLNT_BUFFERFLAGS_SILENT = 2;
            uint bytespersample=(uint)(waveformat.nChannels*waveformat.wBitsPerSample/8);
            WaveFormat _wf = new WaveFormat((int)waveformat.nSamplesPerSec, (int)waveformat.wBitsPerSample, (int)waveformat.nChannels);
            BE_CONFIG _mp3config = new BE_CONFIG(_wf);
            Mp3Writer _mp3writer = new Mp3Writer(memStream, _wf, _mp3config);           

            int retVal = iAudioClient.Start();
            if (retVal != 0)
            {
                throw new Exception("IAudioClient.Start()");
            }
            keepgoing = true;
            while (keepgoing)
            {
                //if (packetLength == 0)
                //{
                //    double sleeptime = 2*hnsActualDuration / 10000000;
                //    int samplesize = (int) (waveformat.wBitsPerSample * waveformat.nChannels/8);
                //    double space = sleeptime*waveformat.nSamplesPerSec*samplesize;
                //    int roundedspace = ((int)(space / samplesize)) * samplesize;
                //    pData = new byte[roundedspace];
                //    for (int i = 0; i < pData.Length; i++)
                //        pData[i] = 0;
                //    wfw.Write(pData, 0, pData.Length);
                //}

                Thread.Sleep((int)(hnsActualDuration / 5000));
                retVal=iAudioCaptureClient.GetNextPacketSize(out packetLength);
                if (retVal!=0) {
                    throw new Exception("iAudioCaptureClient.GetNextPacketSize()");
                }
                while (packetLength != 0)
                {
                    IntPtr dataPtr = IntPtr.Zero;
                    retVal = iAudioCaptureClient.GetBuffer(out dataPtr, out numFramesAvailable, out flags, out junk1, out junk2);
                    if (retVal != 0)
                    {
                        throw new Exception("iAudioCaptureClient.GetBuffer()");
                    }
                    pData = new byte[bytespersample * numFramesAvailable];
                    if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) != 0)
                    {
                        for (int i = 0; i < pData.Length; i++)
                            pData[i] = 0;
                    }
                    else
                    {
                        Marshal.Copy(dataPtr, pData, 0, (int)(bytespersample * numFramesAvailable));
                    }
                    retVal = iAudioCaptureClient.ReleaseBuffer(numFramesAvailable);
                    if (retVal!=0) {
                        throw new Exception("iAudioCaptureClient.ReleaseBuffer()");
                    }

                    lock (synclock)
                    {                        
                        long datawritten = memStream.Length;
                        _mp3writer.Write(pData, 0, pData.Length);
                        datawritten = memStream.Length - datawritten;
                    }
                    if (notify != null) notify();

                    retVal = iAudioCaptureClient.GetNextPacketSize(out packetLength);
                    if (retVal != 0)
                    {
                        throw new Exception("iAudioCaptureClient.GetNextPacketSize()");
                    }
                }
            }
            _mp3writer.Close();
            memStream.Close();
            memStream.Dispose();
            if (iAudioClient != null)
            {
                retVal = iAudioClient.Stop();
                if (retVal != 0)
                {
                    throw new Exception("iAudioClient.Stop()");
                }
            }
        }


        public CoreAudio()
        {
            const uint REFTIMES_PER_SEC = 10000000;
            const uint CLSCTX_INPROC_SERVER = 1;            
            Guid clsid = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
            Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
            oEnumerator = null;
            uint hResult = CoCreateInstance(ref clsid, null, CLSCTX_INPROC_SERVER, ref IID_IUnknown, out oEnumerator);
            if (hResult != 0 || oEnumerator == null)
            {
                throw new Exception("CoCreateInstance() pInvoke failed");
            }

            iMde = oEnumerator as IMMDeviceEnumerator;
            if (iMde == null)
            {
                throw new Exception("COM cast failed to IMMDeviceEnumerator");
            }

            IntPtr pDevice = IntPtr.Zero;
            int retVal = iMde.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, ref pDevice);
            if (retVal != 0)
            {
                throw new Exception("IMMDeviceEnumerator.GetDefaultAudioEndpoint()");
            }
            //int dwStateMask = DEVICE_STATE_ACTIVE | DEVICE_STATE_NOTPRESENT | DEVICE_STATE_UNPLUGGED;
            //IntPtr pCollection = IntPtr.Zero;
            //retVal = iMde.EnumAudioEndpoints(EDataFlow.eRender, dwStateMask, ref pCollection);
            //if (retVal != 0)
            //{
            //    throw new Exception("IMMDeviceEnumerator.EnumAudioEndpoints()");
            //}
            imd = (IMMDevice) System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(pDevice);
            if (imd == null)
            {
                throw new Exception("COM cast failed to IMMDevice");
            }

            Guid iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
            uint dwClsCtx = (uint)CLSCTX.CLSCTX_ALL;
            IntPtr pActivationParams = IntPtr.Zero;
            IntPtr pEndPoint = IntPtr.Zero;
            retVal = imd.Activate(iid, dwClsCtx, pActivationParams, out pEndPoint);
            if (retVal != 0)
            {
                throw new Exception("IMMDevice.Activate()");
            }
            iAudioEndpoint = (IAudioEndpointVolume) System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(pEndPoint);
            if (iAudioEndpoint == null)
            {
                throw new Exception("COM cast failed to IAudioEndpointVolume");
            }

            iid = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
            dwClsCtx = (uint)CLSCTX.CLSCTX_ALL;
            pActivationParams = IntPtr.Zero;
            pEndPoint = IntPtr.Zero;
            retVal = imd.Activate(iid, dwClsCtx, pActivationParams, out pEndPoint);
            if (retVal != 0)
            {                
                throw new Exception("IAudioClient.Activate() " + Convert.ToString(retVal, 2));
            }

            iAudioClient = (IAudioClient) System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(pEndPoint);
            if (iAudioClient == null)
            {
                throw new Exception("COM cast failed to iAudioClient");
            }
            ulong processInterval;
            ulong minimumInterval;
            retVal=iAudioClient.GetDevicePeriod(out processInterval, out minimumInterval);
            if (retVal != 0)
            {
                throw new Exception("iAudioClient.GetDevicePeriod()");
            }

            waveformat = new WAVEFORMATEX();
            waveformat.wFormatTag = (ushort)WaveFormatEncoding.Pcm;
            waveformat.nChannels = 2;
            waveformat.nBlockAlign = 4;
            waveformat.wBitsPerSample = 16;
            waveformat.nSamplesPerSec = 44100;
            waveformat.cbSize = 0;
            waveformat.nAvgBytesPerSec = 176400;
            IntPtr reqForm = Marshal.AllocHGlobal(Marshal.SizeOf(waveformat));
            Marshal.StructureToPtr(waveformat, reqForm, false);

            IntPtr propForm = Marshal.AllocHGlobal(Marshal.SizeOf(waveformat));
            retVal = iAudioClient.IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, reqForm, out propForm);
            if (retVal != 0)
            {
                throw new Exception("IAudioClient.IsFormatSupported()");
            }

            retVal = iAudioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK, 2000000, 0, reqForm, Guid.Empty);
            if (retVal != 0)
            {
                throw new Exception("IAudioClient.Initialize() "+retVal);
            }
            uint buffersize=0;
            retVal = iAudioClient.GetBufferSize(out buffersize);
            //logentry("buffersize: " + buffersize);
            if (retVal != 0)
            {
                throw new Exception("IAudioClient.GetBufferSize()");
            }            
            iid=new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
            IntPtr capclient = IntPtr.Zero;
            retVal = iAudioClient.GetService(iid, out capclient);
            if (retVal != 0)
            {
                throw new Exception("IAudioClient.GetService()");
            }
            iAudioCaptureClient = (IAudioCaptureClient) System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(capclient);
            if (iAudioCaptureClient == null)
            {
                throw new Exception("COM cast failed to iAudioCaptureClient");
            }
            hnsActualDuration = (double)(REFTIMES_PER_SEC * buffersize / waveformat.nSamplesPerSec); // 8391 smallest possible value
            //logentry("hnsActualDuration "+hnsActualDuration);
            recordingthread = new Thread(recordingloop);
            recordingthread.IsBackground = false;
            recordingthread.Start();
        }

        public virtual void Dispose()
        {
            keepgoing = false;
            Thread.Sleep(20);
            if (iAudioCaptureClient != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(iAudioCaptureClient);
                iAudioCaptureClient = null;
            }
            if (iAudioClient != null) 
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(iAudioClient);
                iAudioClient = null;
            }
            if (iAudioEndpoint != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(iAudioEndpoint);
                iAudioEndpoint = null;
            }

            if (imd != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(imd);
                imd = null;
            }

            if (iMde != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(iMde);
                iMde = null;
            }

            if (oEnumerator != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oEnumerator);
                oEnumerator = null;
            }
        }           

        public bool Mute
        {
            get
            {
                bool mute = false;
                int retVal = iAudioEndpoint.GetMute(ref mute);
                if (retVal != 0)
                {
                    throw new Exception("IAudioEndpointVolume.GetMute() failed!");
                }
                return mute;
            }

            set
            {
                int retVal = iAudioEndpoint.SetMute(Convert.ToInt32(value), Guid.Empty);
                if (retVal != 0)
                {
                    throw new Exception("IAudioEndpointVolume.SetMute() failed!");
                }
            }
        }

        public float MasterVolume
        {
            get
            {
                float level = 0.0F;
                int retVal = iAudioEndpoint.GetMasterVolumeLevelScalar(ref level);
                if (retVal != 0)
                {
                    throw new Exception("IAudioEndpointVolume.GetMasterVolumeLevelScalar()");
                }
                return level;
            }
            set
            {
                int retVal = iAudioEndpoint.SetMasterVolumeLevelScalar(value, GuidEventContext);
                if (retVal != 0)
                {
                    throw new Exception("IAudioEndpointVolume.SetMasterVolumeLevelScalar()");
                }
            }
        }

        public uint GetChannelCount()
        {
            uint result = 0;
            int retVal = iAudioEndpoint.GetChannelCount(ref result);

            Marshal.ThrowExceptionForHR(retVal);
            return result;
        }

        public float GetChannelVolume(uint channel)
        {
            float level = 0.0F;

            int retVal = iAudioEndpoint.GetChannelVolumeLevelScalar(channel, ref level);

            Marshal.ThrowExceptionForHR(retVal);

            return level;
        }

        public void SetChannelVolume(uint channel, float level)
        {
            int retVal = iAudioEndpoint.SetChannelVolumeLevelScalar(channel, level, GuidEventContext);
            Marshal.ThrowExceptionForHR(retVal);
        }
        
        public void VolumeUp()
        {
            int retVal = iAudioEndpoint.VolumeStepUp(GuidEventContext);
            Marshal.ThrowExceptionForHR(retVal);
        }

        public void VolumeDown()
        {
            int retVal = iAudioEndpoint.VolumeStepDown(GuidEventContext);
            Marshal.ThrowExceptionForHR(retVal);
        }
    }
}
