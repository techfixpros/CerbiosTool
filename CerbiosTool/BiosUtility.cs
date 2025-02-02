﻿using System.Diagnostics;
using System.Text;

namespace CerbiosTool
{
    public static class BiosUtility
    {
        public static int SearchData(byte[] data, byte[] searchPattern)
        {
            if (data.Length < searchPattern.Length)
            {
                return -1;
            }
            for (var i = 0; i < data.Length; ++i)
            {
                for (var j = 0; j < searchPattern.Length; ++j)
                {
                    if ((i + j) >= data.Length || searchPattern[j] != data[i + j])
                    {
                        break;
                    }
                    if (j == searchPattern.Length - 1)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static string GetString(byte[] buffer, int offset, int maxLength)
        {
            var result = string.Empty;
            for (var i = 0; i < maxLength; i++)
            {
                var value = (byte)Encoding.UTF8.GetString(buffer, i + offset, 1)[0];
                if (value == 0)
                {
                    break;
                }
                result += (char)value;
            }
            return result;
        }

        private static void SetString(string value, byte[] buffer, int offset, int maxLength)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            for (var i = 0; i < maxLength; i++)
            {
                if (i < valueBytes.Length)
                {
                    buffer[i + offset] = valueBytes[i];
                }
                else
                {
                    buffer[i + offset] = 0;
                }
            }
        }

        private static ushort IGRToUshort(string value)
        {
            ushort igrValue = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var shift = Convert.ToUInt16(value[i].ToString(), 16);
                igrValue = (ushort)(igrValue | (1 << (shift ^ 8)));
            }
            return igrValue;
        }

        private static string ShortToIGR(ushort value)
        {
            var temp = string.Empty;
            for (var i = 0; i < 16; i++)
            {
                var shift = 1 << (i ^ 8);
                if ((value & shift) > 0)
                {
                    temp += i.ToString("X1");
                }
            }
            return temp;
        }

        public static bool LoadBiosComfig(string path, ref Config config, ref byte[] biosData, ref string configMainVersion, ref string confiIgrVersion)
        {
            biosData = File.ReadAllBytes(path);

            var searchPatternMain = new byte[] { (byte)'C', (byte)'E', (byte)'R', (byte)'B', (byte)'I', (byte)'O', (byte)'S', (byte)'-', (byte)'M', (byte)'A', (byte)'I', (byte)'N', (byte)'-', (byte)'-' };
            var configOffsetMain = SearchData(biosData, searchPatternMain);
            if (configOffsetMain >= 0)
            {                
                var version = new string(new char[] { (char)biosData[configOffsetMain + 14], (char)biosData[configOffsetMain + 15] });
                if (version != "01" && version != "02" && version != "03")
                {
                    return false;
                }

                configMainVersion = version;

                config.LoadConfig = biosData[configOffsetMain + 16];
                config.AVCheck = biosData[configOffsetMain + 17];
                config.Debug = biosData[configOffsetMain + 18];
                config.DriveSetup = biosData[configOffsetMain + 19];
                config.CdPath1 = GetString(biosData, configOffsetMain + 20, 100);
                config.CdPath2 = GetString(biosData, configOffsetMain + 120, 100);
                config.CdPath3 = GetString(biosData, configOffsetMain + 220, 100);
                config.DashPath1 = GetString(biosData, configOffsetMain + 320, 100);
                config.DashPath2 = GetString(biosData, configOffsetMain + 420, 100);
                config.DashPath3 = GetString(biosData, configOffsetMain + 520, 100);
                config.BootAnimPath = GetString(biosData, configOffsetMain + 620, 100);
                config.FrontLed = GetString(biosData, configOffsetMain + 720, 5);
                config.FanSpeed = biosData[configOffsetMain + 725];
                config.UDMAModeMaster = biosData[configOffsetMain + 726];

                var configOffset = 0;

                if (version == "03")
                {
                    config.UDMAModeSlave = biosData[configOffsetMain + 727];
                    configOffset += 1;
                }

                if (version == "02" || version == "03")
                {
                    config.Force480p = biosData[configOffsetMain + 727 + configOffset];
                    config.ForceVGA = biosData[configOffsetMain + 728 + configOffset];
                    config.RTCEnable = biosData[configOffsetMain + 729 + configOffset];
                    config.BlockDashupdate = biosData[configOffsetMain + 730 + configOffset];
                    configOffset += 4;
                }

                config.SplashBackground = (uint)((biosData[configOffsetMain + 727 + configOffset]) | (biosData[configOffsetMain + 728 + configOffset] << 8) | biosData[configOffsetMain + 729 + configOffset] << 16);
                config.SplashCerbiosText = (uint)((biosData[configOffsetMain + 731 + configOffset]) | (biosData[configOffsetMain + 732 + configOffset] << 8) | biosData[configOffsetMain + 733 + configOffset] << 16);
                config.SplashSafeModeText = (uint)((biosData[configOffsetMain + 735 + configOffset]) | (biosData[configOffsetMain + 736 + configOffset] << 8) | biosData[configOffsetMain + 737 + configOffset] << 16);
                config.SplashLogo1 = (uint)((biosData[configOffsetMain + 739 + configOffset]) | (biosData[configOffsetMain + 740 + configOffset] << 8) | biosData[configOffsetMain + 741 + configOffset] << 16);
                config.SplashLogo2 = (uint)((biosData[configOffsetMain + 743 + configOffset]) | (biosData[configOffsetMain + 744 + configOffset] << 8) | biosData[configOffsetMain + 745 + configOffset] << 16);
                config.SplashLogo3 = (uint)((biosData[configOffsetMain + 747 + configOffset]) | (biosData[configOffsetMain + 748 + configOffset] << 8) | biosData[configOffsetMain + 749 + configOffset] << 16);
                config.SplashLogo4 = (uint)((biosData[configOffsetMain + 751 + configOffset]) | (biosData[configOffsetMain + 752 + configOffset] << 8) | biosData[configOffsetMain + 753 + configOffset] << 16);
                config.SplashScale = biosData[configOffsetMain + 755 + configOffset];
            }

            var searchPatternIGR = new byte[] { (byte)'C', (byte)'E', (byte)'R', (byte)'B', (byte)'I', (byte)'O', (byte)'S', (byte)'-', (byte)'I', (byte)'G', (byte)'R', (byte)'-', (byte)'-', (byte)'-' };
            var configOffsetIGR = SearchData(biosData, searchPatternIGR);
            if (configOffsetIGR >= 0)
            {
                var version = new string(new char[] { (char)biosData[configOffsetIGR + 14], (char)biosData[configOffsetIGR + 15] });
                if (version != "01" && version != "02" && version != "03")
                {
                    return false;
                }

                confiIgrVersion = version;

                config.IGRMasterPort = biosData[configOffsetIGR + 16];
                config.IGRDash = ShortToIGR((ushort)((biosData[configOffsetIGR + 17]) | (biosData[configOffsetIGR + 18] << 8)));
                config.IGRGame = ShortToIGR((ushort)((biosData[configOffsetIGR + 20]) | (biosData[configOffsetIGR + 21] << 8)));
                config.IGRFull = ShortToIGR((ushort)((biosData[configOffsetIGR + 23]) | (biosData[configOffsetIGR + 24] << 8)));
                config.IGRShutdown = ShortToIGR((ushort)((biosData[configOffsetIGR + 26]) | (biosData[configOffsetIGR + 27] << 8)));

                if (version == "02" || version == "03")
                {
                    config.IGRCycle = ShortToIGR((ushort)((biosData[configOffsetIGR + 29]) | (biosData[configOffsetIGR + 30] << 8)));
                }
            }
            return true;
        }

        public static void SaveBiosConfig(Config config, string loadPath, string savePath, byte[] biosData, bool bfm)
        {
            var searchPatternMain = new byte[] { (byte)'C', (byte)'E', (byte)'R', (byte)'B', (byte)'I', (byte)'O', (byte)'S', (byte)'-', (byte)'M', (byte)'A', (byte)'I', (byte)'N', (byte)'-', (byte)'-' };
            var configOffsetMain = SearchData(biosData, searchPatternMain);
            if (configOffsetMain >= 0)
            {
                var version = new string(new char[] { (char)biosData[configOffsetMain + 14], (char)biosData[configOffsetMain + 15] });
                if (version != "01" && version != "02" && version != "03")
                {
                    return;
                }

                biosData[configOffsetMain + 16] = config.LoadConfig;
                biosData[configOffsetMain + 17] = config.AVCheck;
                biosData[configOffsetMain + 18] = config.Debug;
                biosData[configOffsetMain + 19] = config.DriveSetup;
                SetString(config.CdPath1, biosData, configOffsetMain + 20, 100);
                SetString(config.CdPath2, biosData, configOffsetMain + 120, 100);
                SetString(config.CdPath3, biosData, configOffsetMain + 220, 100);
                SetString(config.DashPath1, biosData, configOffsetMain + 320, 100);
                SetString(config.DashPath2, biosData, configOffsetMain + 420, 100);
                SetString(config.DashPath3, biosData, configOffsetMain + 520, 100);
                SetString(config.BootAnimPath, biosData, configOffsetMain + 620, 100);
                SetString(config.FrontLed.PadRight(4, 'O'), biosData, configOffsetMain + 720, 5);
                biosData[configOffsetMain + 725] = config.FanSpeed;
                biosData[configOffsetMain + 726] = config.UDMAModeMaster;

                var configOffset = 0;

                if (version == "03")
                {
                    biosData[configOffsetMain + 727] = config.UDMAModeSlave;
                    configOffset += 1;
                }

                if (version == "02" || version == "03")
                {
                    biosData[configOffsetMain + 727 + configOffset] = config.Force480p;
                    biosData[configOffsetMain + 728 + configOffset] = config.ForceVGA;
                    biosData[configOffsetMain + 729 + configOffset] = config.RTCEnable;
                    biosData[configOffsetMain + 730 + configOffset] = config.BlockDashupdate;
                    configOffset += 4;
                }

                biosData[configOffsetMain + 727 + configOffset] = (byte)((config.SplashBackground) & 0xff);
                biosData[configOffsetMain + 728 + configOffset] = (byte)((config.SplashBackground >> 8) & 0xff);
                biosData[configOffsetMain + 729 + configOffset] = (byte)((config.SplashBackground >> 16) & 0xff);
                biosData[configOffsetMain + 731 + configOffset] = (byte)((config.SplashCerbiosText) & 0xff);
                biosData[configOffsetMain + 732 + configOffset] = (byte)((config.SplashCerbiosText >> 8) & 0xff);
                biosData[configOffsetMain + 733 + configOffset] = (byte)((config.SplashCerbiosText >> 16) & 0xff);
                biosData[configOffsetMain + 735 + configOffset] = (byte)((config.SplashSafeModeText) & 0xff);
                biosData[configOffsetMain + 736 + configOffset] = (byte)((config.SplashSafeModeText >> 8) & 0xff);
                biosData[configOffsetMain + 737 + configOffset] = (byte)((config.SplashSafeModeText >> 16) & 0xff);
                biosData[configOffsetMain + 739 + configOffset] = (byte)((config.SplashLogo1) & 0xff);
                biosData[configOffsetMain + 740 + configOffset] = (byte)((config.SplashLogo1 >> 8) & 0xff);
                biosData[configOffsetMain + 741 + configOffset] = (byte)((config.SplashLogo1 >> 16) & 0xff);
                biosData[configOffsetMain + 743 + configOffset] = (byte)((config.SplashLogo2) & 0xff);
                biosData[configOffsetMain + 744 + configOffset] = (byte)((config.SplashLogo2 >> 8) & 0xff);
                biosData[configOffsetMain + 745 + configOffset] = (byte)((config.SplashLogo2 >> 16) & 0xff);
                biosData[configOffsetMain + 747 + configOffset] = (byte)((config.SplashLogo3) & 0xff);
                biosData[configOffsetMain + 748 + configOffset] = (byte)((config.SplashLogo3 >> 8) & 0xff);
                biosData[configOffsetMain + 749 + configOffset] = (byte)((config.SplashLogo3 >> 16) & 0xff);
                biosData[configOffsetMain + 751 + configOffset] = (byte)((config.SplashLogo4) & 0xff);
                biosData[configOffsetMain + 752 + configOffset] = (byte)((config.SplashLogo4 >> 8) & 0xff);
                biosData[configOffsetMain + 753 + configOffset] = (byte)((config.SplashLogo4 >> 16) & 0xff);
                biosData[configOffsetMain + 755 + configOffset] = config.SplashScale;
            }

            var searchPatternIGR = new byte[] { (byte)'C', (byte)'E', (byte)'R', (byte)'B', (byte)'I', (byte)'O', (byte)'S', (byte)'-', (byte)'I', (byte)'G', (byte)'R', (byte)'-', (byte)'-', (byte)'-' };
            var configOffsetIGR = SearchData(biosData, searchPatternIGR);
            if (configOffsetIGR >= 0)
            {
                var version = new string(new char[] { (char)biosData[configOffsetIGR + 14], (char)biosData[configOffsetIGR + 15] });
                if (version != "01" && version != "02" && version != "03")
                {
                    return;
                }

                biosData[configOffsetIGR + 16] = config.IGRMasterPort;
                var tempIgrDash = IGRToUshort(config.IGRDash);
                biosData[configOffsetIGR + 17] = (byte)((tempIgrDash) & 0xff);
                biosData[configOffsetIGR + 18] = (byte)((tempIgrDash >> 8) & 0xff);
                var tempIgrGame = IGRToUshort(config.IGRGame);
                biosData[configOffsetIGR + 20] = (byte)((tempIgrGame) & 0xff);
                biosData[configOffsetIGR + 21] = (byte)((tempIgrGame >> 8) & 0xff);
                var tempIgrFull = IGRToUshort(config.IGRFull);
                biosData[configOffsetIGR + 23] = (byte)((tempIgrFull) & 0xff);
                biosData[configOffsetIGR + 24] = (byte)((tempIgrFull >> 8) & 0xff);
                var tempIgrShutdown = IGRToUshort(config.IGRShutdown);
                biosData[configOffsetIGR + 26] = (byte)((tempIgrShutdown) & 0xff);
                biosData[configOffsetIGR + 27] = (byte)((tempIgrShutdown >> 8) & 0xff);

                if (version == "02" || version == "03")
                {
                    var tempIgrCycle = IGRToUshort(config.IGRCycle);
                    biosData[configOffsetIGR + 29] = (byte)((tempIgrCycle) & 0xff);
                    biosData[configOffsetIGR + 30] = (byte)((tempIgrCycle >> 8) & 0xff);
                }
            }

            if (bfm == true)
            {
                for (var i = 0; i < BFM.PatchData.Length; i++)
                {
                    biosData[i + BFM.PatchOffset] = BFM.PatchData[i];
                }
            }
            else
            {
                for (var i = 0; i < NonBFM.PatchData.Length; i++)
                {
                    biosData[i + NonBFM.PatchOffset] = NonBFM.PatchData[i];
                }
            }

            File.WriteAllBytes(savePath, biosData);
        }
    }
}
