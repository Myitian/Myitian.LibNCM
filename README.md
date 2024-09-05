# Myitian.LibNCM
[![NuGet version (Myitian.LibNCM)](https://img.shields.io/nuget/v/Myitian.LibNCM?color=6cf&style=for-the-badge)](https://www.nuget.org/packages/Myitian.LibNCM)

一个读写网易云音乐 NCM 文件的库。

## 依赖
### .NET 6/7/8
无额外依赖
### .NET Standard 2.1
[System.Text.Json](https://www.nuget.org/packages/System.Text.Json)
### .NET Standard 2.0
[System.Text.Json](https://www.nuget.org/packages/System.Text.Json)\
[IndexRange](https://www.nuget.org/packages/IndexRange)

## 例子
```csharp
using Myitian.LibNCM;

NCM ncm1 = NCM.Create("example.ncm"); // 支持读取 NCM 文件
ncm1.WriteToFile("example-output.ncm"); // 支持导出 NCM 文件

using (FileStream fs = new FileStream("example.ncm", FileMode.Open))
{
    NCM ncm2 = NCM.ReadFromStream(fs); // 支持从流读取 NCM
    // 支持修改文件内容，比如 RC4 密钥，封面，元数据和音乐数据等
    ncm2.RC4Key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
    ncm2.ImageCover = File.ReadAllBytes("cover.png");
    ncm2.Metadata = new NCMMetadata()
    {
        MusicName = "Example Song 01",
        Album = "Example Album"
    };
    using (MemoryStream ms = new MemoryStream())
    {
        ncm2.WriteStream(ms); // 支持导出 NCM 到流
    }
}

NCM ncm3 = new NCM() // 支持创建 NCM
{
    RC4Key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 },
    MusicData = File.ReadAllBytes("music.mp3"),
    Metadata = new NCMMetadata()
    {
        MusicName = "Example Song 02",
        Album = "Example Album",
        Format = "mp3"
    }
};
Console.WriteLine(ncm3.ToString()); // 打印 NCM 信息
```

## 参考资料
- [网易云音乐ncm格式分析以及ncm与mp3格式转换](https://www.cnblogs.com/cyx-b/p/13443003.html)
- [ncmdump](https://github.com/taurusxin/ncmdump)
- [ncmdumpGUI](https://github.com/kpali/ncmdumpGUI)