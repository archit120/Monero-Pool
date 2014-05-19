// Copyright (c) 2012-2013 The Cryptonote developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

#pragma once

#include <stdint.h>
#include <stdlib.h>
union hash_state {
  uint8_t b[200];
  uint64_t w[25];
};

void cn_slow_hash(const void *data, size_t length, char *hash);

void hash_permutation(union hash_state *state);
void hash_process(union hash_state *state, const uint8_t *buf, size_t count);

void hash_extra_blake(const void *data, size_t length, char *hash);
void hash_extra_groestl(const void *data, size_t length, char *hash);
void hash_extra_jh(const void *data, size_t length, char *hash);
void hash_extra_skein(const void *data, size_t length, char *hash);

enum {
  HASH_SIZE = 32,
  HASH_DATA_AREA = 136
};