// Copyright (c) 2012-2013 The Cryptonote developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

#include <assert.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include <string>

#include "base58.h"

  struct public_key {
    char data[32];
  };



struct account_public_address
  {
    public_key m_spend_public_key;
    public_key m_view_public_key;

  };

  struct public_address_outer_blob
  {
    uint8_t m_ver;
    account_public_address m_address;
    uint8_t check_sum;
  };

  bool check_account_address(const std::string& str)
  {
    if (2 * sizeof(public_address_outer_blob) != str.size())
    {
		std::string data;
      uint64_t prefix;
      if (!tools::base58::decode_addr(str, prefix, data))
      {
		 return false;
      }

      /*if (CRYPTONOTE_PUBLIC_ADDRESS_BASE58_PREFIX != prefix)
      {
        LOG_PRINT_L1("Wrong address prefix: " << prefix << ", expected " << CRYPTONOTE_PUBLIC_ADDRESS_BASE58_PREFIX);
        return false;
      }*/

	  //Let's not check for prefix, makes the pool usable for other coins too

/*      if (!::serialization::parse_binary(data, adr))
      {
        LOG_PRINT_L1("Account public address keys can't be parsed");
        return false;
      }

      if (!crypto::check_key(adr.m_spend_public_key) || !crypto::check_key(adr.m_view_public_key))
      {
        LOG_PRINT_L1("Failed to validate address keys");
        return false;
      }
    }
    else
    {
      // Old address format
      std::string buff;
      if(!string_tools::parse_hexstr_to_binbuff(str, buff))
        return false;

      if(buff.size()!=sizeof(public_address_outer_blob))
      {
        LOG_PRINT_L1("Wrong public address size: " << buff.size() << ", expected size: " << sizeof(public_address_outer_blob));
        return false;
      }

      public_address_outer_blob blob = *reinterpret_cast<const public_address_outer_blob*>(buff.data());


      if(blob.m_ver > CRYPTONOTE_PUBLIC_ADDRESS_TEXTBLOB_VER)
      {
        LOG_PRINT_L1("Unknown version of public address: " << blob.m_ver << ", expected " << CRYPTONOTE_PUBLIC_ADDRESS_TEXTBLOB_VER);
        return false;
      }

      if(blob.check_sum != get_account_address_checksum(blob))
      {
        LOG_PRINT_L1("Wrong public address checksum");
        return false;
      }

      //we success
      adr = blob.m_address;*/
    }

    return true;
  }