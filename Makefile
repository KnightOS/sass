# Makefile just to make life easier for those installing sass on unixes

XBUILD:=xbuild
DESTDIR:=/usr
PREFIX:=/usr

all: sass/bin/Debug/sass.exe

clean:
	rm -r sass/bin/
	rm -r sass/obj/

sass/bin/Debug/sass.exe: sass/*.cs
	xbuild

install:
	mkdir -p $(DESTDIR)/bin/
	mkdir -p $(DESTDIR)/mono/
	install -c -m 775 sass/bin/Debug/sass.exe $(DESTDIR)/mono/sass.exe
	echo -ne "#!/bin/sh\nmono $(PREFIX)/mono/sass.exe \$$*" > $(DESTDIR)/bin/sass
	chmod +x $(DESTDIR)/bin/sass

uninstall:
	rm $(DESTDIR)/bin/sass
	rm $(DESTDIR)/mono/sass.exe

.PHONY: all install uninstall clean
