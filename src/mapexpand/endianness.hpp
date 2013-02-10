#pragma once

// Returns true if running on a big-endian CPU
static bool cpuIsBigEndian()
{
	union
	{
		uint32_t i;
		uint8_t b[4];
	} u;

	u.i = 1;
	return (u.b[3] == 1);
}

// Begin compiler-specific crap
#ifdef _MSC_VER
#include <intrin.h>
#define SWAP16(x) _byteswap_ushort(x)
#define SWAP32(x) _byteswap_ulong(x)
#elif __GNUC__
#define SWAP16(x) (((x) << 8) | ((x) >> 8))
#define SWAP32(x) __builtin_bswap32(x)
#else
#error "Compiler is not yet supported"
#endif
// End compiler-specific crap