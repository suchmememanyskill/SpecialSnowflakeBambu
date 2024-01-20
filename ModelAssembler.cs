using System.Xml;

namespace SpecialSnowflakeBambu;

public class ModelAssembler
{
    private XmlDocument _root;
    private XmlElement _resources;
    private XmlElement _build;
    private int _objCount = 0;

    private readonly Dictionary<string, string> _modelInitData =
    new() {
        { "unit", "millimeter" },
        { "xml:lang", "en-US" },
        { "xmlns", "http://schemas.microsoft.com/3dmanufacturing/core/2015/02"},
        { "xmlns:slic3rpe", "http://schemas.slic3r.org/3mf/2017/06"}
    };

    public ModelAssembler()
    {
        _root = new();
        var decl = _root.CreateXmlDeclaration("1.0", "UTF-8", null);
        _root.AppendChild(decl);
        var model = _root.CreateElement("model", "http://schemas.microsoft.com/3dmanufacturing/core/2015/02");
        _root.AppendChild(model);

        AddDictToElement(model, _modelInitData);

        _resources = _root.CreateElement("resources");
        model.AppendChild(_resources);

        _build = _root.CreateElement("build");
        model.AppendChild(_build);
    }

    public void AddMesh(List<XmlNode> meshes)
    {
        if (meshes.Count <= 0)
            return;
        
        var obj = _root.CreateElement("object");
        _objCount++;
        AddDictToElement(obj, new()
        {
            {"id", _objCount.ToString()},
            {"type", "model"}
        });
        
        meshes.ForEach(x =>
        {
            var import = _root!.ImportNode(x, true);
            obj.AppendChild(import);
        });

        var item = _root.CreateElement("item");
        AddDictToElement(item, new()
        {
            {"objectid", _objCount.ToString()},
            {"transform", "1 0 0 0 1 0 0 0 1 47.0249977 44.3248482 -10.5"},
            {"printable", "1"}
        });

        _resources.AppendChild(obj);
        _build.AppendChild(item);
    }

    public void Write(Stream output)
    {
        var writer = XmlWriter.Create(output);
        _root.WriteTo(writer);
    }

    private void AddDictToElement(XmlElement element, Dictionary<string, string> data)
    {
        foreach (var x in data)
        {
            var attr = _root.CreateAttribute(x.Key);
            attr.Value = x.Value;
            element.Attributes.Append(attr);
        }
    }
}