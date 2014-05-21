 #ifndef __GNUC__
extern "C" __declspec(dllexport) uint32_t check_account_address(const char* str, uint32_t prefix);
#else
uint32_t check_account_address(const char* str, uint32_t prefix);
#endif