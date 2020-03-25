//お布施はこちらへ　学生　金欠です　https://www.amazon.jp/hz/wishlist/ls/IMC1G88FCO7X?ref_=wl_share
//takosama.312@gmail.comもしくは @rin_sns4に利用報告くれると嬉しいですが自由に使ってください
//くそコードなので責任取れません　自己責任で使ってください　

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using BenchmarkDotNet;


public class Program
{
    public static void Main()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<Bentimark>();
    }
}

public class Bentimark
{
    Bitmap bmp = new Bitmap(1919,1080);
    [BenchmarkDotNet.Attributes.Benchmark]

    public void BentimarkSIMDParallel()
    {
        FastBitmap Fbmp = new FastBitmap();
        Fbmp.Start(bmp);
        Fbmp.Fill(Color.FromArgb(255, 255, 255));
        Fbmp.End();
    }
    [BenchmarkDotNet.Attributes.Benchmark]
    public void BentimarkNomal()
    {
        Graphics g = Graphics.FromImage(bmp);

        g.FillRectangle(Brushes.White, new Rectangle(0, 0, bmp.Width, bmp.Height));
     
        
        
    }
}
unsafe class FastBitmap
{
    BitmapData _bitmapData = null;
    byte* _ptr = null;
    int _stride = 0;
    int width = 0;
    int height = 0;
    Bitmap _bmp;

    public void Start(Bitmap bmp)
    {


        //    Bitmap bmp = new Bitmap(x,y);
        this._bmp = bmp;
        //
        var bmpData = bmp.LockBits(new Rectangle(new Point(0, 0), new Size(bmp.Width, bmp.Height)), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        this._bitmapData = bmpData;

        byte* ptr = (byte*)bmpData.Scan0.ToPointer();
        this._ptr = ptr;

        this._stride = bmpData.Stride;

        this.width = bmp.Width;
        this.height = bmp.Height;
    }

    public void End()
    {
        this._bmp.UnlockBits(this._bitmapData);

    }


    public void SetPixel(int x, int y, Color col)
    {
        *(byte*)(this._ptr + (this._stride * y) + (x * 4)) = col.B;
        *(byte*)(this._ptr + (this._stride * y) + (x * 4) + 1) = col.G;
        *(byte*)(this._ptr + (this._stride * y) + (x * 4) + 2) = col.R;
        *(byte*)(this._ptr + (this._stride * y) + (x * 4) + 3) = col.A;
    }

    public void Fill(Color col)
    {
        uint color = 0;// ((uint)col.B) << 24 + ((uint)col.R) << 16 +((uint) col.R) << 8 + (uint)col.A;
        color = col.A;
        color <<= 8;
        color += col.B;
        color <<= 8;
        color += col.G;
        color <<= 8;
        color += col.R;
        if (this.width % 8 == 0)
        {
            Vector256<uint> vector = Vector256.Create(color, color, color, color, color, color, color, color);
            int xEnd = this.width / 8;

            Parallel.For(0, this.height, (int y) =>
            {
                var ptr = (uint*)(this._ptr + (this._stride * y));
                for (int x = 0; x < xEnd; x++)
                {
                    Avx.Store(ptr, vector);
                    //*(ptr) = color;
                    ptr += 8;
                }
            });
        }
        else if (this.width % 4 == 0)
        {
            Vector128<uint> vector = Vector128.Create(color, color, color, color);

            Parallel.For(0, this.height, (int y) =>
            {
                var ptr = (uint*)(this._ptr + (this._stride * y));
                for (int x = 0; x < this.width / 4; x++)
                {
                    Sse2.Store(ptr, vector);
                    //*(ptr) = color;
                    ptr += 4;
                }
            });
        }
        else
        {
            Parallel.For(0, this.height, (int y) =>
            {
                var ptr = (uint*)(this._ptr + (this._stride * y));
                for (int x = 0; x < this.width; x++)
                {

                    *(ptr) = color;
                    ptr++;
                }
            });
        }

    }

}
