// See https://aka.ms/new-console-template for more information

using SpecialSnowflakeBambu;
var a = new ZipManager();
if (args.Length >= 1)
{
    a.NewFile(args[0]);
}
else
{
    a.NewFile("test.3mf");
}

