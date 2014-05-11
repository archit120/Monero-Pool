// This is the main DLL file.
#include <stdio.h>
#include <malloc.h>

#include "hash-ops.h"
#include "CryptoNight.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;
using namespace CryptoNight;
namespace CryptoNight
{
	array<Byte> ^Crypto::CnSlowHash(array<Byte> ^data, int length)
	{
		Stopwatch ^stop1 = gcnew Stopwatch();
		Stopwatch ^stop2 = gcnew Stopwatch();

		stop1->Start();
		char * cdata = (char *)malloc(length);
		Marshal::Copy(data, 0, (IntPtr)cdata, length);

		char* chash = (char*)malloc(32);
		stop2->Start();

		cn_slow_hash(cdata, length, chash);
				stop2->Start();

		array<Byte> ^hash = gcnew array<Byte> (32);
		Marshal::Copy((IntPtr)chash, hash, 0, 32);

		free(chash);
		free(cdata);
		stop1->Stop();

		Console::WriteLine("{0} vs {1}", stop1->ElapsedMilliseconds, stop2->ElapsedMilliseconds);
		return hash;
	} 
}

	
