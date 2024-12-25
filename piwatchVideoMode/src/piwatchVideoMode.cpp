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
#include <iostream>

#include <sys/types.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <sys/ioctl.h>
#include <fcntl.h>
#include <linux/fb.h>
#include <unistd.h>
#include <stdio.h>
#include <errno.h>

#include <signal.h>
#include <linux/kd.h>

#include <cstring>
#include <functional>
#include <map>
#include <sstream>

void log(std::stringstream &ss)
{
    std::cout << ss.str();
    ss.str("");
}

void setGraphicsMode()
{
    std::stringstream ss;

    ss << "Setting video mode to graphics\n";
    log(ss);

    int fdConsole;
    long int arg;
    if ((fdConsole = open("/dev/console", O_NOCTTY)) == -1)
    {
        ss << "open " << strerror(errno) << "\n";
        log(ss);
        exit(-1);
    }
    if (ioctl(fdConsole, KDSETMODE, KD_GRAPHICS) == -1)
    {
        ss << "ioctl KDSETMODE " << strerror(errno) << "\n";
        log(ss);
    }
    if ((ioctl(fdConsole, KDGETMODE, &arg)) == -1)
    {
        ss << "ioctl " << strerror(errno) << "\n";
        log(ss);
        close(fdConsole);
        return;
    }

    if (arg == KD_TEXT)
    {
        ss << "Console in text mode" << "\n";
        log(ss);
    }
    else if (arg == KD_GRAPHICS)
    {
        ss << "Console is in graphics mode" << "\n";
        log(ss);
    }
    else
    {
        ss << "Unexpected video mode " << arg << "\n";
        log(ss);
    }
    close(fdConsole);
}

std::ostream& operator<<(std::ostream& out, const fb_var_screeninfo& in)
{
    out << "xres " << in.xres
            << " yres " << in.yres
            << " bits_per_pixel " << in.bits_per_pixel;
    return out;
}

void printFrameBufferInfo()
{
    std::stringstream ss;
    ss << "Getting frame buffer details\n";
    log(ss);

    int fd = open("/dev/fb0", O_RDWR);

    struct fb_var_screeninfo screeninfo;
    ioctl(fd, FBIOGET_VSCREENINFO, &screeninfo);

    ss << screeninfo << "\n";
    log(ss);
}

int main(int argc, char** argv)
{
    std::stringstream ss;
    ss << program_invocation_short_name << " running\n";
    log(ss);

    const std::map<std::string, std::function<void()>> commands =
    {
        {"-g", &setGraphicsMode},
        {"-i", &printFrameBufferInfo},
    };

    for (int i = 1; i < argc; ++i)
    {
        ss << "Argument " << argv[i] << "\n";
        log(ss);
        if (auto it = commands.find(argv[i]); it != commands.end())
        {
            it->second();
        }
    }

    return 0;
}
