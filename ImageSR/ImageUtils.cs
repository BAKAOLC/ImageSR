using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using RgbaImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

namespace ImageSR
{
    public static partial class ImageUtils
    {
        struct FrameData
        {
            public RgbaImage[] Frames;
            public int[] Delays;
            public int TotalLength => Delays.Sum(x => x);
            public bool IsGif;

            public FrameData(RgbaImage image)
            {
                IsGif = image.Frames.Count > 1;
                if (IsGif)
                {
                    Delays = new int[image.Frames.Count];
                    Frames = new RgbaImage[image.Frames.Count];
                    for (int i = 0; i < image.Frames.Count; i++)
                    {
                        Delays[i] = image.Frames[i].GetFrameDelay();
                        Frames[i] = image.Frames.CloneFrame(i);
                    }
                }
                else
                {
                    Delays = Array.Empty<int>();
                    Frames = new RgbaImage[] { image };
                }
            }

            public RgbaImage GetFrameOnTime(int t)
            {
                if (IsGif)
                {
                    int T = 0;
                    for (int i = 0; i < Delays.Length; i++)
                    {
                        T += Delays[i];
                        if (t < T)
                            return Frames[i];
                    }
                    return Frames.Last();
                }
                else
                    return Frames.First();
            }
        }

        static int GetFrameDelay(this ImageFrame frame)
            => frame.Metadata.GetGifMetadata().FrameDelay;

        static int[] CalcFrames(FrameData[] datas)
        {
            var list = new List<int>();
            list.Add(0);
            foreach (var data in datas)
            {
                if (data.IsGif)
                {
                    int t = 0;
                    foreach (var delay in data.Delays)
                    {
                        t += delay;
                        if (!list.Contains(t))
                            list.Add(t);
                    }
                }
            }
            return list.OrderBy(x => x).ToArray();
        }

        public static RgbaImage Synthesis(List<List<RgbaImage>> images)
        {
            int width = 0, height = 0;
            var datas = new List<FrameData>();
            var _datas = new List<List<FrameData>>();
            var heights = new List<int>();
            foreach (var row in images)
            {
                int w = 0, h = 0;
                var list = new List<FrameData>();
                foreach (var img in row)
                {
                    w += img.Width;
                    h = Math.Max(h, img.Height);
                    var data = new FrameData(img);
                    datas.Add(data);
                    list.Add(data);
                }
                _datas.Add(list);
                width = Math.Max(width, w);
                heights.Add(h);
                height += h;
            }
            var frames = CalcFrames(datas.ToArray());
            if (frames.Length > 1)
            {
                var resultImage = new RgbaImage(width, height);
                resultImage.Metadata.GetGifMetadata().RepeatCount = 0;
                for (int i = 0; i < frames.Length - 1; i++)
                {
                    int T = frames[i];
                    int nextT = frames[i + 1];
                    int delay = nextT - T;
                    var frame = DrawFrame(width, height, _datas, heights, T);
                    frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = delay;
                    resultImage.Frames.AddFrame(frame.Frames.RootFrame);
                }
                resultImage.Frames.RemoveFrame(0);
                return resultImage;
            }
            else
            {
                return DrawFrame(width, height, _datas, heights, 0);
            }
        }

        static void DrawImage(RgbaImage texture, RgbaImage image, int x, int y)
        {
            for (int py = 0; py < image.Height; py++)
            {
                for (int px = 0; px < image.Width; px++)
                {
                    int _x = x + px;
                    int _y = y + py;
                    if (_x >= 0 && _x < texture.Width && _y >= 0 && _y < texture.Height)
                    {
                        texture[_x, _y] = image[px, py];
                    }
                }
            }
        }

        static readonly Rgba32 BackgroundColor = new Rgba32(1f, 1f, 1f, 1f);
        static RgbaImage DrawFrame(int width, int height, List<List<FrameData>> datas, List<int> heights, int time)
        {
            var resultImage = new RgbaImage(width, height);
            for (var py = 0; py < height; py++)
                for (var px = 0; px < width; px++)
                    resultImage[px, py] = BackgroundColor;
            int y = 0;
            for (var i = 0; i < datas.Count; i++)
            {
                int x = 0;
                var row = datas[i];
                var h = heights[i];
                for (var j = 0; j < row.Count; j++)
                {
                    var img = row[j].GetFrameOnTime(time);
                    DrawImage(resultImage, img, x, y + h - img.Height);
                    x += img.Width;
                }
                y += h;
            }
            return resultImage;
        }

        public static RgbaImage[] Split(RgbaImage image, int row, int column)
        {
            int pw = image.Width / row;
            int ph = image.Height / column;
            int y = 0;
            var list = new RgbaImage[row * column];
            var id = 0;
            for (int h = 0; h < row; h++)
            {
                int x = 0;
                if (h == row - 1)
                {
                    int _h = image.Height - y;
                    for (int w = 0; w < column; w++)
                    {
                        if (w == column - 1)
                        {
                            list[id] = GetSubImage(image, x, y, image.Width - x, _h);
                        }
                        else
                        {
                            list[id] = GetSubImage(image, x, y, pw, _h);
                            x += pw;
                        }
                        id++;
                    }
                }
                else
                {
                    for (int w = 0; w < column; w++)
                    {
                        if (w == column - 1)
                        {
                            list[id] = GetSubImage(image, x, y, image.Width - x, ph);
                        }
                        else
                        {
                            list[id] = GetSubImage(image, x, y, pw, ph);
                            x += pw;
                        }
                        id++;
                    }
                    y += ph;
                }
            }
            return list;
        }

        static RgbaImage GetSubImage(RgbaImage image, int x, int y, int w, int h)
        {
            var result = new RgbaImage(w, h);
            if (image.Frames.Count > 1)
            {
                result.Metadata.GetGifMetadata().RepeatCount = 0;
                for (int i = 0; i < image.Frames.Count; i++)
                {
                    var frame = GetSubImage(image.Frames.CloneFrame(i), x, y, w, h);
                    frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = image.Frames[i].Metadata.GetGifMetadata().FrameDelay;
                    result.Frames.AddFrame(frame.Frames.RootFrame);
                }
                result.Frames.RemoveFrame(0);
            }
            else
            {
                for (int py = 0; py < h; py++)
                {
                    for (int px = 0; px < w; px++)
                    {
                        int _x = x + px;
                        int _y = y + py;
                        if (_x >= 0 && _x < image.Width && _y >= 0 && _y < image.Height)
                        {
                            result[px, py] = image[_x, _y];
                        }
                    }
                }
            }
            return result;
        }

        public static void SaveImage(RgbaImage image, string name)
        {
            if (image.Frames.Count > 1)
            {
                image.SaveAsGif(name);
            }
            else
            {
                image.SaveAsPng(name);
            }
        }
    }
}
