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
        private const uint MagicNumber = 0x56676d20;

        [SerializeField, HideInInspector]
        private byte[] _data = Array.Empty<byte>();

        [SerializeField]
        private VgmHeader _header;

        public ReadOnlySpan<byte> Data => _data;

        [Conditional("UNITY_EDITOR")]
        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            var data = Data;
            var magic = BinaryPrimitives.ReadInt32LittleEndian(data);
            if (magic != MagicNumber)
            {
                SirenixEditorGUI.ErrorMessageBox(
                    $"Expected a VGM file with a magic number of {MagicNumber:X8}, found {magic:X8}. This is not a valid VGM file."
                );

                return;
            }

            StringBuilder warnings;
        }

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
    }
}
