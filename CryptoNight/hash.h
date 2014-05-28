#ifndef __GNUC__
extern "C" __declspec(dllexport) void cn_slow_hash(char *data, uint32_t length, char *hash);
extern "C" __declspec(dllexport) void cn_fast_hash(char *data, uint32_t length, char *hash);
extern "C" __declspec(dllexport) void tree_hash(const char (*hashes)[32], size_t count, char *root_hash);
#else
extern "C" void cn_slow_hash(char *data, uint32_t length, char *hash);
extern "C" void cn_fast_hash(char *data, uint32_t length, char *hash);
extern "C" void tree_hash(const char (*hashes)[32], size_t count, char *root_hash);
#endif
