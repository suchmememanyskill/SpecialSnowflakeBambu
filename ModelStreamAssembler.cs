using System.Xml;

namespace SpecialSnowflakeBambu;

public class ModelStreamAssembler : IDisposable
{
    private XmlWriter _writer;
    private int _meshCount = 0;

    public ModelStreamAssembler(Stream dest)
    {
        _writer = XmlWriter.Create(dest);
        Start();
    }

    private void Start()
    {
        _writer.WriteStartDocument();
        _writer.WriteStartElement("model", "http://schemas.microsoft.com/3dmanufacturing/core/2015/02");
        _writer.WriteAttributeString("unit", "millimeter");
        _writer.WriteAttributeString("xml", "lang", null, "en-US");
        _writer.WriteAttributeString("xmlns", "slic3rpe", null,"http://schemas.slic3r.org/3mf/2017/06");
        _writer.WriteStartElement("resources");
    }

    public void IncorporateModelFile(XmlReader reader)
    {
        bool createdStartElements = false;
        
        while (!reader.EOF)
        {
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "mesh")
                    break;

            if (reader.EOF)
                break;

            if (!createdStartElements)
            {
                createdStartElements = true;
                _meshCount++;
                _writer.WriteStartElement("object");
                _writer.WriteAttributeString("id", _meshCount.ToString());
                _writer.WriteAttributeString("type", "model");
            }

            bool endMesh = false;
            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        _writer.WriteStartElement(reader.Name);
                        bool empty = reader.IsEmptyElement;
                        
                        for (int i = 0; i < reader.AttributeCount; i++) 
                        {
                            reader.MoveToAttribute(i); 
                            _writer.WriteAttributeString(reader.Name, reader.Value);
                        }

                        if (empty)
                        {
                            _writer.WriteEndElement();
                        }
                        
                        break;
                    case XmlNodeType.Text:
                        _writer.WriteString(reader.Value);
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        _writer.WriteProcessingInstruction(reader.Name, reader.Value);
                        break;
                    case XmlNodeType.Comment:
                        _writer.WriteComment(reader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        _writer.WriteFullEndElement();

                        if (reader.Name == "mesh")
                            endMesh = true;
                        
                        break;
                    case XmlNodeType.Whitespace:
                        _writer.WriteRaw(reader.Value);
                        break;
                }
            } while (!endMesh && reader.Read());
        }
        
        if (createdStartElements)
            _writer.WriteFullEndElement();
    }
    
    public void Dispose()
    {
        _writer.WriteFullEndElement();
        _writer.WriteStartElement("build");
        for (int i = 1; i <= _meshCount; i++)
        {
            _writer.WriteStartElement("item");
            _writer.WriteAttributeString("objectid", i.ToString());
            _writer.WriteAttributeString("printable", "1");
            _writer.WriteAttributeString("transform", "1 0 0 0 1 0 0 0 1 115 112.5 10");
            _writer.WriteEndElement();
        }
        _writer.WriteFullEndElement(); // build
        _writer.WriteFullEndElement(); // model
        _writer.Dispose();
    }
}