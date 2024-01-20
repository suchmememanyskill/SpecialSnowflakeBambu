using System.Xml;
using System.Linq;

namespace SpecialSnowflakeBambu;

public class ModelRipper : IDisposable
{
    private XmlDocument _doc = new();
    private XmlReader _reader = null;
    
    public List<XmlNode>? Rip(Stream xmlData)
    {
        try
        {
            _reader = XmlReader.Create(xmlData);
            _reader.MoveToElement();
            _reader.ReadToNextSibling("model");
            XmlNode node = _doc.ReadNode(_reader)!;
            
            List<XmlNode> nodes = new();
            var obj = node["resources"]!["object"]!;
            for (int i = 0; i < obj.ChildNodes.Count; i++)
            {
                if (obj.ChildNodes[i]?.Name == "mesh")
                    nodes.Add(obj.ChildNodes[i]!);
            }

            return nodes;
        }
        catch
        {
            return null;
        }
    }


    public void Dispose()
    {
        _reader?.Dispose();
    }
}