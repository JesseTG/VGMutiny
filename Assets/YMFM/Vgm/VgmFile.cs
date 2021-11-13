using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

[assembly: InternalsVisibleTo("YMFM.Editor")]

namespace Ymfm.Vgm
{
    public sealed class VgmFile : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private byte[] _data = Array.Empty<byte>();

        [SerializeField]
        private VgmHeader _header;

        public ReadOnlySpan<byte> Data => _data;


        public static VgmFile CreateInstance(ReadOnlySpan<byte> data)
        {
            var vgm = CreateInstance<VgmFile>();

            vgm._data = data.ToArray();
            vgm._header = new VgmHeader(data);

            return vgm;
        }

        public static VgmFile CreateInstance(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var vgm = CreateInstance<VgmFile>();

            vgm._data = data;
            vgm._header = new VgmHeader(data);

            return vgm;
        }


#if UNITY_EDITOR
        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            if (_header.Magic != VgmHeader.MagicNumber)
            {
                SirenixEditorGUI.ErrorMessageBox(
                    $"Expected a VGM file with a magic number of 0x{VgmHeader.MagicNumber:x8}, found 0x{_header.Magic:x8}. This is not a valid VGM file."
                );

                return;
            }

            _warnings.Clear().AppendLine("Encountered the following potential problems:\n");
            var foundWarnings = false;
            if (0x04 - 4 + _header.EndOfFileOffset > _data.Length)
            {
                foundWarnings = true;

                _warnings.AppendFormat(
                    "- Expected a file of size {0:N}B, actual is {1:N}B\n",
                    _header.EndOfFileOffset,
                    _data.Length
                );
            }

            if (_header.Version > 0x171)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- File version 0x{0:x4} is greater than 0x{1:x4}, some things may not work\n",
                    _header.Version,
                    VgmHeader.MaxVersion
                );
            }

            if (_header.Sn76489Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for SN76489 specified ({0:N}Hz), but not supported\n",
                    _header.Sn76489Clock
                );
            }

            if (_header.Version >= 0x151)
            {
                foundWarnings = AddVersion151Warnings(foundWarnings);
            }

            if (_header.Version >= 0x161)
            {
                foundWarnings = AddVersion161Warnings(foundWarnings);
            }

            if (_header.Version >= 0x171)
            {
                foundWarnings = AddVersion171Warnings(foundWarnings);
            }

            if (foundWarnings)
            {
                SirenixEditorGUI.WarningMessageBox(_warnings.ToString());
            }

            _warnings.Clear();
        }

        private bool AddVersion151Warnings(bool foundWarnings)
        {
            if (_header.SegaPcmClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for Sega PCM specified ({0:N}Hz), but not supported\n",
                    _header.SegaPcmClock
                );
            }

            if (_header.SegaPcmInterfaceRegister != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Sega PCM interface register specified (0x{0:x8}, but not supported\n",
                    _header.SegaPcmInterfaceRegister
                );
            }

            if (_header.Rf5c68Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for RF5C68 specified ({0:N}Hz), but not supported\n",
                    _header.Rf5c68Clock
                );
            }

            if (_header.Ymf271Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for YMF271 specified ({0:N}Hz), but not supported\n",
                    _header.Ymf271Clock
                );
            }

            if (_header.Ymz280bClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for YMZ280B specified ({0:N}Hz), but not supported\n",
                    _header.Ymz280bClock
                );
            }

            if (_header.Rf5c164Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for RF5C164 specified ({0:N}Hz), but not supported\n",
                    _header.Rf5c164Clock
                );
            }

            if (_header.PwmClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for PWM specified ({0:N}Hz), but not supported\n",
                    _header.PwmClock
                );
            }

            if (_header.Ay8910Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for AY8910 specified ({0:N}Hz), but not supported\n",
                    _header.Ay8910Clock
                );
            }

            if (_header.Ay8910ChipType != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Chip type for AY8910 specified (0x{0:x2}), but not supported\n",
                    _header.Ay8910ChipType
                );
            }

            if (_header.Ay8910Flags != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Flags for AY8910 specified (0x{0:x2}), but not supported\n",
                    _header.Ay8910Flags
                );
            }

            if (_header.Ym2203Ay8910Flags != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Flags for YM2203/AY8910 specified (0x{0:x2}), but not supported\n",
                    _header.Ym2203Ay8910Flags
                );
            }

            if (_header.Ym2608Ay8910Flags != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Flags for YM2608/AY8910 specified (0x{0:x2}), but not supported\n",
                    _header.Ym2608Ay8910Flags
                );
            }

            return foundWarnings;
        }

        private bool AddVersion161Warnings(bool foundWarnings)
        {
            if (_header.GameBoyDmgClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for Game Boy DMG specified ({0:N}Hz), but not supported\n",
                    _header.GameBoyDmgClock
                );
            }

            if (_header.NesApuClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for NES APU specified ({0:N}Hz), but not supported\n",
                    _header.NesApuClock
                );
            }

            if (_header.MultiPcmClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for MultiPCM specified ({0:N}Hz), but not supported\n",
                    _header.MultiPcmClock
                );
            }

            if (_header.Upd7759Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for uPD7759 specified ({0:N}Hz), but not supported\n",
                    _header.Upd7759Clock
                );
            }

            if (_header.Okim6258Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for OKIM6258 specified ({0:N}Hz), but not supported\n",
                    _header.Okim6258Clock
                );
            }

            if (_header.Okim6258Flags != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Flags for OKIM6258 specified (0x{0:x2}), but not supported\n",
                    _header.Okim6258Flags
                );
            }

            if (_header.K054539Flags != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Flags for K054539 specified (0x{0:x2}), but not supported\n",
                    _header.K054539Flags
                );
            }

            if (_header.C140ChipType != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Chip type for C140 specified (0x{0:x2}), but not supported\n",
                    _header.C140ChipType
                );
            }

            if (_header.Okim6295Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for OKIM6295 specified ({0:N}Hz), but not supported\n",
                    _header.Okim6295Clock
                );
            }

            if (_header.K051649Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for K051649 specified ({0:N}Hz), but not supported\n",
                    _header.K051649Clock
                );
            }

            if (_header.K054539Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for K054539 specified ({0:N}Hz), but not supported\n",
                    _header.K054539Clock
                );
            }

            if (_header.HuC6280Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for HuC6280 specified ({0:N}Hz), but not supported\n",
                    _header.HuC6280Clock
                );
            }

            if (_header.C140Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for C140 specified ({0:N}Hz), but not supported\n",
                    _header.C140Clock
                );
            }

            if (_header.K053260Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for K053260 specified ({0:N}Hz), but not supported\n",
                    _header.K053260Clock
                );
            }

            if (_header.PokeyClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for Pokey specified ({0:N}Hz), but not supported\n",
                    _header.PokeyClock
                );
            }

            if (_header.QsoundClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for QSound specified ({0:N}Hz), but not supported\n",
                    _header.QsoundClock
                );
            }

            return foundWarnings;
        }

        private bool AddVersion171Warnings(bool foundWarnings)
        {
            if (_header.WonderSwanClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for WonderSwan specified ({0:N}Hz), but not supported\n",
                    _header.WonderSwanClock
                );
            }

            if (_header.VsuClock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for VSU specified ({0:N}Hz), but not supported\n",
                    _header.VsuClock
                );
            }

            if (_header.Saa1099Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for SAA1099 specified ({0:N}Hz), but not supported\n",
                    _header.Saa1099Clock
                );
            }

            if (_header.Es5503Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for ES5503 specified ({0:N}Hz), but not supported\n",
                    _header.Es5503Clock
                );
            }

            if (_header.Es5505Es5506Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for ES5505/ES5506 specified ({0:N}Hz), but not supported\n",
                    _header.Es5505Es5506Clock
                );
            }

            // TODO: Log that the ES550x's various flags aren't supported

            if (_header.X1010Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for X1-010 specified ({0:N}Hz), but not supported\n",
                    _header.X1010Clock
                );
            }

            if (_header.C352Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for C352 specified ({0:N}Hz), but not supported\n",
                    _header.C352Clock
                );
            }

            if (_header.Ga20Clock != 0)
            {
                foundWarnings = true;
                _warnings.AppendFormat(
                    "- Clock for GA20 specified ({0:N}Hz), but not supported\n",
                    _header.Ga20Clock
                );
            }

            return foundWarnings;
        }

        private static readonly StringBuilder _warnings = new();
#endif
    }
}
