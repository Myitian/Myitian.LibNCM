# Myitian.LibNCM
[![NuGet version (Myitian.LibNCM)](https://img.shields.io/badge/nuget-Myitian.LibNCM-6cf)](https://www.nuget.org/packages/Myitian.LibNCM)

A library to read and write Netease Cloud Music file.

## Supported Runtimes
.NET Framework 4 or above

.NET Standard 2.0 or above

## Example
```csharp
using Myitian.LibNCM;

NCM ncm1 = NCM.ReadFile("example.ncm"); // Reads the NCM file
ncm1.WriteFile("example-output.ncm"); // Supports exporting the NCM file

using (FileStream fs = new FileStream("example.ncm", FileMode.Open))
{
    NCM ncm2 = NCM.ReadStream(fs); // Reads the NCM file from a stream
    // Supports modifying file content, such as RC4 key, cover, metadata and music data.
    ncm2.RC4Key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
    ncm2.ImageCover = File.ReadAllBytes("cover.png");
    ncm2.Metadata = new NCMMetadata()
    {
        MusicName = "Example Song",
        Artist = new List<List<dynamic>> { new List<dynamic> { "Example Artist", 0 } },
        Album = "Example Album"
    };
    using (MemoryStream ms = new MemoryStream())
    {
        ncm2.WriteStream(ms); // Write the NCM file to a stream
    }
}

NCM ncm3 = new NCM() // Create a NCM file
{
    RC4Key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 },
    MusicData = File.ReadAllBytes("music.mp3"),
    Metadata = new NCMMetadata()
    {
        MusicName = "Example Song",
        Artist = new List<List<dynamic>> { new List<dynamic> { "Example Artist", 0 } },
        Album = "Example Album",
        Format = "mp3"
    }
};
Console.WriteLine(ncm3.ToString()); // Prints the information of the NCM file
```

## References
[网易云音乐ncm格式分析以及ncm与mp3格式转换](https://www.cnblogs.com/cyx-b/p/13443003.html)

[ncmdumpGUI](https://github.com/kpali/ncmdumpGUI)