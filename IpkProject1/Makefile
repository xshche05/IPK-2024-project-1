# Purpose: Makefile for the project in C# language.


all: build_linux clean

build_linux:
	dotnet publish -r linux-x64 -c Release -o ./out /p:PublishSingleFile=true /p:PublishTrimmed=true
	mv ./out/IpkProject1 ./ipk24chat-client

build_windows:
	dotnet publish -r win-x64 -c Release -o ./out /p:PublishSingleFile=true /p:PublishTrimmed=true
	mv ./out/IpkProject1.exe ./ipk24chat-client.exe

.PHONY: clean
clean:
	rm -fr ./out
	rm -fr ./obj
	rm -fr ./bin