all:
	mkdir build
	cd CryptoNight; \
	g++ -shared blake256.cpp groestl.cpp hash.cpp hash-extra-blake.cpp hash-extra-groestl.cpp hash-extra-jh.cpp hash-extra-skein.cpp jh.cpp keccak.cpp oaes_lib.c skein.cpp cn-hash.cpp -o ../build/libCryptoNight.so -fPIC

	xbuild MoneroPool.mono.sln
	
	cp MoneroPool/bin/Debug/MoneroPool.exe build/MoneroPool.exe		
	cp MoneroPool/bin/Debug/Newtonsoft.Json.dll build/Newtonsoft.Json.dll
	cp MoneroPool/bin/Debug/StackExchange.Redis.md.dll build/StackExchange.Redis.md.dll	



