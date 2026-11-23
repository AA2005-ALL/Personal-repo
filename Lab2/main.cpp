#include <iostream>
#include <string>
#include <cstdlib>
#include "MainGame.h"

int main(int argc, char** argv) //argument used to call SDL main
{
	// --bench [count]: run each draw mode for a few seconds, print its line, then quit.
	bool bench = (argc > 1 && std::string(argv[1]) == "--bench");
	int count = (argc > 2) ? atoi(argv[2]) : 0;
	MainGame mainGame;
	mainGame.run(bench, count);

	return 0;
}