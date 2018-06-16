﻿namespace CustomCraftSML.Serialization.EasyMarkup
{
    using System;
    using System.Globalization;

    public class EmProperty<T> : EmProperty where T : IConvertible
    {
        protected T ObjValue;

        public T Value
        {
            get => ObjValue;
            set
            {
                ObjValue = value;
                SerializedValue = ObjValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        public EmProperty(string key)
        {
            Key = key;
        }

        public EmProperty(string key, T value) : this(key)
        {
            Value = value;
        }

        protected override string ExtractValue(StringBuffer fullString)
        {
            string serialValue = base.ExtractValue(fullString);

            Value = ConvertFromSerial(serialValue);

            return serialValue;
        }

        public T ConvertFromSerial(string value)
        {
            var type = typeof(T);

            if (type.IsEnum)
                return (T)Enum.Parse(type, value);
            else
                return (T)Convert.ChangeType(value, typeof(T));
        }

        internal override EmProperty Copy() => new EmProperty<T>(Key);
    }

}

