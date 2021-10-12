using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageSR
{
    class Program
    {
        static void Main(string[] args)
        {
            while (ChooseWorkType()) ;
        }

        static bool ChooseWorkType()
        {
            Console.Clear();
            Console.WriteLine("请选择要执行的操作：");
            Console.WriteLine("1. 合成图像");
            Console.WriteLine("2. 分割图像");
            Console.WriteLine("3. 退出");
            string choose;
            string[] chooseList = { "1", "2", "3" };
            while (!chooseList.Contains(choose = Console.ReadLine().Trim())) ;
            switch (choose)
            {
                case "1":
                    Synthesis();
                    break;
                case "2":
                    Split();
                    break;
                case "3":
                    return false;
            }
            return true;
        }

        static void Synthesis()
        {
            Console.Clear();
            string input;
            var row = 0;
            var columns = new List<int>();
            var sources = new List<List<string>>();
            while (true)
            {
                Console.WriteLine("请输入要合成的图像行数：");
                input = Console.ReadLine().Trim();
                if (int.TryParse(input, out row))
                {
                    if (row > 0)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("合成行数不可小于 1");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("数值格式错误");
                }
            }
            for (int i = 0; i < row; i++)
            {
                while (true)
                {
                    Console.WriteLine($"请输入第 {i + 1} 行的图像数量：");
                    input = Console.ReadLine().Trim();
                    if (int.TryParse(input, out int count))
                    {
                        if (count > 0)
                        {
                            columns.Add(count);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("合成图像数不可小于 1");
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("数值格式错误");
                    }
                }
            }
            for (int i = 0; i < row; i++)
            {
                Console.WriteLine($"开始设置第 {i + 1} 行的图像");
                var files = new List<string>();
                for (int j = 0; j < columns[i]; j++)
                {
                    while (true)
                    {
                        Console.WriteLine($"请输入第 {j + 1} 张图的图像路径：");
                        input = Console.ReadLine().Trim();
                        if (File.Exists(input))
                        {
                            files.Add(input);
                            break;
                        }
                        else if (!string.IsNullOrWhiteSpace(input))
                            Console.WriteLine("文件不存在");
                    }
                }
                sources.Add(files);
            }
            Console.WriteLine("生成中……");
            var images = sources.Select(x => x.Select(y => Image.Load<Rgba32>(y)).ToList()).ToList();
            var result = ImageUtils.Synthesis(images);
            string filename = "output" + (result.Frames.Count > 1 ? ".gif" : ".png");
            ImageUtils.SaveImage(result, filename);
            Console.WriteLine("生成完毕，文件已储存至：" + filename);
            Console.WriteLine("按任意键继续");
            Console.ReadKey();
        }

        static void Split()
        {
            Console.Clear();
            string input;
            string file = string.Empty;
            int row = 0, column = 0;
            while (true)
            {
                Console.WriteLine("请输入要分隔的图像文件位置：");
                input = Console.ReadLine().Trim();
                if (File.Exists(input))
                {
                    file = input;
                    break;
                }
                else if (!string.IsNullOrWhiteSpace(input))
                    Console.WriteLine("文件不存在");
            }
            while (true)
            {
                Console.WriteLine("请输入要分割的行数：");
                input = Console.ReadLine().Trim();
                if (int.TryParse(input, out row))
                {
                    if (row > 0)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("分割行数不可小于 1");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("数值格式错误");
                }
            }
            while (true)
            {
                Console.WriteLine("请输入要分割的列数：");
                input = Console.ReadLine().Trim();
                if (int.TryParse(input, out column))
                {
                    if (column > 0)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("分割列数不可小于 1");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("数值格式错误");
                }
            }
            if (!Directory.Exists("Output"))
            {
                Directory.CreateDirectory("Output");
            }
            Console.WriteLine("生成中……");
            var name = Path.GetFileNameWithoutExtension(file);
            var source = Image.Load<Rgba32>(file);
            var result = ImageUtils.Split(source, row, column);
            var saves = new List<string>();
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i].Frames.Count > 1)
                {
                    var filename = $"Output/{name}_{i + 1}.gif";
                    result[i].SaveAsGif(filename);
                    saves.Add(filename);
                }
                else
                {
                    var filename = $"Output/{name}_{i + 1}.png";
                    result[i].SaveAsPng(filename);
                    saves.Add(filename);
                }
            }
            Console.WriteLine("生成完毕，文件已储存至：");
            foreach (var path in saves)
                Console.WriteLine(path);
            Console.WriteLine("按任意键继续");
            Console.ReadKey();
        }
    }
}
