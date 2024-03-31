# Purpose: Makefile for the project in C# language.


all: publish

publish:
	dotnet publish IpkProject1.csproj --ucr --sc -c Release -o . -p:PublishSingleFile=true

.PHONY: clean
clean:
	rm -fr ./out
	rm -fr ./obj
	rm -fr ./bin