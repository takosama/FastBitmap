//お布施はこちらへ　学生　金欠です　https://www.amazon.jp/hz/wishlist/ls/IMC1G88FCO7X?ref_=wl_share
//takosama.312@gmail.comもしくは @rin_sns4に利用報告くれると嬉しいですが自由に使ってください
//くそコードなので責任取れません　自己責任で使ってください　

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

     // new Bentimark().BentimarkSIMDParallel2();
    }
}

public class Bentimark
{
    FastBitmap f = new FastBitmap();
    FastBitmap f2 = new FastBitmap();
   const int size =2;
    Bitmap bmp2 = new Bitmap(size,size);
    Bitmap bmp = new Bitmap(size * 4, size * 4);

    public Bentimark()
    {
        f.Start(bmp);
        f.Fill(Color.FromArgb(255, 255,255));
        f.End();

        f2.Start(bmp2);
        f2.Fill(Color.FromArgb(255, 255, 0));
        f2.End();
    }

    
   // [BenchmarkDotNet.Attributes.Benchmark]

  /*  public void BentimarkSIMDParallel2()
    {
    var f2 = new FastBitmap();

        f2.Start(bmp2);
        var f3 = new FastBitmap();
        f3.Start(bmp);
        for (int i = 0; i < 10; i++)
        {
            f2.ResizeBilinear2(f3);
         }

        f2.End();
        f3.End();

    }*/
  //  [BenchmarkDotNet.Attributes.Benchmark]

    public void BentimarkSIMDParallel()
    {
        var f2 = new FastBitmap();

        f2.Start(bmp2);
        var f3 = new FastBitmap();
        f3.Start(bmp);
        for (int i = 0; i < 10; i++)
        {
            f2.ResizeBilinear(f3);
        }

        f2.End();
        f3.End();

    }


    [BenchmarkDotNet.Attributes.Benchmark]
    public void BentimarkNomal()
    {

        Graphics g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.Bilinear;
        for(int i=0;i<10;i++)
        g.DrawImage(bmp2, 0, 0, bmp2.Width *4, bmp2.Height * 4);
      
        //  Graphics g = Graphics.FromImage(bmp);
      //  g.DrawImage(bmp2, new Point(100, 100));
        
    }
}
unsafe class FastBitmap
{
    BitmapData _bitmapData = null;
    public byte* _ptr = null;
    public int _stride = 0;
    public int width = 0;
    public int height = 0;
    Bitmap _bmp;
    public bool IsStarted = false;
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
        this.IsStarted = true;
    }

    public void End()
    {
        this._bmp.UnlockBits(this._bitmapData);
        this.IsStarted = false;
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

    public void DrawImage(FastBitmap bmp2, Point point)
    {
        if (!this.IsStarted || !bmp2.IsStarted)
            throw new Exception();
        int endX = Math.Min(bmp2.width, this.width - point.X);
        if (endX % 8 == 0)
        {
            Parallel.For(0, Math.Min(bmp2.height, this.height - point.Y), (y) =>
            {
                var ptr = (uint*)(this._ptr + (this._stride * (point.Y + y)) + point.X * 4);
                var ptr2 = (uint*)(bmp2._ptr + (bmp2._stride * y));

                for (int x = 0; x < endX / 8; x++)
                {
                    Avx2.Store(ptr, Avx.LoadVector256(ptr2));
                    ptr += 8;
                    ptr2 += 8;
                }
            });
        }
        else
        {
            Parallel.For(0, Math.Min(bmp2.height, this.height - point.Y), (y) =>
            {
                var ptr = (uint*)(this._ptr + (this._stride * (point.Y + y)) + point.X * 4);
                var ptr2 = (uint*)(bmp2._ptr + (bmp2._stride * y));
                for (int x = 0; x < endX; x++)
                {
                    *ptr++ = *ptr2++;
                }
            });
        }
    }




    public void ResizeSIMD2(FastBitmap rtnImage)
    {
        float scaleX = (float)this.width / rtnImage.width;
        float scaleY = (float)this.height / rtnImage.height;

        if (rtnImage.width % 4 == 0)
        {
            Parallel.For(0, rtnImage.height, (y) =>
            {
                var basePos = (uint*)(rtnImage._ptr + (rtnImage._stride * y));
                var rtnPos = (uint*)(this._ptr + (this._stride * (int)(y * scaleY)));

                Vector128<float> indexf = Vector128.Create(0.0f, 1, 2, 3);
                Vector128<float> iterf = Vector128.Create(4f, 4, 4, 4);
                Vector128<float> scalef = Vector128.Create(scaleX, scaleX, scaleX, scaleX);
                for (int x = 0; x < rtnImage.width; x += 4)
                {
                    Vector128<int> index = Avx.ConvertToVector128Int32WithTruncation(Avx.Multiply(indexf, scalef));
                    Avx.Store(basePos, Avx2.GatherVector128(rtnPos, index, 4));
                    indexf = Avx.Add(indexf, iterf);
                    basePos += 4;
                }
            });
        }
        else
        {
            Parallel.For(0, height, (y) =>
            {
                var basePos = (uint*)(rtnImage._ptr + (rtnImage._stride * y));
                var rtnPos = (uint*)(this._ptr + (this._stride * (int)(y * scaleY)));

                for (int x = 0; x < width; x += 4)
                {
                    *(uint*)(basePos + x) = *(uint*)(rtnPos + ((int)(x * scaleX)));
                }
            });
        }
    }

    public void ResizeBilinearNotOptimaized(FastBitmap rtnImage)
    {
        float scaleX = (float)this.width / rtnImage.width;
        float scaleY = (float)this.height / rtnImage.height;

        for (int y = 0; y < rtnImage.height; y++)
        {
            for (int x = 0; x < rtnImage.width; x++)
            {
                float py = scaleY * y;
                float px = scaleX * x;

                int y0 = (int)py;
                int y1 = y0 + 1;

                int x0 = (int)px;
                int x1 = x0 + 1;

                float ry = py - y0;
                float rx = px - x0;

                byte* _00 = this._ptr + (this._stride * y0) + (x0 * 4);
                byte* _01 = this._ptr + (this._stride * y0) + (x1 * 4);
                byte* _10 = this._ptr + (this._stride * y1) + (x0 * 4);
                byte* _11 = this._ptr + (this._stride * y1) + (x1 * 4);


                uint _y0u = 0;
                uint _y1u = 0;
                byte* _y0 = (byte*)&_y0u;
                byte* _y1 = (byte*)&_y1u;

                _y0[0] = (byte)(_00[0] + (_10[0] - _00[0]) * ry);
                _y0[1] = (byte)(_00[1] + (_10[1] - _00[1]) * ry);
                _y0[2] = (byte)(_00[2] + (_10[2] - _00[2]) * ry);
                _y0[3] = (byte)(_00[3] + (_10[3] - _00[3]) * ry);

                _y1[0] = (byte)(_01[0] + (_11[0] - _01[0]) * ry);
                _y1[1] = (byte)(_01[1] + (_11[1] - _01[1]) * ry);
                _y1[2] = (byte)(_01[2] + (_11[2] - _01[2]) * ry);
                _y1[3] = (byte)(_01[3] + (_11[3] - _01[3]) * ry);

                uint value = 0;
                ((byte*)(&value))[0] = (byte)(_y0[0] + (_y1[0] - _y0[0]) * rx);
                ((byte*)(&value))[1] = (byte)(_y0[1] + (_y1[1] - _y0[1]) * rx);
                ((byte*)(&value))[2] = (byte)(_y0[2] + (_y1[2] - _y0[2]) * rx);
                ((byte*)(&value))[3] = (byte)(_y0[3] + (_y1[3] - _y0[3]) * rx);

                *(uint*)(rtnImage._ptr + (rtnImage._stride * y) + (x * 4)) = value;
            }
        }
    }



    public void ResizeBilinearNotOptimaized2(FastBitmap rtnImage)
    {

        float scaleX = (float)this.width / rtnImage.width;
        float scaleY = (float)this.height / rtnImage.height;
        if (scaleX > 1 || scaleY > 1)
        {
            ResizeBilinearNotOptimaized(rtnImage);
            return;
        }

        byte[] tmp = new byte[4 * (this.height + 1) * (rtnImage.width)];
        fixed (byte* tmpp = tmp)
        {
            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < rtnImage.width; x++)
                {
                    float px = scaleX * x;
                    int x0 = (int)px;
                    int x1 = x0 + 1;
                    float rx = px - x0;

                    byte* _00 = this._ptr + (this._stride * y) + (x0 * 4);
                    byte* _01 = this._ptr + (this._stride * y) + (x1 * 4);

                    uint value = 0;
                    ((byte*)(&value))[0] = (byte)(_00[0] + (_01[0] - _00[0]) * rx);
                    ((byte*)(&value))[1] = (byte)(_00[1] + (_01[1] - _00[1]) * rx);
                    ((byte*)(&value))[2] = (byte)(_00[2] + (_01[2] - _00[2]) * rx);
                    ((byte*)(&value))[3] = (byte)(_00[3] + (_01[3] - _00[3]) * rx);
                    *(uint*)(tmpp + (4 * rtnImage.width * y) + (x * 4)) = value;
                }
            }
            for (int y = 0; y < rtnImage.height; y++)
            {
                float py = scaleY * y;
                int y0 = (int)py;
                int y1 = y0 + 1;
                float ry = py - y0;
                for (int x = 0; x < rtnImage.width; x++)
                {

                    byte* _00 = tmpp + (rtnImage.width * 4 * y0) + x * 4;
                    byte* _10 = tmpp + (rtnImage.width * 4 * y1) + x * 4;

                    uint value = 0;
                    ((byte*)(&value))[0] = (byte)(_00[0] + (_10[0] - _00[0]) * ry);
                    ((byte*)(&value))[1] = (byte)(_00[1] + (_10[1] - _00[1]) * ry);
                    ((byte*)(&value))[2] = (byte)(_00[2] + (_10[2] - _00[2]) * ry);
                    ((byte*)(&value))[3] = (byte)(_00[3] + (_10[3] - _00[3]) * ry);

                    *(uint*)(rtnImage._ptr + (rtnImage._stride * y) + (x * 4)) = value;
                }
            }
        }
    }

   /*public void ResizeBilinear2(FastBitmap rtnImage)
    {

        float scaleX = (float)this.width / rtnImage.width;
        float scaleY = (float)this.height / rtnImage.height;
        if (scaleX > 1 || scaleY > 1)
        {
            ResizeBilinear(rtnImage);
            return;
        }

        byte[] tmp = new byte[4 * (this.height + 1) * (rtnImage.width)];

        fixed (byte* p = tmp)
        {
            byte* tmpp = p;

            Parallel.For(0, this.height, (y) =>
            {
                var _00mask = Vector128.Create(0, 255, 255, 255, 1, 255, 255, 255, 2, 255, 255, 255, 3, 255, 255, 255);
                var _01mask = Vector128.Create(4, 255, 255, 255, 5, 255, 255, 255, 6, 255, 255, 255, 7, 255, 255, 255);
                var _vmask = Vector128.Create(0, 4, 8, 12, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);

                uint* store = stackalloc uint[4];

                uint* pos = (uint*)(this._ptr + (this._stride * y));
                uint* rtnPos = (uint*)(tmpp + (rtnImage._stride * y));
                for (int x = 0; x < rtnImage.width; x++)
                {
                    float px = scaleX * x;
                    int x0 = (int)px;
                    int x1 = x0 + 1;
                    float rx = px - x0;

                    var rxv = Vector128.Create(rx, rx, rx, rx);

                    var _ = Avx2.GatherVector128(pos, Vector128.Create(x0, x1, 0, 0), 4);
                    var _b = Vector128.AsByte(_);
                    var _00 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _00mask).AsInt32());
                    var _01 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _01mask).AsInt32());
                    var vf = Sse.Add(_00, Sse.Multiply(Sse.Subtract(_01, _00), rxv));
                    var vb = Sse2.ConvertToVector128Int32WithTruncation(vf).AsByte();
                    var v = Ssse3.Shuffle(vb, _vmask).AsUInt32();
                    Sse2.Store(store, v);
                    *rtnPos = *store;
                    rtnPos++;
                }
            });
            Parallel.For(0, rtnImage.height, (y) =>
            // for (int y = 0; y < rtnImage.height; y++)

            {
                float py = scaleY * y;
                int y0 = (int)py;
                int y1 = y0 + 1;

                float ry = py - y0;

                uint* pos = (uint*)(tmpp + rtnImage.width * 4 * y0);
                int offset = rtnImage.width;
                var ryv = Vector128.Create(ry, ry, ry, ry);

                var _00mask = Vector128.Create(0, 255, 255, 255, 1, 255, 255, 255, 2, 255, 255, 255, 3, 255, 255, 255);
                var _01mask = Vector128.Create(4, 255, 255, 255, 5, 255, 255, 255, 6, 255, 255, 255, 7, 255, 255, 255);
                var _vmask = Vector128.Create(0, 4, 8, 12, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                uint* rtnPos = (uint*)(rtnImage._ptr + (rtnImage._stride * y));
                uint* store = stackalloc uint[4];

                for (int x = 0; x < rtnImage.width; x++)
                {

                    var _ = Avx2.GatherVector128(pos, Vector128.Create(0, offset, 0, 0), 4);
                    var _b = Vector128.AsByte(_);
                    var _00 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _00mask).AsInt32());
                    var _01 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _01mask).AsInt32());
                    var vf = Sse.Add(_00, Sse.Multiply(Sse.Subtract(_01, _00), ryv));
                    var vb = Sse2.ConvertToVector128Int32WithTruncation(vf).AsByte();
                    var v = Ssse3.Shuffle(vb, _vmask).AsUInt32();
                    Sse2.Store(store, v);
                    *rtnPos = *store;
                    rtnPos++;
                    pos++;
                    //  *rtnPos = *pos;
                    //  rtnPos++;
                    //  pos++;
                    //    byte* _00 = tmpp + (rtnImage.width * 4 * y0) + x * 4;
                    //    byte* _10 = tmpp + (rtnImage.width * 4 * y1) + x * 4;
                    //
                    //    uint value = 0;
                    //    ((byte*)(&value))[0] = (byte)(_00[0] + (_10[0] - _00[0]) * ry);
                    //    ((byte*)(&value))[1] = (byte)(_00[1] + (_10[1] - _00[1]) * ry);
                    //    ((byte*)(&value))[2] = (byte)(_00[2] + (_10[2] - _00[2]) * ry);
                    //    ((byte*)(&value))[3] = (byte)(_00[3] + (_10[3] - _00[3]) * ry);
                    //
                    //    *(uint*)(rtnImage._ptr + (rtnImage._stride * y) + (x * 4)) = value;

                }
            });
        }
    }*/

    //int y0 = (int)py;
    //int y1 = y0 + 1;
    //
    //int x0 = (int)px;
    //int x1 = x0 + 1;
    //
    //float ry = py - y0;
    //float rx = px - x0;
    //
    //byte* _00 = this._ptr + (this._stride * y0) + (x0 * 4);
    //byte* _01 = this._ptr + (this._stride * y0) + (x1 * 4);
    //byte* _10 = this._ptr + (this._stride * y1) + (x0 * 4);
    //byte* _11 = this._ptr + (this._stride * y1) + (x1 * 4);
    //
    //
    //uint _y0u = 0;
    //uint _y1u = 0;
    //byte* _y0 = (byte*)&_y0u;
    //byte* _y1 = (byte*)&_y1u;
    //
    //_y0[0] = (byte)(_00[0] + (_10[0] - _00[0]) * ry);
    //_y0[1] = (byte)(_00[1] + (_10[1] - _00[1]) * ry);
    //_y0[2] = (byte)(_00[2] + (_10[2] - _00[2]) * ry);
    //_y0[3] = (byte)(_00[3] + (_10[3] - _00[3]) * ry);
    //
    //_y1[0] = (byte)(_01[0] + (_11[0] - _01[0]) * ry);
    //_y1[1] = (byte)(_01[1] + (_11[1] - _01[1]) * ry);
    //_y1[2] = (byte)(_01[2] + (_11[2] - _01[2]) * ry);
    //_y1[3] = (byte)(_01[3] + (_11[3] - _01[3]) * ry);
    //
    //uint value = 0;
    //((byte*)(&value))[0] = (byte)(_y0[0] + (_y1[0] - _y0[0]) * rx);
    //((byte*)(&value))[1] = (byte)(_y0[1] + (_y1[1] - _y0[1]) * rx);
    //((byte*)(&value))[2] = (byte)(_y0[2] + (_y1[2] - _y0[2]) * rx);
    //((byte*)(&value))[3] = (byte)(_y0[3] + (_y1[3] - _y0[3]) * rx);
    //
    //*(uint*)(rtnImage._ptr + (rtnImage._stride * y) + (x * 4)) = value;

    public void ResizeBilinear(FastBitmap rtnImage)
    {
        float scaleX = (float)this.width / rtnImage.width;
        float scaleY = (float)this.height / rtnImage.height;

        var _00mask = Vector128.Create(0, 255, 255, 255, 1, 255, 255, 255, 2, 255, 255, 255, 3, 255, 255, 255);
        var _01mask = Vector128.Create(4, 255, 255, 255, 5, 255, 255, 255, 6, 255, 255, 255, 7, 255, 255, 255);
        var _10mask = Vector128.Create(8, 255, 255, 255, 9, 255, 255, 255, 10, 255, 255, 255, 11, 255, 255, 255);
        var _11mask = Vector128.Create(12, 255, 255, 255, 13, 255, 255, 255, 14, 255, 255, 255, 15, 255, 255, 255);
        var _vmask = Vector128.Create(0, 4, 8, 12, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);

        Parallel.For(0, rtnImage.height, (y) =>
       {
           float py = scaleY * y;
           int y0 = (int)py;
           int y1 = y0 + 1;
           float ry = py - y0;
           var ryv = Vector128.Create(ry, ry, ry, ry);

           uint* py0 = (uint*)(this._ptr + (this._stride * y0));
           uint* py1 = (uint*)(this._ptr + (this._stride * y1));
           int dy = (int)(py1 - py0);

           uint* rtnPos = (uint*)(rtnImage._ptr + (rtnImage._stride * y));
           uint* store = stackalloc uint[4];



           for (int x = 0; x < rtnImage.width; x++)
           {
               float px = scaleX * x;
               int x0 = (int)px;
               int x1 = x0 + 1;

               float rx = px - x0;
               var rxv = Vector128.Create(rx, rx, rx, rx);

               var _ = Avx2.GatherVector128(py0, Vector128.Create(x0, x1, x0 + dy, x1 + dy), 4);
               var _b = Vector128.AsByte(_);

               var _00 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _00mask).AsInt32());
               var _01 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _01mask).AsInt32());

               var _10 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _10mask).AsInt32());
               var _11 = Sse2.ConvertToVector128Single(Ssse3.Shuffle(_b, _11mask).AsInt32());

               var _y0 = Sse.Add(_00, Sse.Multiply(Sse.Subtract(_10, _00), ryv));
               var _y1 = Sse.Add(_01, Sse.Multiply(Sse.Subtract(_11, _01), ryv));

               var vf = Sse.Add(_y0, Sse.Multiply(Sse.Subtract(_y1, _y0), rxv));
               var vb = Sse2.ConvertToVector128Int32WithTruncation(vf).AsByte();

               var v = Ssse3.Shuffle(vb, _vmask).AsUInt32();
               Sse2.Store(store, v);
               *rtnPos = *store;
               rtnPos++;

               _00 = _10;
               _01 = _11;
           }
       });
    }


    public void Resize(FastBitmap rtnImage)
    {
        float scaleX = (float)this.width / rtnImage.width;
        float scaleY = (float)this.height / rtnImage.height;
        for (int y = 0; y < rtnImage.height; y++)
        {
            var basePos = (uint*)(rtnImage._ptr + (rtnImage._stride * y));
            var rtnPos = (uint*)(this._ptr + (this._stride * (int)(y * scaleY)));
            for (int x = 0; x < rtnImage.width; x++)
            {
                *(uint*)(basePos + x) = *(uint*)(rtnPos + ((int)(x * scaleX)));
            }
        }
    }
}
