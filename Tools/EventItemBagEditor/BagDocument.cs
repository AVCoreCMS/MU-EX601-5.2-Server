using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace EventItemBagEditor;

internal sealed class BagDocument
{
    public string FilePath { get; private set; }
    public XDocument Document { get; private set; }
    public XElement Root => Document.Root ?? throw new InvalidDataException("XML without root element.");
    public bool IsExtended => string.Equals((string?)Root.Attribute("UseEx"), "1", StringComparison.OrdinalIgnoreCase) || Root.Element("Ex") is not null;

    private BagDocument(string filePath, XDocument document)
    {
        FilePath = filePath;
        Document = document;
    }

    public static BagDocument Load(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        if (doc.Root?.Name.LocalName != "ItemBag")
            throw new InvalidDataException("The selected XML is not an EventItemBag file (root <ItemBag> expected).");
        return new BagDocument(path, doc);
    }

    public Dictionary<string, string> GetConfig()
    {
        var config = Root.Element("Config");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (config is null) return result;
        foreach (var attr in config.Attributes()) result[attr.Name.LocalName] = attr.Value;
        return result;
    }

    public void SetConfig(IReadOnlyDictionary<string, string> values)
    {
        var config = Root.Element("Config");
        if (config is null)
        {
            config = new XElement("Config");
            Root.AddFirst(config);
        }
        config.RemoveAttributes();
        foreach (var pair in values.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
            config.SetAttributeValue(pair.Key.Trim(), pair.Value?.Trim() ?? string.Empty);
    }

    public List<Dictionary<string, string>> GetBasicItems()
    {
        return Root.Elements("Item").Select(ToDictionary).ToList();
    }

    public void SetBasicItems(IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        Root.SetAttributeValue("UseEx", "0");
        Root.Element("Ex")?.Remove();
        Root.Elements("Item").Remove();
        foreach (var row in rows)
        {
            var element = new XElement("Item");
            SetAttributes(element, row);
            Root.Add(element);
        }
    }

    public List<Dictionary<string, string>> GetExtendedDrops()
    {
        var rows = new List<Dictionary<string, string>>();
        var ex = Root.Element("Ex");
        if (ex is null) return rows;

        foreach (var drop in ex.Elements("Drop"))
        {
            foreach (var cls in drop.Elements("Class"))
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DropIndex"] = Attr(drop, "Index"),
                    ["DropRate"] = Attr(drop, "DropRate")
                };
                foreach (var a in cls.Attributes()) row[a.Name.LocalName] = a.Value;
                rows.Add(row);
            }
        }
        return rows;
    }

    public List<Dictionary<string, string>> GetExtendedPoolItems()
    {
        var rows = new List<Dictionary<string, string>>();
        var ex = Root.Element("Ex");
        if (ex is null) return rows;

        foreach (var pool in ex.Elements("Pool"))
        {
            foreach (var item in pool.Elements("Item"))
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Section"] = Attr(pool, "Section")
                };
                foreach (var a in item.Attributes()) row[a.Name.LocalName] = a.Value;
                rows.Add(row);
            }
        }
        return rows;
    }

    public void SetExtended(
        IEnumerable<IReadOnlyDictionary<string, string>> dropRows,
        IEnumerable<IReadOnlyDictionary<string, string>> poolRows)
    {
        Root.SetAttributeValue("UseEx", "1");
        Root.Elements("Item").Remove();
        Root.Element("Ex")?.Remove();

        var ex = new XElement("Ex");
        Root.Add(ex);

        foreach (var group in dropRows.GroupBy(r => $"{Value(r, "DropIndex")}|{Value(r, "DropRate")}"))
        {
            var first = group.First();
            var drop = new XElement("Drop");
            drop.SetAttributeValue("Index", Value(first, "DropIndex"));
            drop.SetAttributeValue("DropRate", Value(first, "DropRate"));
            foreach (var row in group)
            {
                var cls = new XElement("Class");
                foreach (var pair in row)
                {
                    if (pair.Key.Equals("DropIndex", StringComparison.OrdinalIgnoreCase) ||
                        pair.Key.Equals("DropRate", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(pair.Value)) cls.SetAttributeValue(pair.Key, pair.Value.Trim());
                }
                drop.Add(cls);
            }
            ex.Add(drop);
        }

        foreach (var group in poolRows.GroupBy(r => Value(r, "Section")))
        {
            var pool = new XElement("Pool");
            pool.SetAttributeValue("Section", group.Key);
            foreach (var row in group)
            {
                var item = new XElement("Item");
                foreach (var pair in row)
                {
                    if (pair.Key.Equals("Section", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(pair.Value)) item.SetAttributeValue(pair.Key, pair.Value.Trim());
                }
                pool.Add(item);
            }
            ex.Add(pool);
        }
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Root.Name.LocalName != "ItemBag") errors.Add("Root element must be <ItemBag>.");

        var config = Root.Element("Config");
        if (config is not null)
        {
            CheckRange(config, "ItemDropRate", 0, 100, errors, "Config");
            CheckRange(config, "SetItemDropRate", 0, 10000, errors, "Config");
            CheckMin(config, "ItemDropCount", 0, errors, "Config");
        }

        if (!IsExtended)
        {
            var i = 0;
            foreach (var item in Root.Elements("Item"))
            {
                i++;
                CheckRequiredInt(item, "Type", errors, $"Item #{i}");
                CheckRequiredInt(item, "Index", errors, $"Item #{i}");
                CheckMin(item, "DropRate", 0, errors, $"Item #{i}");
                CheckRange(item, "SocketOption", 0, 5, errors, $"Item #{i}", optional: true);
            }
        }
        else
        {
            var ex = Root.Element("Ex");
            if (ex is null)
            {
                errors.Add("UseEx=1 but <Ex> is missing.");
                return errors;
            }

            var sections = new HashSet<string>(ex.Elements("Pool").Select(p => Attr(p, "Section")));
            foreach (var drop in ex.Elements("Drop"))
            {
                CheckRange(drop, "DropRate", 0, 10000, errors, $"Drop {Attr(drop, "Index")}");
                foreach (var cls in drop.Elements("Class"))
                {
                    var section = Attr(cls, "Section");
                    if (!string.IsNullOrWhiteSpace(section) && !sections.Contains(section) && ParseLong(Attr(cls, "Money")) <= 0)
                        errors.Add($"Drop {Attr(drop, "Index")}: Section {section} has no matching Pool and Money is 0.");
                }
            }
        }

        return errors;
    }

    public void Save(string? rawXml = null)
    {
        var backup = FilePath + ".bak";
        File.Copy(FilePath, backup, true);

        if (!string.IsNullOrWhiteSpace(rawXml))
        {
            var parsed = XDocument.Parse(rawXml, LoadOptions.PreserveWhitespace);
            if (parsed.Root?.Name.LocalName != "ItemBag") throw new InvalidDataException("Raw XML must have <ItemBag> root.");
            Document = parsed;
        }

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            Encoding = new System.Text.UTF8Encoding(false),
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace
        };
        using var writer = XmlWriter.Create(FilePath, settings);
        Document.Save(writer);
    }

    public string ToXmlString()
    {
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        using var writer = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true, IndentChars = "\t", OmitXmlDeclaration = false });
        Document.Save(writer);
        writer.Flush();
        return sw.ToString();
    }

    private static Dictionary<string, string> ToDictionary(XElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attr in element.Attributes()) result[attr.Name.LocalName] = attr.Value;
        return result;
    }

    private static void SetAttributes(XElement element, IReadOnlyDictionary<string, string> values)
    {
        foreach (var pair in values)
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                element.SetAttributeValue(pair.Key.Trim(), pair.Value.Trim());
    }

    private static string Value(IReadOnlyDictionary<string, string> row, string key)
        => row.TryGetValue(key, out var value) ? value?.Trim() ?? string.Empty : string.Empty;

    private static string Attr(XElement e, string name) => (string?)e.Attribute(name) ?? string.Empty;
    private static long ParseLong(string value) => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static void CheckRequiredInt(XElement e, string attr, List<string> errors, string where)
    {
        var value = Attr(e, attr);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) errors.Add($"{where}: {attr} must be an integer.");
    }

    private static void CheckMin(XElement e, string attr, long min, List<string> errors, string where)
    {
        var value = Attr(e, attr);
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < min)
            errors.Add($"{where}: {attr} must be >= {min}.");
    }

    private static void CheckRange(XElement e, string attr, long min, long max, List<string> errors, string where, bool optional = false)
    {
        var value = Attr(e, attr);
        if (optional && string.IsNullOrWhiteSpace(value)) return;
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < min || n > max)
            errors.Add($"{where}: {attr} must be between {min} and {max}.");
    }
}
