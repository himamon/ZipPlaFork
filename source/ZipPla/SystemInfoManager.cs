using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class SystemInfoManager
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool VerifyVersionInfo(
       [System.Runtime.InteropServices.In]
    ref OSVERSIONINFOEX lpVersionInfo,
       uint dwTypeMask,
       ulong dwlConditionMask);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern ulong VerSetConditionMask(
            ulong dwlConditionMask,
            uint dwTypeBitMask,
            byte dwConditionMask);

        private const uint VER_MINORVERSION = 0x0000001; //dwMajorVersion
        private const uint VER_MAJORVERSION = 0x0000002; //dwMinorVersion
        private const uint VER_BUILDNUMBER = 0x0000004; //dwBuildNumber
        private const uint VER_PLATFORMID = 0x0000008; //dwPlatformId
        private const uint VER_SERVICEPACKMINOR = 0x0000010; //wServicePackMajor
        private const uint VER_SERVICEPACKMAJOR = 0x0000020; //wServicePackMinor
        private const uint VER_SUITENAME = 0x0000040; //wSuiteMask
        private const uint VER_PRODUCT_TYPE = 0x0000080; //wProductType

        //現在の値と指定された値が同じでなければならない
        private const byte VER_EQUAL = 1;
        //現在の値が指定された値より大きくなければならない
        private const byte VER_GREATER = 2;
        //現在の値が指定された値より大きいか同じでなければならない
        private const byte VER_GREATER_EQUAL = 3;
        //現在の値が指定された値より小さいくなければならない
        private const byte VER_LESS = 4;
        //現在の値が指定された値より小さいか同じでなければならない
        private const byte VER_LESS_EQUAL = 5;
        //指定されたwSuiteMaskがすべて含まれていななければならない
        private const byte VER_AND = 6;
        //指定されたwSuiteMaskの少なくとも1つが含まれていななければならない
        private const byte VER_OR = 7;

        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [System.Runtime.InteropServices.MarshalAs(
                System.Runtime.InteropServices.UnmanagedType.ByValTStr,
                SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        /// <summary>
        /// 現在のOSが指定されたバージョン以上かを調べる
        /// </summary>
        /// <param name="majorVersion">
        /// メジャーバージョン番号。負の数の時は調べない。</param>
        /// <param name="minorVersion">
        /// マイナーバージョン番号。負の数の時は調べない。</param>
        /// <param name="servicePackMajor">
        /// サービスパックのメジャーバージョン番号。負の数の時は調べない。</param>
        /// <returns>現在のOSが指定されたバージョン以上ならTrue。</returns>
        public static bool IsWindowsVersionOrGreater(
            int majorVersion, int minorVersion, int servicePackMajor)
        {
            if (majorVersion < 0 && minorVersion < 0 && servicePackMajor < 0)
            {
                return true;
            }

            //lpVersionInfo、dwTypeMask、dwlConditionMaskを作成する
            OSVERSIONINFOEX osvi = new OSVERSIONINFOEX();
            uint typeMask = 0;
            ulong conditionMask = 0;
            if (0 < majorVersion)
            {
                osvi.dwMajorVersion = (uint)majorVersion;
                conditionMask = VerSetConditionMask(
                    conditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
                typeMask |= VER_MAJORVERSION;
            }
            if (0 < minorVersion)
            {
                osvi.dwMinorVersion = (uint)minorVersion;
                conditionMask = VerSetConditionMask(
                    conditionMask, VER_MINORVERSION, VER_GREATER_EQUAL);
                typeMask |= VER_MINORVERSION;
            }
            if (0 < servicePackMajor)
            {
                osvi.wServicePackMajor = (short)servicePackMajor;
                conditionMask = VerSetConditionMask(
                    conditionMask, VER_SERVICEPACKMAJOR, VER_GREATER_EQUAL);
                typeMask |= VER_SERVICEPACKMAJOR;
            }

            //VerifyVersionInfoを呼び出す
            return VerifyVersionInfo(ref osvi, typeMask, conditionMask);
        }
    }
}
