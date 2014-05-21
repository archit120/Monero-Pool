// Copyright (c) 2012-2013 The Cryptonote developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

#include <assert.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include <string>

#include "address.h"
#include "base58.h"

  bool isValid(std::string address,uint64_t prefix)
  {
	  if(address.length() != 95)
		  return false;
	  std::string decoded;
	  uint64_t pre;

	  bool abc = tools::base58::decode_addr(address, pre,decoded);
		 if(!abc)
			 return false;
      if (pre != prefix)
	  {
		  return false;
	  }
	  return true;
  }

  uint32_t check_account_address(const char* str, uint32_t prefix)
  {
	 if(isValid(std::string(str), prefix))
		 return 1;
	 return 0;
	 //Take no risks with P/Invoke
  }