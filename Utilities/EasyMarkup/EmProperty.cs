﻿namespace Common.EasyMarkup
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.Assertions;

    public abstract class EmProperty : IEquatable<EmProperty>
    {
        internal const char SpChar_KeyDelimiter = ':';
        internal const char SpChar_ValueDelimiter = ';';
        internal const char SpChar_BeginComplexValue = '(';
        internal const char SpChar_FinishComplexValue = ')';
        internal const char SpChar_ListItemSplitter = ',';
        internal const char SpChar_CommentBlock = '#';
        internal const char SpChar_LiteralStringBlock = '"';
        internal const char SpChar_EscapeChar = '\\';

        protected delegate void OnValueExtracted();
        protected OnValueExtracted OnValueExtractedEvent;

        public string Key { get; protected set; }

        internal string SerializedValue;

        public override string ToString() => $"{this.Key}{SpChar_KeyDelimiter}{EscapeSpecialCharacters(SerializedValue)}{SpChar_ValueDelimiter}";

        public static bool CheckKey(string rawValue, out string foundKey, string keyToValidate) => CheckKey(rawValue, out foundKey) && foundKey == keyToValidate;

        public static bool CheckKey(string rawValue, out string foundKey)
        {
            StringBuffer cleanValue = CleanValue(new StringBuffer(rawValue), true);

            foundKey = cleanValue.ToString();

            return !string.IsNullOrEmpty(foundKey);
        }

        public bool FromString(string rawValue, bool haltOnKeyMismatch = false)
        {
            StringBuffer cleanValue = CleanValue(new StringBuffer(rawValue));

            if (cleanValue.IsEmpty)
                return false;

            string key = ExtractKey(cleanValue);
            if (string.IsNullOrEmpty(this.Key))
                this.Key = key;
            else if (haltOnKeyMismatch && this.Key != key)
                throw new AssertionException($"Key mismatch. Expected:{this.Key} but was {key}.", $"Wrong key found: {this.Key}=/={key}");

            if (cleanValue.Count <= 1) // only enough for the final delimiter
                return true;

            SerializedValue = ExtractValue(cleanValue);
            OnValueExtractedEvent?.Invoke();

            return true;
        }

        protected virtual string ExtractKey(StringBuffer fullString)
        {
            var key = new StringBuffer();
            while (fullString.Count > 0 && fullString.PeekStart() != ':')
                key.PushToEnd(fullString.PopFromStart());

            fullString.PopFromStart(); // Skip : separator

            return key.ToString();
        }

        protected virtual string ExtractValue(StringBuffer fullString) =>
            ReadUntilDelimiter(fullString, SpChar_ValueDelimiter).ToString();

        internal abstract EmProperty Copy();

        public string PrettyPrint()
        {
            string originalValue = ToString();

            if (string.IsNullOrEmpty(originalValue))
                return string.Empty;

            var originalString = new StringBuffer(originalValue);

            var prettyString = new StringBuffer();

            int indentLevel = 0;
            const int indentSize = 4;

            do
            {
                switch (originalString.PeekStart())
                {
                    case SpChar_BeginComplexValue:
                        prettyString.PushToEnd('\r', '\n');
                        prettyString.PushToEnd(' ', indentLevel * indentSize);
                        indentLevel++;
                        prettyString.PushToEnd(originalString.PopFromStart());
                        prettyString.PushToEnd('\r', '\n');
                        prettyString.PushToEnd(' ', indentLevel * indentSize);
                        prettyString.PushToEnd(originalString.PopFromStart());
                        break;
                    case SpChar_ValueDelimiter:
                        prettyString.PushToEnd(originalString.PopFromStart());

                        if (originalString.IsEmpty || originalString.PeekStart() == SpChar_FinishComplexValue)
                            indentLevel--;

                        prettyString.PushToEnd('\r', '\n');
                        prettyString.PushToEnd(' ', indentLevel * indentSize);

                        break;
                    case SpChar_CommentBlock:
                        prettyString.PushToEnd(originalString.PopFromStart());

                        do
                        {
                            prettyString.PushToEnd(originalString.PopFromStart());

                        } while (!originalString.IsEmpty && prettyString.PeekEnd() != SpChar_CommentBlock);

                        prettyString.PushToEnd('\r', '\n');
                        prettyString.PushToEnd(' ', indentLevel * indentSize);
                        break;
                    case SpChar_KeyDelimiter:
                        prettyString.PushToEnd(originalString.PopFromStart());
                        prettyString.PushToEnd(' '); // Add a space after every KeyDelilmiter
                        break;
                    default:
                        prettyString.PushToEnd(originalString.PopFromStart());
                        break;
                }

            } while (!originalString.IsEmpty);

            return prettyString.ToString();
        }

        internal abstract bool ValueEquals(EmProperty other);

        public bool Equals(EmProperty other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (this.GetType() != other.GetType())
                return false;

            return
                this.Key == other.Key &&
                this.ValueEquals(other);
        }

        private static StringBuffer CleanValue(StringBuffer rawValue, bool stopAtKey = false)
        {
            var cleanValue = new StringBuffer();

            while (!rawValue.IsEmpty)
            {
                switch (rawValue.PeekStart())
                {
                    case ' ': // spaces
                        rawValue.TrimStart(' ');
                        break;
                    case '\t': // tabs
                        rawValue.TrimStart('\t');
                        break;
                    case '\r': // line breaks
                    case '\n':
                        rawValue.TrimStart('\r', '\n');
                        break;
                    case SpChar_CommentBlock: // comments
                        rawValue.PopFromStart(); // Pop first #

                        char poppedChar;
                        do
                        {
                            poppedChar = rawValue.PopFromStart();

                        } while (!rawValue.IsEmpty && poppedChar != SpChar_CommentBlock);

                        break;
                    case SpChar_KeyDelimiter when stopAtKey:
                        return cleanValue;
                    case SpChar_LiteralStringBlock:
                        cleanValue.PushToEnd(rawValue.PopFromStart()); // Add first "

                        char popped;
                        do
                        {
                            popped = rawValue.PopFromStart();
                            cleanValue.PushToEnd(popped);
                        } while (popped != SpChar_LiteralStringBlock);

                        break;
                    case SpChar_EscapeChar:
                        cleanValue.PushToEnd(rawValue.PopFromStart()); // Pop escape char
                        cleanValue.PushToEnd(rawValue.PopFromStart()); // Add escaped char
                        break;
                    default:
                        cleanValue.PushToEnd(rawValue.PopFromStart());
                        break;

                }
            }

            return cleanValue;
        }

        internal static string ReadUntilDelimiter(StringBuffer fullString, char delimeter)
            => ReadUntilDelimiter(fullString, new HashSet<char> { delimeter });

        internal static string ReadUntilDelimiter(StringBuffer fullString, ICollection<char> delimeters)
        {
            var value = new StringBuffer();
            char nextChar;

            while (!fullString.IsEmpty && !delimeters.Contains(nextChar = fullString.PeekStart()))
            {
                if (nextChar == SpChar_EscapeChar)
                {
                    fullString.PopFromStart(); // Skip the escape char.
                    value.PushToEnd(fullString.PopFromStart()); // Allow the escaped char into the value.
                }
                else if (nextChar == SpChar_LiteralStringBlock)
                {
                    fullString.PopFromStart(); // Skip the escape char.
                    while (!fullString.IsEmpty && (nextChar = fullString.PopFromStart()) != SpChar_LiteralStringBlock)
                    {
                        value.PushToEnd(nextChar); // Grab everything contained between the " chars except the " chars
                    }
                }
                else
                {
                    value.PushToEnd(fullString.PopFromStart()); // Basic case
                }
            }

            fullString.PopFromStart(); // Skip delimeter

            return value.ToString();
        }

        internal static string EscapeSpecialCharacters(string unescapedValue)
        {
            if (string.IsNullOrEmpty(unescapedValue))
                return string.Empty;

            var original = new StringBuffer(unescapedValue);
            var escaped = new StringBuffer();

            if (original.Contains(' '))
            {
                escaped.PushToEnd(SpChar_LiteralStringBlock);
                escaped.TransferToEnd(original);
                escaped.PushToEnd(SpChar_LiteralStringBlock);
            }
            else
            {
                while (!original.IsEmpty)
                {
                    switch (original.PeekStart())
                    {
                        case SpChar_KeyDelimiter:
                        case SpChar_ValueDelimiter:
                        case SpChar_BeginComplexValue:
                        case SpChar_FinishComplexValue:
                        case SpChar_ListItemSplitter:
                        case SpChar_CommentBlock:
                        case SpChar_EscapeChar:
                            escaped.PushToEnd(SpChar_EscapeChar);
                            break;
                        default:
                            break;
                    }

                    escaped.PushToEnd(original.PopFromStart());
                }
            }

            return escaped.ToString();
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(EmProperty a, EmProperty b)
        {
            if (a is null)
                return b is null;

            return a.Equals(b);
        }

        public static bool operator !=(EmProperty a, EmProperty b)
        {
            if (a is null)
                return !(b is null);

            return !a.Equals(b);
        }
    }

}