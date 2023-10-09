using ICSharpCode.SharpZipLib.Zip;

public class EpubProcessor
{
    static void Main(string[] args)
    {
        Console.WriteLine("输入EPUB文件夹：");
        string sourceFolder = Console.ReadLine();
        if (!Directory.Exists(sourceFolder))
        {
            Console.WriteLine("filePath is null");
        }
        string temDestinationFolder = Path.Combine(Path.GetDirectoryName(sourceFolder), Path.GetFileName(sourceFolder) + "tem");
        string destinationFolder = Path.Combine(Path.GetDirectoryName(sourceFolder), Path.GetFileName(sourceFolder) + "new");
        EpubProcessor epubProcessor = new EpubProcessor();
        epubProcessor.ProcessEpubFolder(sourceFolder, temDestinationFolder, destinationFolder);
        Directory.Delete(temDestinationFolder, true);
    }

    public void ProcessEpubFolder(string sourceFolder, string temDestinationFolder, string newDestinationFolder)
    {
        // 1. 读取指定文件夹下的所有epub文件
        string[] epubFiles = Directory.GetFiles(sourceFolder, "*.epub");

        if (Directory.Exists(temDestinationFolder))
        {
            try
            {
                Directory.Delete(temDestinationFolder, true);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        if (Directory.Exists(newDestinationFolder))
        {
            try
            {
                Directory.Delete(newDestinationFolder, true);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        if (!Directory.Exists(newDestinationFolder))
        {
            Directory.CreateDirectory(newDestinationFolder);
        }

        foreach (var epubFile in epubFiles)
        {
            // 2. 解压epub文件，获取其中的html文件
            string tempFolder = Path.Combine(temDestinationFolder, Path.GetFileNameWithoutExtension(epubFile));
            UnzipEpub(epubFile, tempFolder);
            string newSglFolder = Path.Combine(newDestinationFolder, Path.GetFileNameWithoutExtension(epubFile));
            Directory.CreateDirectory(newSglFolder);

            // 3. 从html文件中提取图像地址
            ProcessHtmlFiles(tempFolder, newSglFolder);

            // 4. 保存这些图像到新的文件夹，并按顺序重命名
            // 5. 压缩新的文件夹为RAR文件
            CompressToRar(tempFolder);
        }
    }

    private void UnzipEpub(string epubFile, string destinationFolder)
    {
        // 使用SharpZipLib解压缩epub文件
        // 将文件解压到destinationFolder
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        using (FileStream fs = new FileStream(epubFile, FileMode.Open, FileAccess.Read))
        {
            using (ZipInputStream zipStream = new ZipInputStream(fs))
            {
                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (!entry.IsFile)
                    {
                        continue;
                    }

                    string entryFileName = Path.Combine(destinationFolder, entry.Name);

                    Directory.CreateDirectory(path: Path.GetDirectoryName(entryFileName));

                    using (FileStream entryStream = new FileStream(entryFileName, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            entryStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }
    }

    private void ProcessHtmlFiles(string folder, string newDestinationFolder)
    {
        // 使用HTMLAgilityPack解析html文件，提取图像地址
        // 保存图像到新的文件夹，并按顺序重命名
        int targetLineNumber = 11;
        string[] banFiles = { "cover.png", "createby", "tpl_2", "tpl_4" };

        string htmlFolder = Path.Combine(folder, "html");

        string[] htmlFiles = Directory.GetFiles(htmlFolder, "*.html")
        .OrderBy(file => int.TryParse(Path.GetFileNameWithoutExtension(file), out int number) ? number : int.MaxValue)
        .ToArray()
        ;
        foreach (var htmlFile in htmlFiles)
        {
            string htmlName = Path.GetFileNameWithoutExtension(htmlFile);
            if (banFiles.Contains(htmlName))
            {
                continue;
            }


            string targetLine = File.ReadLines(htmlFile).Skip(targetLineNumber - 1).FirstOrDefault();

            int startIndex = targetLine.IndexOf("src=\"") + 5;
            int endIndex = targetLine.IndexOf("\"", startIndex);
            string imgSrc;

            if (startIndex != -1 && endIndex != -1)
            {
                imgSrc = targetLine.Substring(startIndex, endIndex - startIndex);
                int lastSeparatorIndex = imgSrc.LastIndexOf('/');
                imgSrc = imgSrc.Substring(lastSeparatorIndex + 1);
            }
            else
            {
                Console.WriteLine("No 'src' attribute found in the line.");
                continue;
            }
            string imgDestination;

            if (int.TryParse(htmlName, out int num))
            {
                num++;
                imgDestination = num.ToString();
            }
            else
            {
                imgDestination = "1";
            }
            imgDestination = imgDestination + ".jpg";

            string imgFolder = Path.Combine(folder, "image");
            string sourceImgPath = Path.Combine(imgFolder, imgSrc);
            string newDestinationImgPath = Path.Combine(newDestinationFolder, imgDestination);
            File.Move(sourceImgPath, newDestinationImgPath);

        }
    }

    private void CompressToRar(string folder)
    {
        // 使用SharpZipLib将文件夹压缩为rar
    }
}
