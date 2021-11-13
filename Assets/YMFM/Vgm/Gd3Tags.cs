using System;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Ymfm.Vgm;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace Ymfm
{
    [Serializable]
    public sealed class Gd3Tags : IEquatable<Gd3Tags>
    {
        public const uint MagicNumber = 0x_20_33_64_47; // "Gd3 "
        public const uint CurrentVersion = 0x00_01_00_00;

        [SerializeField]
        [CustomValueDrawer("HexLabelDrawer")]
        [LabelText("Magic Number")]
        private uint magic;

        public uint Magic => magic;

        [SerializeField]
        [CustomValueDrawer("HexLabelDrawer")]
        private uint version;

        public uint Version => version;

        [SerializeField]
        [CustomValueDrawer("LengthDrawer")]
        private uint length;

        public uint Length => length;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string trackName;

        public string TrackName => trackName;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        [LabelText("Track Name (JP)")]
        private string trackNameJp;

        public string TrackNameJp => trackNameJp;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string gameName;

        public string GameName => gameName;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        [LabelText("Game Name (JP)")]
        private string gameNameJp;

        public string GameNameJp => gameNameJp;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string systemName;

        public string SystemName => systemName;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        [LabelText("System Name (JP)")]
        private string systemNameJp;

        public string SystemNameJp => systemNameJp;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string composer;

        public string Composer => composer;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        [LabelText("Composer (JP)")]
        private string composerJp;

        public string ComposerJp => composerJp;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string releaseDate;

        public string ReleaseDate => releaseDate;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string transcriber;

        public string Transcriber => transcriber;

        [SerializeField]
        [CustomValueDrawer("TextDrawer")]
        private string notes;

        public string Notes => notes;

        public Gd3Tags(ReadOnlySpan<byte> input)
        {
            switch (input.Length)
            {
                case 0:
                    throw new ArgumentException("Input is empty");
                case < 4:
                    throw new ArgumentException("No magic number");
            }

            magic = input.ReadUInt32LittleEndian(0x00); // Magic number is really an ASCII string
            version = input.ReadUInt32LittleEndian(0x04);
            length = input.ReadUInt32LittleEndian(0x08);

            var data = MemoryMarshal.Cast<byte, char>(input[0x0c..]);
            data = data.NextNullTerminatedString(out trackName);
            data = data.NextNullTerminatedString(out trackNameJp);
            data = data.NextNullTerminatedString(out gameName);
            data = data.NextNullTerminatedString(out gameNameJp);
            data = data.NextNullTerminatedString(out systemName);
            data = data.NextNullTerminatedString(out systemNameJp);
            data = data.NextNullTerminatedString(out composer);
            data = data.NextNullTerminatedString(out composerJp);
            data = data.NextNullTerminatedString(out releaseDate);
            data = data.NextNullTerminatedString(out transcriber);
            data = data.NextNullTerminatedString(out notes);
        }

        public bool Equals(Gd3Tags other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return
                magic == other.magic &&
                version == other.version &&
                length == other.length &&
                trackName == other.trackName &&
                trackNameJp == other.trackNameJp &&
                gameName == other.gameName &&
                gameNameJp == other.gameNameJp &&
                systemName == other.systemName &&
                systemNameJp == other.systemNameJp &&
                composer == other.composer &&
                composerJp == other.composerJp &&
                releaseDate == other.releaseDate &&
                transcriber == other.transcriber &&
                notes == other.notes
                ;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Gd3Tags other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(magic);
            hashCode.Add(version);
            hashCode.Add(length);
            hashCode.Add(trackName);
            hashCode.Add(trackNameJp);
            hashCode.Add(gameName);
            hashCode.Add(gameNameJp);
            hashCode.Add(systemName);
            hashCode.Add(systemNameJp);
            hashCode.Add(composer);
            hashCode.Add(composerJp);
            hashCode.Add(releaseDate);
            hashCode.Add(transcriber);
            hashCode.Add(notes);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(Gd3Tags left, Gd3Tags right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Gd3Tags left, Gd3Tags right)
        {
            return !Equals(left, right);
        }

#if UNITY_EDITOR
        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            if (Magic != MagicNumber)
            {
                SirenixEditorGUI.ErrorMessageBox(
                    $"Expected a GD3 section with a magic number of 0x{MagicNumber:x8}, found 0x{Magic:x8}. Section is invalid."
                );
            }
        }

        private static uint HexLabelDrawer(uint value, GUIContent label)
        {
            EditorGUILayout.LabelField(label.ToString(), $"0x{value:x8}");
            return value;
        }

        private static uint LengthDrawer(uint value, GUIContent label)
        {
            EditorGUILayout.LabelField(
                label.ToString(),
                $"{value:N0} B == {StringUtilities.NicifyByteSize((int)value)} (0x{value:x8})"
            );
            return value;
        }

        private static string TextDrawer(string value, GUIContent label)
        {
            EditorGUILayout.LabelField(label.ToString(), string.IsNullOrEmpty(value) ? "<none>" : value);
            return value;
        }
#endif
    }
}
