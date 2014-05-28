
#include "cryptonote_boost_serialization.h"
#include <boost/foreach.hpp>



crypto::hash get_transaction_hash(const transaction& t)
  {
    crypto::hash h;
    size_t blob_size = 0;
    get_object_hash(t, h, blob_size);
    return h;
  }

 bool parse_and_validate_block_from_blob(const blobdata& b_blob, block& b)
  {
    std::stringstream ss;
    ss << b_blob;
    binary_archive<false> ba(ss);
    bool r = ::serialization::serialize(ba, b);
    if(!r)
		return false;//, "Failed to parse block from blob");
    return true;
  }

  blobdata get_block_hashing_blob(const block& b)
  {
    blobdata blob = t_serializable_object_to_blob(static_cast<block_header>(b));
    crypto::hash tree_root_hash = get_tx_tree_hash(b);
    blob.append((const char*)&tree_root_hash, sizeof(tree_root_hash ));
    blob.append(tools::get_varint_data(b.tx_hashes.size()+1));
    return blob;
  }

    void get_tx_tree_hash(const std::vector<crypto::hash>& tx_hashes, crypto::hash& h)
  {
	  tree_hash((const char (*) [32])tx_hashes.data(), tx_hashes.size(), h.data);
  }
  //---------------------------------------------------------------
  crypto::hash get_tx_tree_hash(const std::vector<crypto::hash>& tx_hashes)
  {
    crypto::hash h;
    get_tx_tree_hash(tx_hashes, h);
    return h;
  }
  //---------------------------------------------------------------
  crypto::hash get_tx_tree_hash(const block& b)
  {
    std::vector<crypto::hash> txs_ids;
    crypto::hash h =  get_transaction_hash(b.miner_tx);
    txs_ids.push_back(h);
    BOOST_FOREACH(auto& th, b.tx_hashes)
      txs_ids.push_back(th);
    return get_tx_tree_hash(txs_ids);
  }

  uint32_t convert_block(char* cblock, int length, char* convertedblock)
{
	blobdata blockin = std::string(cblock, length);

	block b;
    parse_and_validate_block_from_blob(blockin, b);

	blobdata out = get_block_hashing_blob(b);

	memcpy(convertedblock, out.data() , out.length());

	return out.length();
}