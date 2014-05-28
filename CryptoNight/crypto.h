// Copyright (c) 2012-2013 The Cryptonote developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

#pragma once

#include <cstddef>
#include <mutex>
#include <vector>
#include <string>

#include "hash.h"
namespace crypto {

#pragma pack(push, 1)

#define HASH_SIZE 32

	  struct hash {
    char data[HASH_SIZE];

  };

  struct ec_point {
    char data[32];
  };

  struct ec_scalar {
    char data[32];
  };

  struct public_key: ec_point {
    friend class crypto_ops;
  };

  struct secret_key: ec_scalar {
    friend class crypto_ops;
  };

  struct key_derivation: ec_point {
    friend class crypto_ops;
  };

  struct key_image: ec_point {
    friend class crypto_ops;
  };

  struct signature {
    ec_scalar c, r;
    friend class crypto_ops;
  };
#pragma pack(pop)
}