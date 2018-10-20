# Ini
Ini file reader and writer for C# written in pure .NET in a single source file.

Supports bools (case insensitive true/false), integers, doubles, and strings. Strings can have whitespace preserved or not, and can have exterior quotes or not. Defaults to case insensitive string comparisons for keys (section names and variable names), but this can be changed by changing the StringComparer field, or by changing IniFile.DefaultStringComparer for all future instances.

Comments are ignored and not saved. Inline comments at the end of values are not supported. Anything before the first section is also ignored.

Empty sections are not saved unless SaveEmptySections is set to true. Whitespace surrounding section names is removed on save and load. It is not removed while working with an IniFile instance, because this could create undesirable behavior.

Attempting to retrieve missing sections or values through indexing will not throw exceptions.

# Saving
```csharp
var ini = new IniFile();

ini["test1"]["boolA"] = true;
ini["test1"]["boolB"] = false;
ini["test1"]["boolC"] = "TRUE";
ini["test1"]["boolD"] = "FALSE";
ini["test1"]["intA"] = 8;
ini["test1"]["intB"] = "not an int";
ini["test1"]["doubleA"] = 0.238471;
ini["test1"]["doubleB"] = "not a double";
ini["test1"]["doubleC"] = "NaN";

ini["test2"]["str"] = "normal string";
ini["test2"]["quotstr"] = "\"quoted string\"";
ini["test2"]["wstr"] = "    whitespace string    ";
ini["test2"]["wquotstr"] = "      \"      quoted whitespace string     \"     ";

ini.Save("test.ini");
```

# Loading
```csharp
var ini = new IniFile();

ini.Load("test.ini");
Console.WriteLine(ini.GetContents());

Console.WriteLine(ini["test1"]["boolA"].ToBool());
Console.WriteLine(ini["test1"]["boolB"].ToBool());
Console.WriteLine(ini["test1"]["boolC"].ToBool());
Console.WriteLine(ini["test1"]["boolD"].ToBool());
Console.WriteLine(ini["test1"]["intA"].ToInt(-1));
Console.WriteLine(ini["test1"]["intB"].ToInt(-1));
Console.WriteLine(ini["test1"]["doubleA"].ToDouble(-1));
Console.WriteLine(ini["test1"]["doubleB"].ToDouble(-1));
Console.WriteLine(ini["test1"]["doubleC"].ToDouble(-1));

Console.WriteLine(ini["test2"]["str"].GetString());
Console.WriteLine(ini["test2"]["quotstr"].GetString(true, false));
Console.WriteLine(ini["test2"]["quotstr"].GetString(false, false));
Console.WriteLine(ini["test2"]["wstr"].GetString(true));
Console.WriteLine(ini["test2"]["wstr"].GetString(false));
Console.WriteLine(ini["test2"]["wquotstr"].GetString(true, false));
Console.WriteLine(ini["test2"]["wquotstr"].GetString(false, false));
Console.WriteLine(ini["test2"]["wquotstr"].GetString(true, true));
Console.WriteLine(ini["test2"]["wquotstr"].GetString(false, true));
```

If you want to know whether the conversion succeeded instead of supplying a default value for invalid conversions, use TryConvertBool, TryConvertInt, and TryConvertDouble functions.

You can also enumerate sections and values within sections:

```csharp
foreach (var section in ini) {
    Console.WriteLine(string.Format("Section {0} found, with {1} entries", section.Key, section.Value.Count));

    foreach (var item in section.Value) {
        Console.WriteLine(string.Format("Item {0} found, value is {1}", item.Key, item.Value));
    }
}
```

# Ordering

By default sections will not preserve the order of items. In order to preserve the order of items within a section, set the IniSection's Ordered property to true, or specify ordered=true when calling IniFile.Add. Ini files can also be loaded with their order preserved by specifying ordered=true when calling IniFile.Load.

Ordered IniSection instances have the following additional methods available: IndexOf, LastIndexOf, Insert, InsertRange, RemoveAt, RemoveRange, Reverse, GetOrderedValues, and can be indexed using integers. Calling these methods on a non-ordered section will result in an InvalidOperationException being thrown.

Take note that the Keys property of an ordered IniSection will be the keys in order, but the Values property does not have the order guaranteed. To get the in-order values of an IniSection use the GetOrderedValues method, or enumerate the section using foreach.

Both ways of iterating over values below are functionally equivalent for an ordered section.

```csharp
var ini = new IniFile();
ini.Load("somefile.ini", ordered: true);
var section = ini.First().Value;

var index = 0;
foreach (var item in section) {
    Console.WriteLine(string.Format("Item at index {0} is {1}", index, item.Value.GetString()));
    index++;
}

for (var i = 0; i < section.Count; i++) {
    Console.WriteLine(string.Format("Item at index {0} is {1}", i, section[i].GetString()));
}
```
