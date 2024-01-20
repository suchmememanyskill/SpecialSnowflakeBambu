// See https://aka.ms/new-console-template for more information

using SpecialSnowflakeBambu;
var a = new ZipManager();
if (args.Length >= 1)
{
    foreach (var s in args)
    {
        a.ConvertInMemory(s);
    }
}
else
{
    Console.WriteLine("Please provide a file");
}