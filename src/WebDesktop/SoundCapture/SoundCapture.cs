using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Security;

namespace SoundCapture
{
    public class WinMM : audioCapture
    {
        static public WaveHdr[] buffers = new WaveHdr[20];
        static public IntPtr[] unmanagedHeaders = new IntPtr[20];
        static public bool recording = false;
        static public int currentbuffer = 0;
        static public IntPtr phwi = IntPtr.Zero;
        static public MemoryStream memStream;
        static public MemoryStream waveStream;
        static public Object synclock = new Object();
        static public Object reclock = new Object();
        static public Mp3Writer _mp3writer;
        static public Thread _trd;
        static public long wavepointer = 0;
        static public long bytesread = 0;
        static public WaveFormat waveformat;
        static public AutoResetEvent syncthreads = new AutoResetEvent(false);
        static public WaveInDelegate callbackm = new WaveInDelegate(myDelegate);
        static public int maxstreamlength = 1000000;

        public static void myDelegate(IntPtr hwo, MIWM uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            switch (uMsg)
            {
                case MIWM.WIM_OPEN:
                    break;
                case MIWM.WIM_DATA:
                    WinMM.syncthreads.Set();
                    break;
                case MIWM.WIM_CLOSE:
                    break;
            }
        }

        public byte[] readbytes()
        {
            if (WinMM.recording)
            {
                lock (WinMM.synclock)
                {
                    if (WinMM.memStream.Length > WinMM.bytesread)
                    {
                        long oldpos = WinMM.memStream.Position;
                        int toread = (int)(WinMM.memStream.Length - WinMM.bytesread);
                        WinMM.memStream.Position = WinMM.bytesread;
                        byte[] buffer = new byte[toread];
                        WinMM.memStream.Read(buffer, 0, toread);
                        WinMM.memStream.Position = oldpos;
                        WinMM.bytesread += toread;
                        return buffer;
                    }
                }
            }
            return new byte[0];
        }        

        static public void wavtomp3()
        {
            try
            {
                while (WinMM.recording)
                {
                    WinMM.syncthreads.WaitOne();
                    WinMM.buffers[WinMM.currentbuffer] = (WaveHdr)Marshal.PtrToStructure(WinMM.unmanagedHeaders[WinMM.currentbuffer], typeof(WaveHdr));
                    byte[] bytes = new byte[WinMM.buffers[WinMM.currentbuffer].dwBytesRecorded];
                    Marshal.Copy(WinMM.buffers[WinMM.currentbuffer].lpData, bytes, 0, WinMM.buffers[WinMM.currentbuffer].dwBytesRecorded);
                    lock (WinMM.reclock)
                    {
                        WinMM.waveStream.Write(bytes, 0, bytes.Length);
                        if (WinMM.waveStream.Length > maxstreamlength)
                        {
                            lock (WinMM.synclock)
                            {
                                int bytestotransfer = 0;
                                MemoryStream tmpMemStream = new MemoryStream();
                                WinMM.waveStream.Position = WinMM.wavepointer;
                                bytestotransfer = (int)(WinMM.waveStream.Length - WinMM.waveStream.Position);
                                byte[] buffer2 = new byte[bytestotransfer];
                                WinMM.waveStream.Read(buffer2, 0, bytestotransfer);
                                tmpMemStream.Write(buffer2, 0, bytestotransfer);
                                WinMM.wavepointer = 0;
                                WinMM.waveStream.Dispose();
                                WinMM.waveStream = null;
                                WinMM.waveStream = tmpMemStream;
                            }
                        }
                    }
                    WinMM.ThrowExceptionForError(WinMM.waveInAddBuffer(WinMM.phwi, WinMM.unmanagedHeaders[WinMM.currentbuffer], Marshal.SizeOf(typeof(WinMM.WaveHdr))));
                    WinMM.currentbuffer = (WinMM.currentbuffer + 1) % WinMM.buffers.Length;

                    if (WinMM.waveStream.Length > WinMM.wavepointer)
                    {
                        int toread = (int)(WinMM.waveStream.Length - WinMM.wavepointer);
                        byte[] buffer = new byte[toread];
                        lock (WinMM.reclock)
                        {
                            long prevpos = WinMM.waveStream.Position;
                            WinMM.waveStream.Position = WinMM.wavepointer;
                            WinMM.waveStream.Read(buffer, 0, toread);
                            WinMM.waveStream.Position = prevpos;
                            WinMM.wavepointer += toread;
                        }
                        lock (WinMM.synclock)
                        {
                            WinMM._mp3writer.Write(buffer, 0, buffer.Length);
                        }

                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WaveHdr
        {
            public IntPtr lpData;
            public int dwBufferLength;
            public int dwBytesRecorded;
            public IntPtr dwUser;
            public WHDR dwFlags;
            public int dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [Flags]
        public enum WHDR
        {
            None = 0x0,
            Done = 0x00000001,
            Prepared = 0x00000002,
            BeginLoop = 0x00000004,
            EndLoop = 0x00000008,
            InQueue = 0x00000010
        }

        public enum MMRESULT : int
        {
            MMSYSERR_NOERROR = 0,
            MMSYSERR_ERROR = 1,
            MMSYSERR_BADDEVICEID = 2,
            MMSYSERR_NOTENABLED = 3,
            MMSYSERR_ALLOCATED = 4,
            MMSYSERR_INVALHANDLE = 5,
            MMSYSERR_NODRIVER = 6,
            MMSYSERR_NOMEM = 7,
            MMSYSERR_NOTSUPPORTED = 8,
            MMSYSERR_BADERRNUM = 9,
            MMSYSERR_INVALFLAG = 10,
            MMSYSERR_INVALPARAM = 11,
            MMSYSERR_HANDLEBUSY = 12,
            MMSYSERR_INVALIDALIAS = 13,
            MMSYSERR_BADDB = 14,
            MMSYSERR_KEYNOTFOUND = 15,
            MMSYSERR_READERROR = 16,
            MMSYSERR_WRITEERROR = 17,
            MMSYSERR_DELETEERROR = 18,
            MMSYSERR_VALNOTFOUND = 19,
            MMSYSERR_NODRIVERCB = 20,
            WAVERR_BADFORMAT = 32,
            WAVERR_STILLPLAYING = 33,
            WAVERR_UNPREPARED = 34
        }

        public const int MMSYSERR_NOERROR = 0;
        public const int MAXPNAMELEN = 32;
        public const int MIXER_LONG_NAME_CHARS = 64;
        public const int MIXER_SHORT_NAME_CHARS = 16;
        public const int MIXER_GETLINEINFOF_COMPONENTTYPE = 0x3;
        public const int MIXER_GETCONTROLDETAILSF_VALUE = 0x0;
        public const int MIXER_GETLINECONTROLSF_ONEBYTYPE = 0x2;
        public const int MIXER_SETCONTROLDETAILSF_VALUE = 0x0;
        public const int MIXERLINE_COMPONENTTYPE_DST_FIRST = 0x0;
        public const int MIXERLINE_COMPONENTTYPE_SRC_FIRST = 0x1000;
        public const int MIXERLINE_COMPONENTTYPE_DST_SPEAKERS = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 4);
        public const int MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 3);
        public const int MIXERLINE_COMPONENTTYPE_SRC_LINE = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 2);
        public const int MIXERCONTROL_CT_CLASS_FADER = 0x50000000;
        public const int MIXERCONTROL_CT_UNITS_UNSIGNED = 0x30000;
        public const int MIXERCONTROL_CT_CLASS_SWITCH = 0x20000000;
        public const int MIXERCONTROL_CT_SC_SWITCH_BOOLEAN = 0x0;
        public const int MIXERCONTROL_CT_UNITS_BOOLEAN = 0x10000;
        public const int MIXERCONTROL_CONTROLTYPE_FADER = (MIXERCONTROL_CT_CLASS_FADER | MIXERCONTROL_CT_UNITS_UNSIGNED);
        public const int MIXERCONTROL_CONTROLTYPE_VOLUME = (MIXERCONTROL_CONTROLTYPE_FADER + 1);
        public const int MIXERCONTROL_CONTROLTYPE_BOOLEAN = (MIXERCONTROL_CT_CLASS_SWITCH | MIXERCONTROL_CT_SC_SWITCH_BOOLEAN | MIXERCONTROL_CT_UNITS_BOOLEAN);

        public const int MIXERCONTROL_CONTROLTYPE_MUTE = (MIXERCONTROL_CONTROLTYPE_BOOLEAN + 2);

        [StructLayout(LayoutKind.Sequential)]
        public struct MIXERCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public int vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint fdwSupport;
            public uint cDestinations;
            public override String ToString()
            {
                return String.Format(" Manufacturer ID: {0}\n Product ID: {1}\n Driver Version: {2}\n Product Name: \"{3}\"\n Support: {4}\n Destinations: {5}\n", wMid, wPid, vDriverVersion, szPname, fdwSupport, cDestinations);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
            public override string ToString()
            {
                return string.Format(" wMid:{0}\n wPid:{1}\n vDriverVersion:{2}\n szPname:\"{3}\"\n dwFormats:{4}\n wChannels:{5}\n wReserved:{6}\n dwSupport:{7}\n", wMid, wPid, vDriverVersion, szPname, dwFormats, wChannels, wReserved1, dwSupport);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public int vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved;
            public override string ToString()
            {
                return string.Format(" wMid:{0}\n wPid:{1}\n vDriverVersion:{2}\n szPname:\"{3}\"\n dwFormats:{4}\n wChannels:{5}\n wReserved:{6}\n", wMid, wPid, vDriverVersion, szPname, dwFormats, wChannels, wReserved);
            }
        }

        public struct MIXERCONTROL
        {
            public int cbStruct;
            public int dwControlID;
            public int dwControlType;
            public int fdwControl;
            public int cMultipleItems;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)]
            public string szShortName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public string szName;
            public int lMinimum;
            public int lMaximum;
            [MarshalAs(UnmanagedType.U4, SizeConst = 10)]
            public int reserved;
        }

        public struct MIXERCONTROLDETAILS
        {
            public int cbStruct;
            public int dwControlID;
            public int cChannels;
            public int item;
            public int cbDetails;
            public IntPtr paDetails;
        }

        public struct MIXERCONTROLDETAILS_UNSIGNED
        {
            public int dwValue;
        }

        public struct MIXERCONTROLDETAILS_BOOLEAN
        {
            public int dwValue;
        }

        public struct MIXERLINE
        {
            public int cbStruct;
            public int dwDestination;
            public int dwSource;
            public int dwLineID;
            public int fdwLine;
            public int dwUser;
            public int dwComponentType;
            public int cChannels;
            public int cConnections;
            public int cControls;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)]
            public string szShortName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public string szName;
            public int dwType;
            public int dwDeviceID;
            public int wMid;
            public int wPid;
            public int vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)]
            public string szPname;
        }

        public struct MIXERLINECONTROLS
        {
            public int cbStruct;
            public int dwLineID;
            public int dwControl;
            public int cControls;
            public int cbmxctrl;
            public IntPtr pamxctrl;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveInGetNumDevs();
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutGetNumDevs();
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int waveInGetDevCaps(IntPtr uDeviceID, ref WAVEINCAPS pwic, uint cbwic);
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int waveOutGetDevCaps(IntPtr hwo, ref WAVEOUTCAPS pwoc, uint cbwoc);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerGetDevCaps(IntPtr uMxId, ref MIXERCAPS pmxcaps, uint cbmxcaps);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerClose(int hmx);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerGetControlDetailsA(int hmxobj, ref MIXERCONTROLDETAILS pmxcd, int fdwDetails);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerGetID(int hmxobj, int pumxID, int fdwId);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerGetLineControlsA(int hmxobj, ref MIXERLINECONTROLS pmxlc, int fdwControls);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerGetLineInfoA(int hmxobj, ref MIXERLINE pmxl, int fdwInfo);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerGetNumDevs();
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerMessage(int hmx, int uMsg, int dwParam1, int dwParam2);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerOpen(out int phmx, int uMxId, int dwCallback, int dwInstance, int fdwOpen);
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern int mixerSetControlDetails(int hmxobj, ref MIXERCONTROLDETAILS pmxcd, int fdwDetails);

        [DllImport("winmm.dll", EntryPoint = "sndPlaySoundA")]
        public static extern int sndPlaySound(string lpszSoundName, int uFlags);

        public delegate void WaveInDelegate(IntPtr hwo, MIWM uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        // WaveOut calls
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutWrite(IntPtr hWaveOut,IntPtr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, [In, MarshalAs(UnmanagedType.LPStruct)] WaveFormat lpFormat, WaveInDelegate dwCallback, int dwInstance, int dwFlags);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutReset(IntPtr hWaveOut);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutClose(IntPtr hWaveOut);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutPause(IntPtr hWaveOut);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutRestart(IntPtr hWaveOut);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);

        // WaveIn calls
        [DllImport("winmm.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
        public static extern int waveInAddBuffer(IntPtr hwi, IntPtr lpWaveInHdr, int cbwh);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveInClose(IntPtr hwi);
        [DllImport("winmm.dll", EntryPoint = "waveInOpen"), SuppressUnmanagedCodeSecurity]
        public static extern int waveInOpen(out IntPtr phwi, int uDeviceID, [In, MarshalAs(UnmanagedType.LPStruct)] WaveFormat lpFormat, WaveInDelegate dwCallback, IntPtr dwInstance, WaveOpenFlags dwFlags);
        [DllImport("winmm.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
        public static extern int waveInPrepareHeader(IntPtr hWaveIn, IntPtr lpWaveInHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
        public static extern int waveInUnprepareHeader(IntPtr hWaveIn, IntPtr lpWaveInHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveInReset(IntPtr hwi);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveInStart(IntPtr hwi);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveInStop(IntPtr hwi);

        [DllImport("winmm.dll", ExactSpelling = true, CharSet = CharSet.Unicode, EntryPoint = "waveInGetErrorTextW"),SuppressUnmanagedCodeSecurity]
        public static extern int GetErrorText(int errvalue,[Out] StringBuilder lpText,int uSize);

        public static void ThrowExceptionForError(int rc)
        {
            if (rc != 0)
            {
                StringBuilder foo = new StringBuilder(256);
                GetErrorText(rc, foo, 256);
                throw new Exception(foo.ToString());
            }
        }

        public enum MIWM
        {
            WIM_OPEN = 0x3BE,
            WIM_CLOSE = 0x3BF,
            WIM_DATA = 0x3C0
        }

        [Flags]
        public enum WaveOpenFlags
        {
            None = 0,
            FormatQuery = 0x0001,
            AllowSync = 0x0002,
            Mapped = 0x0004,
            FormatDirect = 0x0008,
            Null = 0x00000000,      /* no callback */
            Window = 0x00010000,    /* dwCallback is a HWND */
            Thread = 0x00020000,    /* dwCallback is a THREAD */
            Function = 0x00030000,  /* dwCallback is a FARPROC */
            Event = 0x00050000      /* dwCallback is an EVENT Handle */
        }

        public const int MM_WOM_OPEN = 0x3BB;
        public const int MM_WOM_CLOSE = 0x3BC;
        public const int MM_WOM_DONE = 0x3BD;

        public const int MM_WIM_OPEN = 0x3BE;
        public const int MM_WIM_CLOSE = 0x3BF;
        public const int MM_WIM_DATA = 0x3C0;

        public const int TIME_MS = 0x0001;  // time in milliseconds 
        public const int TIME_SAMPLES = 0x0002;  // number of wave samples 
        public const int TIME_BYTES = 0x0004;  // current byte offset 

        public const int SND_ASYNC = 0x1;
        public const int SND_LOOP = 0x8;

        public static void stoprecording()
        {
            try
            {
                WinMM.recording = false;
                for (int i = 0; i < WinMM.unmanagedHeaders.Length; i++)
                {
                    WinMM.buffers[i] = (WaveHdr)Marshal.PtrToStructure(WinMM.unmanagedHeaders[i], typeof(WinMM.WaveHdr));
                    int count = 0;
                    while (WinMM.waveInUnprepareHeader(WinMM.phwi, WinMM.unmanagedHeaders[i], Marshal.SizeOf(typeof(WinMM.WaveHdr))) != 0 && count++ < 10)
                    {
                        Thread.Sleep(2);
                    }
                    Marshal.FreeHGlobal(WinMM.buffers[i].lpData);
                    Marshal.DestroyStructure(WinMM.unmanagedHeaders[i], typeof(WinMM.WaveHdr));
                }
                if (WinMM.phwi != IntPtr.Zero)
                {
                    WinMM.waveInStop(WinMM.phwi);
                    WinMM.waveInClose(WinMM.phwi);
                }
                WinMM._mp3writer.Flush();
                WinMM._mp3writer.Close();
            }
            catch (Exception e)
            {
                throw (e);
                //WinMM.logerror(e);
            }
        }        

        static public void startrecoding()
        {
            try {

                WinMM.memStream = new MemoryStream();
                WinMM.waveStream = new MemoryStream();
                WinMM.bytesread = 0;
                WinMM.waveformat = new WaveFormat(44100, 16, 2);
                BE_CONFIG _mp3config = new BE_CONFIG(waveformat);
                WinMM._mp3writer = new Mp3Writer(memStream, waveformat, _mp3config);
                WinMM.recording = true;
                WinMM._trd = new Thread(WinMM.wavtomp3);
                WinMM._trd.IsBackground = true;
                WinMM._trd.Start();
                WinMM.ThrowExceptionForError(WinMM.waveInOpen(out WinMM.phwi, 0, WinMM.waveformat, WinMM.callbackm, IntPtr.Zero, WinMM.WaveOpenFlags.Function));
                int buffsize = WinMM.waveformat.AverageBytesPerSecond / 100;
                for (int i = 0; i < WinMM.buffers.Length; i++)
                {
                    WinMM.buffers[i] = new WinMM.WaveHdr();
                    WinMM.buffers[i].lpData = Marshal.AllocHGlobal(buffsize);
                    WinMM.buffers[i].dwBufferLength = buffsize;
                    WinMM.buffers[i].dwUser = IntPtr.Zero;
                    WinMM.buffers[i].dwFlags = WinMM.WHDR.None;
                    WinMM.buffers[i].dwLoops = 0;
                    WinMM.buffers[i].lpNext = IntPtr.Zero;
                    WinMM.buffers[i].reserved = IntPtr.Zero;
                    WinMM.unmanagedHeaders[i] = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinMM.WaveHdr)));
                    Marshal.StructureToPtr(WinMM.buffers[i], WinMM.unmanagedHeaders[i], false);
                    WinMM.ThrowExceptionForError(WinMM.waveInPrepareHeader(WinMM.phwi, WinMM.unmanagedHeaders[i], Marshal.SizeOf(typeof(WinMM.WaveHdr))));
                    WinMM.ThrowExceptionForError(WinMM.waveInAddBuffer(WinMM.phwi, WinMM.unmanagedHeaders[i], Marshal.SizeOf(typeof(WinMM.WaveHdr))));
                }
                WinMM.ThrowExceptionForError(WinMM.waveInStart(WinMM.phwi));
            }
            catch (Exception e)
            {
                throw e;
            }            

        }

        public WinMM(bool microphone=false)
        {
            WinMM.startrecoding();
        }

        public void Dispose()
        {
            WinMM.stoprecording();
            Thread.Sleep(50);
            WinMM._trd.Abort();
        }
    }

    public class CoreAudio : audioCapture
    {
        public int maxstreamlength = 10000000;
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

        private long bytesread = 0;

        public byte[] readbytes()
        {
            if (keepgoing)
            {
                lock (synclock)
                {
                    if (memStream.Length > bytesread)
                    {
                        long oldpos = memStream.Position;
                        int toread = (int)(memStream.Length - bytesread);
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
                        _mp3writer.Write(pData, 0, pData.Length);
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


        public CoreAudio(bool microphone = false)
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

            //iMde.EnumAudioEndpoints(EDataFlow.eCapture, DEVICE_STATE_ACTIVE,ref pDevice);
            int retVal;
            if (microphone)
                retVal = iMde.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, ref pDevice);
            else
                retVal = iMde.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, ref pDevice);            
            
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

            if (microphone)
                retVal = iAudioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE,0, 2000000, 0, reqForm, Guid.Empty);
            else
                retVal = iAudioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK, 2000000, 0, reqForm, Guid.Empty);
            
            if (retVal != 0)
            {
                throw new Exception("IAudioClient.Initialize() "+retVal);
            }
            uint buffersize=0;
            retVal = iAudioClient.GetBufferSize(out buffersize);
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
            recordingthread = new Thread(recordingloop);
            recordingthread.IsBackground = false;
            recordingthread.Start();
        }

        public void Dispose()
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

    public interface audioCapture
    {
        byte[] readbytes();
        void Dispose();
    }
}
