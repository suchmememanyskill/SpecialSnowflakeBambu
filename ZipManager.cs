using System.IO.Compression;
using System.Text;
using System.Xml;

namespace SpecialSnowflakeBambu;

public class ZipManager
{
    public void NewFile(string filename)
    {
        using (var packageStream = new MemoryStream())
        {
            using (var fs = File.OpenRead(filename))
            {
                fs.CopyTo(packageStream);
            }
            
            EditInPlace2(packageStream);

            packageStream.Position = 0;
            using (var fs = File.OpenWrite(filename[..^4] + ".prusa.3mf"))
            {
                packageStream.CopyTo(fs);
            }
        }
    }
    
    public void NewFile2(string filename)
    {
        using var fs = File.OpenRead(filename);
        Unzip2(fs, filename[..^4] + ".prusa.3mf");
    }
    
    public void EditInPlace(MemoryStream stream)
    {
        stream.Position = 0;

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            if (!zip.Entries.Any(x => x.FullName.StartsWith("3D/Objects")) && !zip.Entries.Any(x => x.FullName == "3D/3dmodel.model"))
                throw new Exception("Not a Bambu 3mf");

            ModelAssembler assembler = new();
            

            foreach (var modelObject in zip.Entries.Where(x => x.FullName.StartsWith("3D/Objects")))
            {
                using var modelObjectStream = modelObject.Open();
                using ModelRipper ripper = new();
                var data = ripper.Rip(modelObjectStream);
                if (data != null)
                    assembler.AddMesh(data);
            }

            
            ZipArchiveEntry? toDelete = null;
            do
            {
                toDelete = zip.Entries.FirstOrDefault(x => x.FullName.StartsWith("3D/Objects"));
                toDelete?.Delete();
            } while (toDelete != null);
            
            
            var model = zip.Entries.First(x => x.FullName == "3D/3dmodel.model");
            model.Delete();
            model = zip.CreateEntry("3D/3dmodel.model");
            
            
            using (var modelStream = model.Open())
            {
                assembler.Write(modelStream);
            }
            
            foreach (var zipArchiveEntry in zip.Entries)
            {
                Console.WriteLine(zipArchiveEntry.FullName);
            }
        }
    }
    
    public void EditInPlace2(MemoryStream stream)
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
    
    public void Unzip(Stream stream, string destPath)
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
        
        var assembler = new ModelAssembler();
        string objectPath = Path.Join(path, "3D", "Objects");
        
        foreach (var file in Directory.EnumerateFiles(objectPath))
        {
            using var modelObjectStream = File.OpenRead(file);
            using ModelRipper ripper = new();
            var data = ripper.Rip(modelObjectStream);
            if (data != null)
                assembler.AddMesh(data);
        }
        
        Directory.Delete(objectPath, true);

        using (var modelFile = File.OpenWrite(Path.Join(path, "3D", "3dmodel.model")))
        {
            assembler.Write(modelFile);
        }
        
        if (File.Exists(destPath))
            File.Delete(destPath);
        
        ZipFile.CreateFromDirectory(path, destPath);
    }
    
    public void Unzip2(Stream stream, string destPath)
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