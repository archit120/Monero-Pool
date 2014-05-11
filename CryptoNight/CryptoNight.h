// CryptoNight.h

#pragma once

using namespace System;


#pragma managed
namespace CryptoNight
{
	public ref  class Crypto
	{
	public:
	static array<Byte> ^Crypto::CnSlowHash(array<Byte> ^data, int length);
	private:

	};
}
/*namespace CryptoNight {

	public ref class Crypto
	{
private:
public:
	static array<Byte> ^CnSlowHash(array<Byte> ^data, int length);
		// TODO: Add your methods for this class here.
	};
}
*/