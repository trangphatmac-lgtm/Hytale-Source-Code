using System;
using HytaleClient.Core;
using SDL2;

namespace HytaleClient.Graphics;

public class Cursors : Disposable
{
	public readonly IntPtr Arrow;

	public readonly IntPtr IBeam;

	public readonly IntPtr Hand;

	public readonly IntPtr Move;

	public readonly IntPtr SizeWE;

	public readonly IntPtr SizeNS;

	public Cursors()
	{
		Arrow = SDL.SDL_CreateSystemCursor((SDL_SystemCursor)0);
		IBeam = SDL.SDL_CreateSystemCursor((SDL_SystemCursor)1);
		Hand = SDL.SDL_CreateSystemCursor((SDL_SystemCursor)11);
		Move = SDL.SDL_CreateSystemCursor((SDL_SystemCursor)9);
		SizeWE = SDL.SDL_CreateSystemCursor((SDL_SystemCursor)7);
		SizeNS = SDL.SDL_CreateSystemCursor((SDL_SystemCursor)8);
	}

	protected override void DoDispose()
	{
		SDL.SDL_FreeCursor(Hand);
		SDL.SDL_FreeCursor(IBeam);
		SDL.SDL_FreeCursor(Hand);
		SDL.SDL_FreeCursor(Move);
		SDL.SDL_FreeCursor(SizeWE);
		SDL.SDL_FreeCursor(SizeNS);
	}
}
