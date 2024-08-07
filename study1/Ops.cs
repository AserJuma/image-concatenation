﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Configuration;
using System.Runtime.InteropServices;

namespace study1
{
    public static class Ops // Operations
    {
        private static byte[,] CrossShape
        {
            get
            {
                return new byte[,]
                {
                    { 0, 1, 0 },
                    { 1, 1, 1 },
                    { 0, 1, 0 }
                };
            }
        }
        
        // Otsu helper function, Computes image histogram of pixel intensities
        // Initializes an array, iterates through and fills up histogram count values
        private static unsafe void GetHistogram(byte* pt, int width, int height, int stride, int[] histArr)
        {
            histArr.Initialize();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width * 3; j += 3)
                {
                    int index = i * stride + j;
                    histArr[pt[index]]++;
                }
            }
        }

        // Otsu helper function, Compute q values
        // Gets the sum of some histogram values within an intensity range
        private static float Px(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += hist[i];

            return sum;
        }

        // Otsu helper function, Get the mean values in the equation
        // Gets weighted sum of histogram values in an intensity range
        private static float Mx(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += i * hist[i];

            return sum;
        }

        // Otsu helper function, Maximum element
        private static int FindMax(float[] vec, int n) // Returns index of maximum float value in array
        {
            float maxVec = 0;
            int idx = 0;
            int i;
            for (i = 1; i < n - 1; i++)
            {
                if (vec[i] > maxVec)
                {
                    maxVec = vec[i];
                    idx = i;
                }
            }

            return idx;
        }

        // Otsu's threshold
        private static byte GetOtsuThreshold(Bitmap bmp)
        {
            float[] vet = new float[256];
            int[] hist = new int[256];
            vet.Initialize();

            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);
            unsafe
            {
                byte* p = (byte*)bmData.Scan0.ToPointer();
                GetHistogram(p, bmp.Width, bmp.Height, bmData.Stride,
                    hist); // Fills up an array with pixel intensity values
                // loop through all possible threshold values and maximize between-class variance
                for (int k = 1; k != 255; k++)
                {
                    var p1 = Px(0, k, hist);
                    var p2 = Px(k + 1, 255, hist);
                    // Continually sums up histogram values in different ranges, covering the span of the image data, in two float values p1, p2
                    var p12 = p1 * p2;
                    if (p12 == 0)
                        p12 = 1;
                    float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                    vet[k] = diff * diff /
                             p12; // Computes and stores variance values for each threshold value using simple variance formula from statistics.
                    //vet[k] = (float)Math.Pow((Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1), 2) / p12; // Another way to compute variance (more overhead/overly complex)
                }
            }

            bmp.UnlockBits(bmData);

            return (byte)FindMax(vet, 256); // Finds maximum variance value
        }

        private static Bitmap ChangePixelFormat(Bitmap bmp)
        {
            PixelFormat pFormat = bmp.PixelFormat;
            Bitmap convertedBmp = new Bitmap(bmp.Width, bmp.Height);
            //Bitmap convertedBmp = (Bitmap)bmp.Clone();
            switch (pFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    //Console.WriteLine("Case 1bit");
                    convertedBmp = Convert1To8(bmp);
                    break;
                case PixelFormat.Format24bppRgb:
                    //Console.WriteLine("Case 8bit");
                    convertedBmp = Convert24To8(bmp);
                    break;
                default:
                    Console.WriteLine("Default entered");
                    break;
            }

            return convertedBmp;
        }

        public static Bitmap Binarize(Bitmap bmp, int thresholdValue)
        {
            Bitmap cBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed); // New empty bitmap
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                cBmp = ChangePixelFormat(bmp);
            
            BitmapData bmpData = cBmp.LockBits(new Rectangle(0, 0, cBmp.Width, cBmp.Height), ImageLockMode.ReadWrite,
                cBmp.PixelFormat);

            int height = cBmp.Height;
            int width = cBmp.Width;
            int stride = bmpData.Stride;
            int offset = stride - cBmp.Width;
            unsafe
            {
                byte* pt = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        *pt = (byte)(*pt < thresholdValue ? 0 : 255);
                        pt += 1;
                    }
                    pt += offset;
                }
            }

            cBmp.UnlockBits(bmpData);
            return cBmp;
        }
        public static Bitmap MeanBinarize(Bitmap bmp)
        {
            byte th = GetOtsuThreshold(bmp);
            return Binarize(bmp, th);
        }
        private static ColorPalette DefineGrayPalette(Bitmap bmp)
        {
            ColorPalette palette = bmp.Palette;
            for (int i = 0; i < 256; i++)
                palette.Entries[i] = Color.FromArgb(i, i, i);
            return palette; //TODO: Is there a better way?
        }

        public static Bitmap Concatenate(Bitmap bmp, Bitmap bmp2, bool direction)
        {
            Bitmap cbmp1 = (Bitmap)bmp.Clone();//new Bitmap(bmp.Width, bmp.Height);
            Bitmap cbmp2 = (Bitmap)bmp2.Clone(); //new Bitmap(bmp2.Width, bmp2.Height);
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                cbmp1 = ChangePixelFormat(bmp);
            if (bmp2.PixelFormat != PixelFormat.Format8bppIndexed)
                cbmp2 = ChangePixelFormat(bmp2);
            
            BitmapData bmData = cbmp1.LockBits(new Rectangle(0, 0, cbmp1.Width, cbmp1.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bmData2 = cbmp2.LockBits(new Rectangle(0, 0, cbmp2.Width, cbmp2.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            int combWidth = direction ? cbmp1.Width + cbmp2.Width : Math.Max(cbmp1.Width, cbmp2.Width);
            int combHeight = direction? Math.Max(cbmp1.Height, cbmp2.Height): cbmp1.Height + cbmp2.Height;
            
            Bitmap concBmp = new Bitmap(combWidth, combHeight, PixelFormat.Format8bppIndexed);
            concBmp.Palette = DefineGrayPalette(concBmp);
            BitmapData concData = concBmp.LockBits(new Rectangle(0, 0, concBmp.Width, concBmp.Height),
                ImageLockMode.WriteOnly, concBmp.PixelFormat);

            int width = cbmp1.Width; int width2 = cbmp2.Width;
            int height = cbmp1.Height; int height2 = cbmp2.Height;
            int stride1 = bmData.Stride; int stride2 = bmData2.Stride; int strideC = concData.Stride;
            int offset1 = stride1 - width; int offset2 = stride2 - width2; int offsetC = strideC - combWidth;

            unsafe
            {
                byte* p = (byte*)bmData.Scan0.ToPointer();
                byte* p2 = (byte*)bmData2.Scan0.ToPointer();
                byte* pC = (byte*)concData.Scan0.ToPointer();

                if (direction) // horizontal
                {
                    for (int y = 0; y < combHeight; y++)
                    {
                        for (int x = 0; x < combWidth; x++)
                        {
                            if (x < width && y < height)
                            {
                                *pC = *p;
                                p += 1;
                            }
                            else if (x >= width && y < height2)
                            {
                                *pC = *p2;
                                p2 += 1;
                            }
                            pC += 1;
                        }
                        p += offset1;
                        p2 += offset2;
                        pC += offsetC;
                    }
                }
                else // vertical
                {
                    for (int y = 0; y < combHeight; y++)
                    {
                        for (int x = 0; x < combWidth; x++)
                        {
                            if (x < width && y < height)
                            {
                                *pC = *p;
                                p += 1;
                            }
                            else if (x < width2 && y >= height)
                            {
                                *pC = *p2;
                                p2 += 1;
                            }

                            pC += 1;
                        }
                        p += offset1;
                        p2 += offset2;
                        pC += offsetC;
                    }
                }
            }

            cbmp1.UnlockBits(bmData);
            cbmp2.UnlockBits(bmData2);
            concBmp.UnlockBits(concData);
            return concBmp;
        }
        
        public static Bitmap Convert24To8(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb) throw new Exception("Error: Bitmap not 24bit");
            
            Bitmap newB = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format8bppIndexed);
            newB.Palette = DefineGrayPalette(newB);
            BitmapData b1 = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            
            BitmapData b2 = newB.LockBits(new Rectangle(0, 0, newB.Width, newB.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            
            int stride = b1.Stride;
            int stride8 = b2.Stride;
            int offset = stride - bitmap.Width * 3;
            int offset8 = stride8 - newB.Width;
            
            int height = bitmap.Height;
            int width = bitmap.Width;

            unsafe
            {
                byte* ptr = (byte*)b1.Scan0.ToPointer();
                byte* ptr8 = (byte*)b2.Scan0.ToPointer();

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        *ptr8 = (byte)(.299 * ptr[0] + .587 * ptr[1] + .114 * ptr[2]); // Weighted average formula
                        ptr += 3;
                        ptr8++;
                    }

                    ptr += offset;
                    ptr8 += offset8;
                }
            }

            bitmap.UnlockBits(b1);
            newB.UnlockBits(b2);
            Console.WriteLine($"New Bitmap has {newB.PixelFormat} Pixel format from 24bit");
            return newB;
        }

        public static Bitmap Convert1To8(Bitmap bitmap1)
        {
            if (bitmap1.PixelFormat != PixelFormat.Format1bppIndexed) throw new Exception("Error: Bitmap not 1bit");
            Bitmap bitmap8 = new Bitmap(bitmap1.Width, bitmap1.Height, PixelFormat.Format8bppIndexed);
            bitmap8.Palette = DefineGrayPalette(bitmap8);
            BitmapData bmp1data = bitmap1.LockBits(new Rectangle(0, 0, bitmap1.Width, bitmap1.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
            BitmapData bmp8data = bitmap8.LockBits(new Rectangle(0, 0, bitmap8.Width, bitmap8.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            int height = bitmap1.Height;
            int width = bitmap1.Width;
            
            int stride1 = bmp1data.Stride;
            int stride8 = bmp8data.Stride;

            unsafe
            {
                byte* ptr1 = (byte*)bmp1data.Scan0.ToPointer();
                byte* ptr8 = (byte*)bmp8data.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index1 = stride1 * y + x / 8;
                        int index8 = stride8 * y + x;

                        byte b = (byte)(ptr1[index1] & (0x80 >> (x % 8)));
                        ptr8[index8] = (byte)(b > 0 ? 255 : 0);
                    }
                }
            }

            bitmap1.UnlockBits(bmp1data);
            bitmap8.UnlockBits(bmp8data);
            Console.WriteLine($"New Bitmap has {bitmap8.PixelFormat} Pixel format from 1bit");
            return bitmap8;
        }

        public static Bitmap AddPadding(Bitmap bitmap)
        {
            /*Bitmap bitmap = (Bitmap)o_bitmap.Clone(); //(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);) // New empty bitmap
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
                bitmap = ChangePixelFormat(o_bitmap);*/
            int oWidth = bitmap.Width;
            int oHeight = bitmap.Height;
            int pWidth = oWidth + 2;
            int pHeight = oHeight + 2;
            Bitmap paddedBmp = new Bitmap(pWidth, pHeight, PixelFormat.Format8bppIndexed);
            paddedBmp.Palette = DefineGrayPalette(paddedBmp);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, oWidth, oHeight),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData pBmpData = paddedBmp.LockBits(new Rectangle(0, 0, pWidth, pHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int offset = bmpData.Stride - oWidth;
            int nOffset = pBmpData.Stride - pWidth;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)pBmpData.Scan0.ToPointer();
                
                for (int i = 0; i < pHeight; i++)
                {
                    for (int j = 0; j < pWidth; j++)
                    {
                        if (i == 0 || j == 0 || j == pWidth - 1 || i == pHeight - 1 )
                            *nPtr = 0;
                        else
                        {
                            *nPtr = *ptr;   ptr++;
                        }
                        nPtr++;
                    }
                    ptr += offset;
                    nPtr += nOffset;
                }
            }
            
            bitmap.UnlockBits(bmpData);
            paddedBmp.UnlockBits(pBmpData);
            return paddedBmp;
        }
        public static void Dilate(int size)
        {
            //TODO: size input validation
            byte[,] sElement = new byte[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    sElement[i,j] = (byte)(j == 1 && i == 1 ? 0 : 1);
                    //Console.WriteLine(sElement[i,j]);
                }
            }
        }
        
        public static Bitmap Erode(Bitmap bitmap, int size)
        {
            byte[,] sElement = new byte[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //sElement[i,j] = (byte)(j == 1 && i == 1 ? 0 : 1);
                }
            }
            return null;
        }
    }
    
    public class ConvMatrix
    {
        public int TopLeft = 0, TopMid = 0, TopRight = 0;
        public int MidLeft = 0, Pixel = 1, MidRight = 0;
        public int BottomLeft = 0, BottomMid = 0, BottomRight = 0;
        public int Factor = 1;
        public int Offset = 0;

        public void SetAll(int nVal)
        {
            TopLeft = TopMid = TopRight = MidLeft = Pixel = MidRight =
                BottomLeft = BottomMid = BottomRight = nVal;
        }
        public void SetCrossShape()
        {
            TopLeft = TopRight = BottomLeft = BottomRight = 0;
            TopMid =  MidLeft = Pixel = MidRight = BottomMid = 1;
        }
        
        
        
    }
}