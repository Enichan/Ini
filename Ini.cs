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
            Value = value != null ? value.ToString() : null;
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

    private static readonly IniValue _default = new IniValue();
    public static IniValue Default { get { return _default; } }
}

public class IniFile : IEnumerable<KeyValuePair<string, IniSection>>, IDictionary<string, IniSection> {
    private Dictionary<string, IniSection> sections;
    public IEqualityComparer<string> StringComparer;

    public bool SaveEmptySections;

    public IniFile() 
        : this(DefaultComparer) {
    }

    public IniFile(IEqualityComparer<string> stringComparer) {
        StringComparer = stringComparer;
        sections = new Dictionary<string, IniSection>(StringComparer);
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
            if (section.Value.Count > 0 || SaveEmptySections) {
                writer.WriteLine(string.Format("[{0}]", section.Key.Trim()));
                foreach (var kvp in section.Value) {
                    writer.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                }
                writer.WriteLine("");
            }
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
        IniSection section = null;

        while (!reader.EndOfStream) {
            var line = reader.ReadLine();

            if (line != null) {
                var trimStart = line.TrimStart();

                if (trimStart.Length > 0) {
                    if (trimStart[0] == '[') {
                        var sectionEnd = trimStart.IndexOf(']');
                        if (sectionEnd > 0) {
                            var sectionName = trimStart.Substring(1, sectionEnd - 1).Trim();
                            section = new IniSection(StringComparer);
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

    public bool TryGetSection(string section, out IniSection result) {
        return sections.TryGetValue(section, out result);
    }

    bool IDictionary<string, IniSection>.TryGetValue(string key, out IniSection value) {
        return TryGetSection(key, out value);
    }

    public bool Remove(string section) {
        return sections.Remove(section);
    }

    public void Add(string section, Dictionary<string, IniValue> values) {
        Add(section, new IniSection(values, StringComparer));
    }

    public void Add(string section, IniSection value) {
        if (value.Comparer != StringComparer) {
            value = new IniSection(value, StringComparer);
        }
        sections.Add(section, value);
    }

    public void Add(string section) {
        sections.Add(section, new IniSection(StringComparer));
    }

    bool IDictionary<string, IniSection>.ContainsKey(string key) {
        return ContainsSection(key);
    }

    public ICollection<string> Keys {
        get { return sections.Keys; }
    }

    public ICollection<IniSection> Values {
        get { return sections.Values; }
    }

    void ICollection<KeyValuePair<string, IniSection>>.Add(KeyValuePair<string, IniSection> item) {
        ((IDictionary<string, IniSection>)sections).Add(item);
    }

    public void Clear() {
        sections.Clear();
    }

    bool ICollection<KeyValuePair<string, IniSection>>.Contains(KeyValuePair<string, IniSection> item) {
        return ((IDictionary<string, IniSection>)sections).Contains(item);
    }

    void ICollection<KeyValuePair<string, IniSection>>.CopyTo(KeyValuePair<string, IniSection>[] array, int arrayIndex) {
        ((IDictionary<string, IniSection>)sections).CopyTo(array, arrayIndex);
    }

    public int Count {
        get { return sections.Count; }
    }

    bool ICollection<KeyValuePair<string, IniSection>>.IsReadOnly {
        get { return ((IDictionary<string, IniSection>)sections).IsReadOnly; }
    }

    bool ICollection<KeyValuePair<string, IniSection>>.Remove(KeyValuePair<string, IniSection> item) {
        return ((IDictionary<string, IniSection>)sections).Remove(item);
    }

    public IEnumerator<KeyValuePair<string, IniSection>> GetEnumerator() {
        return sections.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IniSection this[string section] {
        get {
            IniSection s;
            if (sections.TryGetValue(section, out s)) {
                return s;
            }
            s = new IniSection(StringComparer);
            sections[section] = s;
            return s;
        }
        set {
            var v = value;
            if (v.Comparer != StringComparer) {
                v = new IniSection(v, StringComparer);
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
            return obj.ToLowerInvariant().GetHashCode();
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
            if (obj is string) {
                return GetHashCode((string)obj);
            }
            return obj.ToStringInvariant().ToLowerInvariant().GetHashCode();
        }
#endif
    }
}

public class IniSection : IEnumerable<KeyValuePair<string, IniValue>>, IDictionary<string, IniValue> {
    private Dictionary<string, IniValue> values;

    public IniSection()
        : this(IniFile.DefaultComparer) {
    }

    public IniSection(IEqualityComparer<string> stringComparer) {
        this.values = new Dictionary<string, IniValue>(stringComparer);
    }

    public IniSection(Dictionary<string, IniValue> values)
        : this(values, IniFile.DefaultComparer) {
    }

    public IniSection(Dictionary<string, IniValue> values, IEqualityComparer<string> stringComparer) {
        this.values = new Dictionary<string, IniValue>(values, stringComparer);
    }

    public IniSection(IniSection values)
        : this(values, IniFile.DefaultComparer) {
    }

    public IniSection(IniSection values, IEqualityComparer<string> stringComparer) {
        this.values = new Dictionary<string, IniValue>(values.values, stringComparer);
    }

    public void Add(string key, IniValue value) {
        values.Add(key, value);
    }

    public bool ContainsKey(string key) {
        return values.ContainsKey(key);
    }

    public ICollection<string> Keys {
        get { return values.Keys; }
    }

    public bool Remove(string key) {
        return values.Remove(key);
    }

    public bool TryGetValue(string key, out IniValue value) {
        return values.TryGetValue(key, out value);
    }

    public ICollection<IniValue> Values {
        get { return values.Values; }
    }

    void ICollection<KeyValuePair<string, IniValue>>.Add(KeyValuePair<string, IniValue> item) {
        ((IDictionary<string, IniValue>)values).Add(item);
    }

    public void Clear() {
        values.Clear();
    }

    bool ICollection<KeyValuePair<string, IniValue>>.Contains(KeyValuePair<string, IniValue> item) {
        return ((IDictionary<string, IniValue>)values).Contains(item);
    }

    void ICollection<KeyValuePair<string, IniValue>>.CopyTo(KeyValuePair<string, IniValue>[] array, int arrayIndex) {
        ((IDictionary<string, IniValue>)values).CopyTo(array, arrayIndex);
    }

    public int Count {
        get { return values.Count; }
    }

    bool ICollection<KeyValuePair<string, IniValue>>.IsReadOnly {
        get { return ((IDictionary<string, IniValue>)values).IsReadOnly; }
    }

    bool ICollection<KeyValuePair<string, IniValue>>.Remove(KeyValuePair<string, IniValue> item) {
        return ((IDictionary<string, IniValue>)values).Remove(item);
    }

    public IEnumerator<KeyValuePair<string, IniValue>> GetEnumerator() {
        return values.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IEqualityComparer<string> Comparer { get { return values.Comparer; } }

    public IniValue this[string name] {
        get {
            IniValue val;
            if (values.TryGetValue(name, out val)) {
                return val;
            }
            return IniValue.Default;
        }
        set {
            values[name] = value;
        }
    }

    public static implicit operator IniSection(Dictionary<string, IniValue> dict) {
        return new IniSection(dict);
    }

    public static explicit operator Dictionary<string, IniValue>(IniSection section) {
        return section.values;
    }
}
