/*
 * This file is:
 *
 * 1. Modified version of https://github.com/Duulis/Steam-Library-Manager/blob/master/Source/Steam%20Library%20Manager/Framework/KeyValue.cs
 * All credits goes to Steam-Library-Manager developers.
 *
 * 2. Subject to the terms and conditions defined in
 * file 'license.txt', which can be found at https://github.com/SteamRE/SteamKit/blob/master/SteamKit2/SteamKit2/license.txt
 * All credits goes to SteamKit developers
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DotLauncher.LibraryProviders.Steam
{
    internal class KeyValueReader : StreamReader
    {
        private static readonly Dictionary<char, char> EscapedMapping = new Dictionary<char, char>
        {
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' },
        };

        public KeyValueReader(KeyValue kv, Stream input)
            : base(input)
        {
            var currentKey = kv;

            do
            {
                var s = ReadToken(out _, out _);

                if (string.IsNullOrEmpty(s)) { break; }

                if (currentKey == null)
                {
                    currentKey = new KeyValue(s);
                }
                else
                {
                    currentKey.Name = s;
                }

                s = ReadToken(out var wasQuoted, out var wasConditional);

                if (wasConditional)
                {
                    s = ReadToken(out wasQuoted, out wasConditional);
                }

                if (s.StartsWith("{") && !wasQuoted)
                {
                    // header is valid so load the file
                    currentKey.RecursiveLoadFromBuffer(this);
                }
                else
                {
                    throw new Exception("LoadFromBuffer: missing {");
                }

                currentKey = null;
            }
            while (!EndOfStream);
        }

        private void EatWhiteSpace()
        {
            while (!EndOfStream)
            {
                if (!char.IsWhiteSpace((char)Peek())) { break; }
                Read();
            }
        }

        private bool EatCppComment()
        {
            if (!EndOfStream)
            {
                var next = (char)Peek();

                if (next == '/')
                {
                    Read();

                    if (next == '/')
                    {
                        ReadLine();
                        return true;
                    }

                    throw new Exception("BARE / WHAT ARE YOU DOIOIOIINODGNOIGNONGOIGNGGGGGGG");
                }

                return false;
            }

            return false;
        }

        public string ReadToken(out bool wasQuoted, out bool wasConditional)
        {
            wasQuoted = false;
            wasConditional = false;

            while (true)
            {
                EatWhiteSpace();

                if (EndOfStream) { return null; }
                if (!EatCppComment()) { break; }
            }

            if (EndOfStream) { return null; }

            var next = (char)Peek();

            if (next == '"')
            {
                wasQuoted = true;
                // "
                Read();
                var sb = new StringBuilder();

                while (!EndOfStream)
                {
                    if (Peek() == '\\')
                    {
                        Read();
                        var escapedChar = (char)Read();

                        sb.Append(EscapedMapping.TryGetValue(escapedChar, out var replacedChar)
                            ? replacedChar
                            : escapedChar);

                        continue;
                    }

                    if (Peek() == '"') { break; }
                    sb.Append((char)Read());
                }

                // "
                Read();

                return sb.ToString();
            }

            if (next == '{' || next == '}')
            {
                Read();
                return next.ToString();
            }

            var bConditionalStart = false;
            var ret = new StringBuilder();

            while (!EndOfStream)
            {
                next = (char)Peek();
                if (next == '"' || next == '{' || next == '}') { break; }
                if (next == '[') { bConditionalStart = true; }
                if (next == ']' && bConditionalStart) { wasConditional = true; }
                if (char.IsWhiteSpace(next)) { break; }
                ret.Append(next);
                Read();
            }

            return ret.ToString();
        }
    }

    /// <summary>
    /// Represents a recursive string key to arbitrary value container.
    /// </summary>
    public class KeyValue
    {
        private enum Type : byte
        {
            None = 0,
            String = 1,
            Int32 = 2,
            Float32 = 3,
            Pointer = 4,
            WideString = 5,
            Color = 6,
            UInt64 = 7,
            End = 8,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValue"/> class.
        /// </summary>
        /// <param name="name">The optional name of the root key.</param>
        /// <param name="value">The optional value assigned to the root key.</param>
        public KeyValue(string name = null, string value = null)
        {
            this.Name = name;
            this.Value = value;

            Children = new List<KeyValue>();
        }

        /// <summary>
        /// Represents an invalid <see cref="KeyValue"/> given when a searched for child does not exist.
        /// </summary>
        private static readonly KeyValue Invalid = new KeyValue();

        /// <summary>
        /// Gets or sets the name of this instance.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the value of this instance.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets the children of this instance.
        /// </summary>
        public List<KeyValue> Children { get; private set; }


        /// <summary>
        /// Gets the child <see cref="KeyValue"/> with the specified key.
        /// If no child with this key exists, <see cref="Invalid"/> is returned.
        /// </summary>
        public KeyValue this[string key]
        {
            get
            {
                var child = this.Children
                    .FirstOrDefault(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase));

                return child ?? Invalid;
            }
        }

        /// <summary>
        /// Returns the value of this instance as a string.
        /// </summary>
        /// <returns>The value of this instance as a string.</returns>
        private string AsString() => this.Value;

        /// <summary>
        /// Attempts to convert and return the value of this instance as an integer.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an integer.</returns>
        public int AsInteger(int defaultValue = default(int))
        {
            return int.TryParse(this.Value, out var value) == false ? defaultValue : value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as a long.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an long.</returns>
        public long AsLong(long defaultValue = default(long))
        {
            return long.TryParse(this.Value, out var value) == false ? defaultValue : value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as a float.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an float.</returns>
        public float AsFloat(float defaultValue = default(float))
        {
            return float.TryParse(this.Value, out var value) == false ? defaultValue : value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as a boolean.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an boolean.</returns>
        public bool AsBoolean(bool defaultValue = false)
        {
            if (int.TryParse(this.Value, out var value) == false)
            {
                return defaultValue;
            }

            return value != 0;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{this.Name} = {this.Value}";
        }

        /// <summary>
        /// Attempts to load the given filename as a text <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>a <see cref="KeyValue"/> instance if the load was successful, or <c>null</c> on failure.</returns>
        /// <remarks>
        /// This method will swallow any exceptions that occur when reading, use <see cref="ReadAsText"/> if you wish to handle exceptions.
        /// </remarks>
        public static KeyValue LoadAsText(string path)
        {
            return LoadFromFile(path, false);
        }

        /// <summary>
        /// Attempts to load the given filename as a binary <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>a <see cref="KeyValue"/> instance if the load was successful, or <c>null</c> on failure.</returns>
        /// <remarks>
        /// This method will swallow any exceptions that occur when reading, use <see cref="ReadAsBinary"/> if you wish to handle exceptions.
        /// </remarks>
        public static KeyValue LoadAsBinary(string path)
        {
            return LoadFromFile(path, true);
        }

        private static KeyValue LoadFromFile(string path, bool asBinary)
        {
            if (File.Exists(path) == false) { return null; }

            try
            {
                using var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var kv = new KeyValue();

                if (asBinary)
                {
                    if (kv.ReadAsBinary(input) == false) { return null; }
                }
                else
                {
                    if (kv.ReadAsText(input) == false) { return null; }
                }

                return kv;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to create an instance of <see cref="KeyValue"/> from the given input text.
        /// </summary>
        /// <param name="input">The input text to load.</param>
        /// <returns>a <see cref="KeyValue"/> instance if the load was successful, or <c>null</c> on failure.</returns>
        /// <remarks>
        /// This method will swallow any exceptions that occur when reading, use <see cref="ReadAsText"/> if you wish to handle exceptions.
        /// </remarks>
        public static KeyValue LoadFromString(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);

            using var stream = new MemoryStream(bytes);
            var kv = new KeyValue();

            try
            {
                return kv.ReadAsText(stream) == false ? null : kv;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Populate this instance from the given <see cref="Stream"/> as a text <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="input">The input <see cref="Stream"/> to read from.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        public bool ReadAsText(Stream input)
        {
            this.Children = new List<KeyValue>();
            _ = new KeyValueReader(this, input);
            return true;
        }

        /// <summary>
        /// Opens and reads the given filename as text.
        /// </summary>
        /// <seealso cref="ReadAsText"/>
        /// <param name="filename">The file to open and read.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        public bool ReadFileAsText(string filename)
        {
            try
            {
                using var fs = new FileStream(filename, FileMode.Open);
                return ReadAsText(fs);
            }
            catch { return false; }
        }

        internal void RecursiveLoadFromBuffer(KeyValueReader kvr)
        {
            while (true)
            {
                // bool bAccepted = true;

                // get the key name
                var name = kvr.ReadToken(out var wasQuoted, out _);

                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception("RecursiveLoadFromBuffer: got EOF or empty keyname");
                }

                if (name.StartsWith("}") && !wasQuoted) { break; }
                var dat = new KeyValue(name) {Children = new List<KeyValue>()};
                this.Children.Add(dat);
                // get the value
                var value = kvr.ReadToken(out wasQuoted, out var wasConditional);

                if (wasConditional && value != null)
                {
                    // bAccepted = ( value == "[$WIN32]" );
                    value = kvr.ReadToken(out wasQuoted, out wasConditional);
                }

                if (value == null) { throw new Exception("RecursiveLoadFromBuffer:  got NULL key"); }
                if (value.StartsWith("}") && !wasQuoted){ throw new Exception("RecursiveLoadFromBuffer:  got } in key"); }

                if (value.StartsWith("{") && !wasQuoted)
                {
                    dat.RecursiveLoadFromBuffer(kvr);
                }
                else
                {
                    if (wasConditional) { throw new Exception("RecursiveLoadFromBuffer:  got conditional between key and value"); }
                    dat.Value = value;
                }
            }
        }

        /// <summary>
        /// Saves this instance to file.
        /// </summary>
        /// <param name="path">The file path to save to.</param>
        /// <param name="asBinary">If set to <c>true</c>, saves this instance as binary.</param>
        public void SaveToFile(string path, bool asBinary)
        {
            if (asBinary) { throw new NotImplementedException(); }
            using var f = File.Create(path);
            RecursiveSaveToFile(f);
        }

        private void RecursiveSaveToFile(FileStream f, int indentLevel = 0)
        {
            // write header
            WriteIndents(f, indentLevel);
            WriteString(f, Name, true);
            WriteString(f, "\n");
            WriteIndents(f, indentLevel);
            WriteString(f, "{\n");

            // loop through all our keys writing them to disk
            foreach (var child in Children)
            {
                if (child.Value == null)
                {
                    child.RecursiveSaveToFile(f, indentLevel + 1);
                }
                else
                {
                    WriteIndents(f, indentLevel + 1);
                    WriteString(f, child.Name, true);
                    WriteString(f, "\t\t");
                    WriteString(f, child.AsString(), true);
                    WriteString(f, "\n");
                }
            }

            WriteIndents(f, indentLevel);
            WriteString(f, "}\n");
        }

        private static void WriteIndents(FileStream f, int indentLevel)
        {
            WriteString(f, new string('\t', indentLevel));
        }

        private static void WriteString(FileStream f, string str, bool quote = false)
        {
            str = str.Replace(@"\", @"\\");
            var bytes = Encoding.UTF8.GetBytes((quote ? "\"" : "") + str.Replace("\"", "\\\"") + (quote ? "\"" : ""));
            f.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Populate this instance from the given <see cref="Stream"/> as a binary <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="input">The input <see cref="Stream"/> to read from.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        public bool ReadAsBinary(Stream input)
        {
            this.Children = new List<KeyValue>();

            while (true)
            {

                var type = (Type)input.ReadByte();
                if (type == Type.End) { break; }
                var current = new KeyValue {Name = input.ReadNullTermString(Encoding.UTF8)};

                try
                {
                    switch (type)
                    {
                        case Type.None:
                            {
                                current.ReadAsBinary(input);
                                break;
                            }

                        case Type.String:
                            {
                                current.Value = input.ReadNullTermString(Encoding.UTF8);
                                break;
                            }

                        case Type.WideString:
                            {
                                throw new InvalidDataException("wstring is unsupported");
                            }

                        case Type.Int32:
                        case Type.Color:
                        case Type.Pointer:
                            {
                                current.Value = Convert.ToString(input.ReadInt32());
                                break;
                            }

                        case Type.UInt64:
                            {
                                current.Value = Convert.ToString(input.ReadUInt64());
                                break;
                            }

                        case Type.Float32:
                            {
                                current.Value = Convert.ToString(input.ReadFloat(), CultureInfo.InvariantCulture);
                                break;
                            }

                        default:
                            {
                                throw new InvalidDataException("Unknown KV type encountered.");
                            }
                    }
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException($"An exception ocurred while reading KV '{current.Name}'", ex);
                }

                this.Children.Add(current);
            }

            return input.Position == input.Length;
        }
    }

    internal static class StreamHelpers
    {
        private static readonly byte[] Data = new byte[8];

        public static short ReadInt16(this Stream stream)
        {
            stream.Read(Data, 0, 2);
            return BitConverter.ToInt16(Data, 0);
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            stream.Read(Data, 0, 2);
            return BitConverter.ToUInt16(Data, 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            stream.Read(Data, 0, 4);
            return BitConverter.ToInt32(Data, 0);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            stream.Read(Data, 0, 4);
            return BitConverter.ToUInt32(Data, 0);
        }

        public static ulong ReadUInt64(this Stream stream)
        {
            stream.Read(Data, 0, 8);
            return BitConverter.ToUInt64(Data, 0);
        }

        public static float ReadFloat(this Stream stream)
        {
            stream.Read(Data, 0, 4);
            return BitConverter.ToSingle(Data, 0);
        }

        public static string ReadNullTermString(this Stream stream, Encoding encoding)
        {
            var characterSize = encoding.GetByteCount("e");
            using var ms = new MemoryStream();

            while (true)
            {
                var buffer = new byte[characterSize];
                stream.Read(buffer, 0, characterSize);
                if (encoding.GetString(buffer, 0, characterSize) == "\0") { break; }
                ms.Write(buffer, 0, buffer.Length);
            }

            return encoding.GetString(ms.ToArray());
        }

        private static byte[] bufferCache;

        public static byte[] ReadBytesCached(this Stream stream, int len)
        {
            if (bufferCache == null || bufferCache.Length < len) { bufferCache = new byte[len]; }
            stream.Read(bufferCache, 0, len);
            return bufferCache;
        }

        private static readonly byte[] DiscardBuffer = new byte[2 << 12];

        public static void ReadAndDiscard(this Stream stream, int len)
        {
            while (len > DiscardBuffer.Length)
            {
                stream.Read(DiscardBuffer, 0, DiscardBuffer.Length);
                len -= DiscardBuffer.Length;
            }

            stream.Read(DiscardBuffer, 0, len);
        }
    }
}
