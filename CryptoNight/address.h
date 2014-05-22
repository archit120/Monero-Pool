 #ifndef __GNUC__
extern "C" __declspec(dllexport) uint32_t check_account_address(const char* str, uint32_t prefix);
#else
extern "C" uint32_t check_account_address(const char* str, uint32_t prefix);
#endif