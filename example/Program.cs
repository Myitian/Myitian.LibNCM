using Myitian.LibNCM;

Console.WriteLine("path");
string sncm = Console.ReadLine();
NCM ncm = NCM.Create(sncm);
Console.WriteLine(ncm);
Console.WriteLine(Convert.ToHexString(ncm.Gap0.Span));
Console.WriteLine(Convert.ToHexString(ncm.Gap1.Span));
ncm.WriteToFile(sncm + ".re.ncm");
File.WriteAllBytes(ncm.Metadata.MusicName + "." + ncm.Metadata.Format, ncm.MusicData.ToArray());
File.WriteAllBytes(ncm.Metadata.MusicName + Path.GetExtension(ncm.Metadata.AlbumPic), ncm.CoverImage.ToArray());