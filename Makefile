# Makefile just to make life easier for those installing sass on unixes

ifeq ($(OS),Windows_NT)
XBUILD:=msbuild.exe
SASSPREFIX=
else
ifeq ($(OSTYPE),cygwin)
XBUILD:=msbuild.exe
SASSPREFIX=
else
XBUILD:=xbuild
SASSPREFIX=mono
endif
endif
DESTDIR:=/usr
PREFIX:=/usr

all: sass/bin/Debug/sass.exe

clean:
	rm -r sass/bin/
	rm -r sass/obj/

sass/bin/Debug/sass.exe: sass/*.cs
	$(XBUILD)

install:
	mkdir -p $(DESTDIR)/bin/
	mkdir -p $(DESTDIR)/mono/
	install -c -m 775 sass/bin/Debug/sass.exe $(DESTDIR)/mono/sass.exe
	echo -ne "#!/bin/sh\n$(SASSPREFIX) $(PREFIX)/mono/sass.exe \$$*" > $(DESTDIR)/bin/sass
	chmod +x $(DESTDIR)/bin/sass

uninstall:
	rm $(DESTDIR)/bin/sass
	rm $(DESTDIR)/mono/sass.exe

.PHONY: all install uninstall clean
