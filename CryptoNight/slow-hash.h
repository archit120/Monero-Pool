#ifndef __GNUC__
extern "C" __declspec(dllexport) void cn_slow_hash(char *data, size_t length, char *hash);
#else
extern "C" void cn_slow_hash(char *data, size_t length, char *hash);
#endif
