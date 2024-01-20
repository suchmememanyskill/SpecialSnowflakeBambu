using System.IO.Compression;
using System.Text;
using System.Xml;

namespace SpecialSnowflakeBambu;

public class ZipManager
{
    public void ConvertInMemory(string filename)
    {
        using (var packageStream = new MemoryStream())
        {
            using (var fs = File.OpenRead(filename))
            {
                fs.CopyTo(packageStream);
            }
            
            InMemoryStrategy(packageStream);

            packageStream.Position = 0;
            using (var fs = File.OpenWrite(filename[..^4] + ".prusa.3mf"))
            {
                packageStream.CopyTo(fs);
            }
        }
    }
    
    public void ConvertExtractZip(string filename)
    {
        using var fs = File.OpenRead(filename);
        UnzipStrategy(fs, filename[..^4] + ".prusa.3mf");
    }
    
    public void InMemoryStrategy(MemoryStream stream)
    {
        stream.Position = 0;

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            if (!zip.Entries.Any(x => x.FullName.StartsWith("3D/Objects")) && !zip.Entries.Any(x => x.FullName == "3D/3dmodel.model"))
                throw new Exception("Not a Bambu 3mf");
            
            var model = zip.Entries.First(x => x.FullName == "3D/3dmodel.model");
            model.Delete();
            model = zip.CreateEntry("3D/3dmodel.model");
            
            using (var modelFile = model.Open())
            {
                using var assembler = new ModelStreamAssembler(modelFile);
                foreach (var file in zip.Entries.Where(x => x.FullName.StartsWith("3D/Objects")))
                {
                    using var modelObjectStream = file.Open();
                    XmlReader reader = XmlReader.Create(modelObjectStream);
                    assembler.IncorporateModelFile(reader);
                }
            }
            
            ZipArchiveEntry? toDelete = null;
            do
            {
                toDelete = zip.Entries.FirstOrDefault(x => x.FullName.StartsWith("3D/Objects"));
                toDelete?.Delete();
            } while (toDelete != null);
        }
    }
    
    public void UnzipStrategy(Stream stream, string destPath)
    {
        stream.Position = 0;

        string path = Path.Join(Directory.GetCurrentDirectory(), "_extract");

        if (Path.Exists(path))
        {
            Directory.Delete(path, true);
        }
        
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            zip.ExtractToDirectory(path);
        }
        
        string objectPath = Path.Join(path, "3D", "Objects");

        using (var modelFile = File.OpenWrite(Path.Join(path, "3D", "3dmodel.model")))
        {
            using var assembler = new ModelStreamAssembler(modelFile);
            foreach (var file in Directory.EnumerateFiles(objectPath))
            {
                using var modelObjectStream = File.OpenRead(file);
                XmlReader reader = XmlReader.Create(modelObjectStream);
                assembler.IncorporateModelFile(reader);
            }
        }

        Directory.Delete(objectPath, true);
        
        if (File.Exists(destPath))
            File.Delete(destPath);
        
        ZipFile.CreateFromDirectory(path, destPath);
    }
}