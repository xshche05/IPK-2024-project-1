# Purpose: Makefile for the project in C# language.


all: build clean

build:
	dotnet publish -r linux-x64 -c Release -o ./out /p:PublishSingleFile=true /p:PublishTrimmed=true
	mv ./out/IpkProject1 ./ipk24chat-client

.PHONY: clean
clean:
	rm -fr ./out
	rm -fr ./obj
	rm -fr ./bin