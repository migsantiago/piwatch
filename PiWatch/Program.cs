/*
    MIT License

    Copyright (c) 2024 Santiago Villafuerte - migsantiago.com

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using cyrruspi.Properties;
using System.Threading;
using System.Runtime.InteropServices;

namespace cyrruspi
{
    unsafe class Program
    {
        const Int32 SCREEN_WIDTH = 800;
        const Int32 SCREEN_HEIGHT = 480;

        static int bitsPerPixel = 16;

        static void Main(string[] args)
        {
            Console.WriteLine("PiWatch 2.0 - migsantiago.com");
            while (true)
            {
                drawBitmap();
                Thread.Sleep(1000);
            }
        }

        static String getMonth(int month)
        {
            String monthString = "unknown";
            switch (month)
            {
                case 1: monthString = "January"; break;
                case 2: monthString = "February"; break;
                case 3: monthString = "March"; break;
                case 4: monthString = "April"; break;
                case 5: monthString = "May"; break;
                case 6: monthString = "June"; break;
                case 7: monthString = "July"; break;
                case 8: monthString = "August"; break;
                case 9: monthString = "September"; break;
                case 10: monthString = "October"; break;
                case 11: monthString = "November"; break;
                case 12: monthString = "December"; break;
                default: break;
            }
            return monthString;
        }

        static string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

        static void drawBitmap()
        {
            Bitmap bmp;

            var timeNow = DateTime.Now;
            var brush = Brushes.Blue;

            if ((timeNow.Hour >= 7) && (timeNow.Hour < 18))
            {
                bmp = new Bitmap(Resources.daySky);
            }
            else
            {
                bmp = new Bitmap(Resources.nightSky);
                brush = Brushes.FloralWhite;
            }

            RectangleF rectf = new RectangleF(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);

            Graphics g = Graphics.FromImage(bmp);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Coming Soon font:
            // https://fonts.google.com/specimen/Coming+Soon?query=coming+soon
            // Decompress the ZIP directory into:
            // /usr/share/fonts/
            // Example:
            //pi @raspberrypi:~ $ ls -la /usr/share/fonts/ Coming_Soon
            //total 68
            //drwxr-xr-x 2 root root  4096 Dec 24 17:50.
            //drwxr-xr-x 9 root root  4096 Dec 24 17:50..
            //-rwxr--r-- 1 root root 47128 Dec 24 17:50 ComingSoon - Regular.ttf
            //-rwxr--r-- 1 root root 11560 Dec 24 17:50 LICENSE.txt

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            var time = DateTime.Now;
            String hourString = ((time.Hour == 0) ? ("12") : time.Hour.ToString("D2"));
            g.DrawString(hourString + ":" + time.Minute.ToString("D2"), new Font("Coming Soon", 180, FontStyle.Bold), brush, rectf, sf);

            rectf.Y = 270;
            g.DrawString(time.DayOfWeek.ToString(), new Font("Coming Soon", 50), brush, rectf, sf);

            rectf.Y += 90;
            g.DrawString(getMonth(time.Month) + " " + time.Day.ToString("D2") + GetDaySuffix(time.Day) + " " + time.Year.ToString(), new Font("Coming Soon", 50), brush, rectf, sf);

            g.Flush();

            // Convert the bitmap into raw data that the frame buffer expects
            var raw = convertTo16BPP(bmp);

            // Write the frame buffer
            writeFrameBuffer(raw);
        }

        static UInt16[,] convertTo16BPP(Bitmap bmp)
        {
            UInt16[,] array = new UInt16[SCREEN_WIDTH, SCREEN_HEIGHT];

            for (int row = 0; row < SCREEN_HEIGHT; row++)
            {
                for (int column = 0; column < SCREEN_WIDTH; column++)
                {
                    // For 32 bits per pixel
                    //UInt32 pixel =
                    //    (UInt32)(bmp.GetPixel(column, row).R << 16)
                    //    | (UInt32)(bmp.GetPixel(column, row).G << 8)
                    //    | (UInt32)(bmp.GetPixel(column, row).B);

                    // 16 bits per pixel -> RRRRRGGG GGGBBBBB (5 bits red, 6 bits green, 5 bits blue)
                    //pi @raspberrypi:~ $ fbset - i
                    //mode "480x800"
                    //    geometry 480 800 480 800 16
                    //    timings 0 0 0 0 0 0 0
                    //    rgba 5 / 11,6 / 5,5 / 0,0 / 0
                    //endmode

                    var fullPixel = bmp.GetPixel(column, row);
                    UInt16 pixel =
                        (UInt16)
                        ((UInt16)((fullPixel.R & 0xF8) << 8)
                        | (UInt16)((fullPixel.G & 0xFC) << 3)
                        | (UInt16)(fullPixel.B >> 3));

                    array[column, row] = pixel;
                }
            }

            return array;
        }

        // Linux stuff
        static int O_RDWR = 2;
        static int PROT_READ = 1;
        static int PROT_WRITE = 2;
        static int MAP_SHARED = 1;

        [DllImport("/usr/lib/arm-linux-gnueabihf/libc.so.6")]
        extern static int open(string pathname, int flags);

        [DllImport("/usr/lib/arm-linux-gnueabihf/libc.so.6")]
        extern static void* mmap(void *addr, int length, int prot, int flags, int fd, int offset);

        [DllImport("/usr/lib/arm-linux-gnueabihf/libc.so.6")]
        extern static int munmap(void *addr, int length);

        [DllImport("/usr/lib/arm-linux-gnueabihf/libc.so.6")]
        extern static int close(int fd);

        static void writeFrameBuffer(UInt16[,] raw)
        {
            // Linux C Code:
            // int fd = open("/dev/fb0", O_RDWR);
            //data = (unsigned int*) mmap(0, width * height * bytespp, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
            //std::cout << "Width " << width << " height " << height << " bytes per pixel " << bytespp << std::endl;
            // AARRGGBB Format
            //for (row = 0; row < height; row++)
            //    for (col = 0; col < width; col++)
            //        data[row * width + col] = rawImageAARRGGBB[row * width + col];
            //munmap(data, width * height * bytespp);
            //close(fd);

            // Important: You should have set the /dev/console to graphics mode from the C binary before doing this!
            // ./piwatchVideoMode -i -g

            int fd = open("/dev/fb0", O_RDWR);

            UInt16* data;
            data = (UInt16*)mmap((void *)0, SCREEN_WIDTH * SCREEN_HEIGHT * bitsPerPixel, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);

            int destinationRow;
            for (int sourceRow = 0; sourceRow < SCREEN_HEIGHT; sourceRow++)
            {
                destinationRow = sourceRow;
                int destinationColumn = 799;

                for (int sourceColumn = 0; sourceColumn < SCREEN_WIDTH; sourceColumn++)
                {
                    UInt16 pixel = raw[sourceColumn, sourceRow];
                    int location = (destinationColumn * SCREEN_HEIGHT) + destinationRow;

                    data[location] = pixel;

                    --destinationColumn;

                    //data[(row * SCREEN_HEIGHT) + col] = pix++; // Do this to see how pixels are drawn as it's slowed down by the log below
                    //Console.WriteLine("Row " + row + " col " + col + " = " + data[(row * SCREEN_HEIGHT) + col].ToString("X4"));
                }
            }

            // Do this to find the coordinates of the screen (use a magnifying glass!)
            //data[0] = 0xF800; // red
            //data[479] = 0x07E0; // green
            //data[383520] = 0x001F; // blue
            //data[383999] = 0xFFFF; // white

            // In my Hyperpixel4, these are the coordinates (rotate=270 in /boot/firmware/config.txt)
            // Top Left = 383520 = (799 x 480) + 0
            // Top Right = 0 = (799 x 0) + 0
            // Bottom Left = 383999 = (799 x 480) + 479
            // Bottom Right = 479 = (799 x 0) + 479

            munmap(data, SCREEN_WIDTH * SCREEN_HEIGHT * bitsPerPixel);

            close(fd);
        }
    }
}
