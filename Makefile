# Purpose: Makefile for the project in C# language.
LOGIN = xshche05 

all: publish

publish:
	dotnet publish IpkProject1.csproj --ucr -c Release -o .

pack: dos2unix
	zip -r $(LOGIN).zip ./Enums ./Fsm ./Interfaces ./SysArg ./Tcp ./Udp ./User ./IpkProject1.csproj ./Program.cs ./README.md ./Makefile ./LICENSE ./CHANGELOG.md

dos2unix:
	dos2unix ./Enums/*.cs
	dos2unix ./Fsm/*.cs
	dos2unix ./Interfaces/*.cs
	dos2unix ./SysArg/*.cs
	dos2unix ./Tcp/*.cs
	dos2unix ./Udp/*.cs
	dos2unix ./User/*.cs
	dos2unix ./IpkProject1.csproj
	dos2unix ./Program.cs
	dos2unix ./README.md
	dos2unix ./Makefile
	dos2unix ./LICENSE
	dos2unix ./CHANGELOG.md

.PHONY: clean
clean:
	rm -fr ./out
	rm -fr ./obj
	rm -fr ./bin