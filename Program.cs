using SDL2;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SDLtest
{
    class Program
    {
        static void Main(string[] args)
        {
            bool quit = false;

            ConcurrentStack<int> stack = new ConcurrentStack<int>();

            // Pointless 2nd thread
            Task thread = Task.Run(async () =>
            {
                Random r = new Random();

                while (!quit)
                {
                    await Task.Delay(2000);
                    stack.Push(r.Next(0, 19236));
                }

                Console.WriteLine("Thread exit");
            });

            if (SDL.SDL_Init(
                SDL.SDL_INIT_VIDEO |
                SDL.SDL_INIT_TIMER |
                SDL.SDL_INIT_JOYSTICK |
                SDL.SDL_INIT_HAPTIC |
                SDL.SDL_INIT_GAMECONTROLLER |
                SDL.SDL_INIT_EVENTS |
                SDL.SDL_INIT_NOPARACHUTE) != 0)
            {
                Console.WriteLine($"Failed to initialise SDL: {SDL.SDL_GetError()}");
                Environment.ExitCode = 1;
                return;
            }

            IntPtr window = SDL.SDL_CreateWindow("test", 
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                1280, 
                720,
                (SDL.SDL_WindowFlags)0);

            IntPtr renderer = SDL.SDL_CreateRenderer(
                window,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | 
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC); // FYI: Disabling vsync will amp up CPU use significantly for most machines

            if (SDL.SDL_GetRendererInfo(renderer, out SDL.SDL_RendererInfo renderInfo) == 0)
            {                
                Console.WriteLine(Marshal.PtrToStringAnsi(renderInfo.name));                
            }

            if (SDL_ttf.TTF_Init() != 0)
            {
                Console.WriteLine("Failed to initialise TTF");
                Environment.ExitCode = 1;
                return;
            }

            IntPtr font = SDL_ttf.TTF_OpenFont("test.ttf", 24);
            
            if (font == IntPtr.Zero)
            {
                Console.WriteLine("Failed to load font!");
                Environment.ExitCode = 1;
                return;
            }

            SDL.SDL_Color colour = default(SDL.SDL_Color);
            colour.a = 255;
            colour.r = 255;    
            
            while(!quit)
            {
                while(SDL.SDL_PollEvent(out SDL.SDL_Event current) != 0)
                {
                    if (current.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        quit = true;
                    }
                }

                SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
                SDL.SDL_RenderClear(renderer);

                if (stack.TryPeek(out int found))
                {
                    RenderText(renderer, font, $"RNG from other thread: {found}", 500, 300, ref colour, 100);
                }
                else
                {
                    RenderText(renderer, font, $"Waiting...", 500, 300, ref colour, 100);
                }


                SDL.SDL_RenderPresent(renderer);
            }

            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
            thread.Wait();
        }

        private static void RenderText(IntPtr renderer, IntPtr font, 
            string text, int x, int y, ref SDL.SDL_Color colour, uint width)
        {
            // This is horribly inefficient to do per-frame it's just an example
            //

            IntPtr surface = SDL_ttf.TTF_RenderUTF8_Blended_Wrapped(font, text, colour, width);

            IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, surface);

            SDL.SDL_QueryTexture(texture, out _, out _, out int drawn_width, out int drawn_height);

            SDL.SDL_Rect src;
            src.x = 0;
            src.y = 0;
            src.w = drawn_width;
            src.h = drawn_height;

            SDL.SDL_Rect rect;
            rect.x = x;
            rect.y = y;
            rect.w = drawn_width;
            rect.h = drawn_height;

            SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref rect);

            SDL.SDL_RenderCopyEx(renderer, texture, ref src, ref rect, 0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
            SDL.SDL_FreeSurface(surface);
            SDL.SDL_DestroyTexture(texture);
        }
    }
}
