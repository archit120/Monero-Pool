CXX = g++
CC = cc
#CXXFLAGS = -Wall -Wextra -std=c++0x -O2 -fomit-frame-pointer $(EXTRA_CXXFLAGS)
ifeq ($(OSVERSION),CYGWIN_NT-6.1)
	#CXXFLAGS = -Wall -Wextra -std=gnu++0x -O2 -fomit-frame-pointer 
	CXXFLAGS = -std=gnu++0x -O2 -fomit-frame-pointer -D_BSD_SOURCE $(EXTRA_CFLAGS)
	CFLAGS = -std=gnu++0x -O2 -fomit-frame-pointer -D_BSD_SOURCE $(EXTRA_CFLAGS)
endif

#CFLAGS = -Wall -Wextra -O2 -fomit-frame-pointer $(EXTRA_CFLAGS)

OSVERSION := $(shell uname -s)
#LIBS = -lcrypto -lssl -pthread  -ldl 
ifeq ($(OSVERSION),Linux)
#	LIBS += -lrt
#	CFLAGS += -march=native 
#	CXXFLAGS += -march=native
endif

ifeq ($(OSVERSION),FreeBSD)
	CXX = clang++
	CC = clang
	CFLAGS += -DHAVE_DECL_LE32DEC -march=native
	CXXFLAGS += -DHAVE_DECL_LE32DEC -march=native
endif

# You might need to edit these paths too
LIBPATHS = -L/usr/local/lib -L/usr/lib -L/DBA/openssl/1.0.1f/lib/ 
INCLUDEPATHS = -I/usr/local/include -I/usr/include -Iincludes/ -IOpenCL 

ifeq ($(OSVERSION),Darwin)
	EXTENSION = -mac
	GOT_MACPORTS := $(shell which port)
ifdef GOT_MACPORTS
	LIBPATHS += -L/opt/local/lib
	INCLUDEPATHS += -I/opt/local/include
endif
else
       EXTENSION = .dll

endif
ifeq ($(OSVERSION),CYGWIN_NT-6.1)
	EXTENSION = .exe
#	LIBS += -lOpenCL
	LIBPATHS += -L/cygdrive/c/Program\ Files\ \(x86\)/AMD\ APP\ SDK/2.9/lib/x86_64/
	INCLUDEPATHS += -I/cygdrive/c/Program\ Files\ \(x86\)/AMD\ APP\ SDK/2.9/include
endif

OBJS = \
        blake256.o \
	groestl.o  \
	hash-extra-blake.o \
	hash-extra-groestl.o \
	hash-extra-jh.o \
	hash-extra-skein.o \
	hash.o \
	oaes_lib.o \
	jh.o \
	keccak.o \
	oaes_lib.o \
	skein.o \
	slow-hash.o 

ifeq ($(ENABLE_OPENCL),1)
	OBJS += OpenCLObjects.o 
        OBJS += openCL.o
	CXXFLAGS += -DUSE_OPENCL
	LIBS += -lOpenCL
else

endif

all: CryptoNight$(EXTENSION)

%.o: %.cpp
	$(CXX) -fPIC -c $(CXXFLAGS) $(INCLUDEPATHS) $< -o $@ 

%.o: %.c
	$(CC) -fPIC -c $(CFLAGS) $(INCLUDEPATHS) $< -o $@ 

CryptoNight$(EXTENSION): $(OBJS:%=%) 
	$(CXX) -shared -Wl,-soname,CryptoNight.dll $(CFLAGS) $(LIBPATHS) $(INCLUDEPATHS) -o $@ $^ $(LIBS) -flto
clean:
	-rm -f *.o
	-rm -f CrytoNight$(EXTENSION)
	
