using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct IniValue {
    private static bool TryParseInt(string text, out int value) {
        int res;
        if (Int32.TryParse(text,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out res)) {
            value = res;
            return true;
        }
        value = 0;
        return false;
    }

    private static bool TryParseDouble(string text, out double value) {
        double res;
        if (Double.TryParse(text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out res)) {
            value = res;
            return true;
        }
        value = Double.NaN;
        return false;
    }

    public string Value;

    public IniValue(object value) {
        var formattable = value as IFormattable;
        if (formattable != null) {
            Value = formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        }
        else {
            Value = value.ToString();
        }
    }

    public IniValue(string value) {
        Value = value;
    }

    public bool ToBool(bool valueIfInvalid = false) {
        bool res;
        if (TryConvertBool(out res)) {
            return res;
        }
        return valueIfInvalid;
    }

    public bool TryConvertBool(out bool result) {
        if (Value == null) {
            result = default(bool);
            return false;
        }
        var boolStr = Value.Trim().ToLowerInvariant();
        if (boolStr == "true") {
            result = true;
            return true;
        }
        else if (boolStr == "false") {
            result = false;
            return true;
        }
        result = default(bool);
        return false;
    }

    public int ToInt(int valueIfInvalid = 0) {
        int res;
        if (TryConvertInt(out res)) {
            return res;
        }
        return valueIfInvalid;
    }

    public bool TryConvertInt(out int result) {
        if (Value == null) {
            result = default(int);
            return false;
        }
        if (TryParseInt(Value.Trim(), out result)) {
            return true;
        }
        return false;
    }

    public double ToDouble(double valueIfInvalid = 0) {
        double res;
        if (TryConvertDouble(out res)) {
            return res;
        }
        return valueIfInvalid;
    }

    public bool TryConvertDouble(out double result) {
        if (Value == null) {
            result = default(double);
            return false; ;
        }
        if (TryParseDouble(Value.Trim(), out result)) {
            return true;
        }
        return false;
    }

    public string GetString() {
        return GetString(true, false);
    }

    public string GetString(bool preserveWhitespace) {
        return GetString(true, preserveWhitespace);
    }

    public string GetString(bool allowOuterQuotes, bool preserveWhitespace) {
        if (Value == null) {
            return "";
        }
        var trimmed = Value.Trim();
        if (allowOuterQuotes && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"') {
            var inner = trimmed.Substring(1, trimmed.Length - 2);
            return preserveWhitespace ? inner : inner.Trim();
        }
        else {
            return preserveWhitespace ? Value : Value.Trim();
        }
    }

    public override string ToString() {
        return Value;
    }

    public static implicit operator IniValue(byte o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(short o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(int o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(sbyte o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(ushort o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(uint o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(float o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(double o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(bool o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(string o) {
        return new IniValue(o);
    }
}

public class IniFile : IEnumerable<KeyValuePair<string, Dictionary<string, IniValue>>> {
    private Dictionary<string, Dictionary<string, IniValue>> sections;
    public IEqualityComparer<string> StringComparer;

    public IniFile() 
        : this(DefaultComparer) {
    }

    public IniFile(IEqualityComparer<string> stringComparer) {
        StringComparer = stringComparer;
        sections = new Dictionary<string, Dictionary<string, IniValue>>(StringComparer);
    }

    public void Save(string path, FileMode mode = FileMode.Create) {
        using (var stream = new FileStream(path, mode, FileAccess.Write)) {
            Save(stream);
        }
    }

    public void Save(Stream stream) {
        using (var writer = new StreamWriter(stream)) {
            Save(writer);
        }
    }

    public void Save(StreamWriter writer) {
        foreach (var section in sections) {
            writer.WriteLine(string.Format("[{0}]", section.Key));
            foreach (var kvp in section.Value) {
                writer.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
            }
            writer.WriteLine("");
        }
    }

    public void Load(string path) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            Load(stream);
        }
    }

    public void Load(Stream stream) {
        using (var reader = new StreamReader(stream)) {
            Load(reader);
        }
    }

    public void Load(StreamReader reader) {
        Dictionary<string, IniValue> section = null;

        while (!reader.EndOfStream) {
            var line = reader.ReadLine();

            if (line != null) {
                var trimStart = line.TrimStart();

                if (trimStart.Length > 0) {
                    if (trimStart[0] == '[') {
                        var sectionEnd = trimStart.IndexOf(']');
                        if (sectionEnd > 0) {
                            var sectionName = trimStart.Substring(1, sectionEnd - 1).Trim();
                            section = new Dictionary<string, IniValue>(StringComparer); ;
                            sections[sectionName] = section;
                        }
                    }
                    else if (section != null && trimStart[0] != ';') {
                        string key;
                        IniValue val;

                        if (LoadValue(line, out key, out val)) {
                            section[key] = val;
                        }
                    }
                }
            }
        }
    }

    private bool LoadValue(string line, out string key, out IniValue val) {
        var assignIndex = line.IndexOf('=');
        if (assignIndex <= 0) {
            key = null;
            val = null;
            return false;
        }

        key = line.Substring(0, assignIndex).Trim();
        var value = line.Substring(assignIndex + 1);

        val = new IniValue(value);
        return true;
    }

    public bool ContainsSection(string section) {
        return sections.ContainsKey(section);
    }

    public bool TryGetSection(string section, out Dictionary<string, IniValue> result) {
        return sections.TryGetValue(section, out result);
    }

    public bool Remove(string section) {
        return sections.Remove(section);
    }

    public void Add(string section, Dictionary<string, IniValue> value) {
        if (value.Comparer != StringComparer) {
            value = new Dictionary<string, IniValue>(value, StringComparer);
        }
        sections.Add(section, value);
    }

    public void Add(string section) {
        sections.Add(section, new Dictionary<string, IniValue>(StringComparer));
    }

    public IEnumerator<KeyValuePair<string, Dictionary<string, IniValue>>> GetEnumerator() {
        return sections.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public Dictionary<string, IniValue> this[string section] {
        get {
            return sections[section];
        }
        set {
            var v = value;
            if (v.Comparer != StringComparer) {
                v = new Dictionary<string, IniValue>(v, StringComparer);
            }
            sections[section] = v;
        }
    }

    public string GetContents() {
        using (var stream = new MemoryStream()) {
            Save(stream);
            stream.Flush();
            var builder = new StringBuilder(Encoding.UTF8.GetString(stream.ToArray()));
            return builder.ToString();
        }
    }

    public static IEqualityComparer<string> DefaultComparer = new CaseInsensitiveStringComparer();

    class CaseInsensitiveStringComparer : IEqualityComparer<string> {
        public bool Equals(string x, string y) {
            return String.Compare(x, y, true) == 0;
        }

        public int GetHashCode(string obj) {
            return obj.GetHashCode();
        }

#if JS
            public new bool Equals(object x, object y) {
                var xs = x as string;
                var ys = y as string;
                if (x == null || y == null) {
                    return x == null && y == null;
                }
                return Equals(xs, ys);
            }

            public int GetHashCode(object obj) {
                return obj.GetHashCode();
            }
#endif
    }
}