Monero-Pool
===========

An open source Crypto Night Pool written in C# and C++ for maximum speed and efficiency for coins such as Monero and Bytecoin.  Uses Redis for a high performance database. Also comes with a full fledged front end.


#### Index
* [Features](#features)
* [Usage](#usage)
  * [Requirements](#requirements)
  * [Downloading & Installing](#1-downloading--installing)
  * [Configuration](#2-configuration)
  * [Starting the Pool](#3-start-the-pool)
  * [Upgrading](#upgrading)
* [Setting up Testnet](#setting-up-testnet)
* [Credits](#credits)

#### Features
* A beautiful front end to amaze your visitors.
* Highly scalable pool server which tries to get the maximum juice out of multiple cores of your server without using Synchronous blocking.
* Extremely detailed logging with variable settings.
* Extremely intricate error management so that the server never crashes out on you.
* Compatibility with all major OSes.
* IP banning.
* Payment processing.
* Extremely detailed database.
* Front end with individual miner statistics and beautiful charts.
* Address validation.
* Vardiff support
* Live stats
* TCP based Stratum like protocol

===

#### Requirements
* Coin daemon and wallet daemon

#### On Linux:
* [G++](https://gcc.gnu.org/) compiler and [Mono 3.2+](http://www.mono-project.com/Main_Page) 
* [Redis](http://redis.io/) key-value store

#### On Windows:
* [Visual Studio 2012+] (http://www.visualstudio.com/) 
* [.Net framework 4.5] (http://www.microsoft.com/en-in/download/details.aspx?id=30653) 
* [Redis](http://redis.io/) key-value store

#### 1) Downloading & Installing

##### Compiling from source
##### On Linux:
Clone this repository locally and run make
```bash
git clone https://github.com/archit120/Monero-Pool.git
cd Monero-Pool
make
```

The resulting binaries can be found in /build/

##### On Windows:
Compile the C++ part of the solution then compile the C# project named Monero-Pool. After doing this copy the C++ dll to same folder as your pool binaries.


##### Binaries
Binary releases can be found on the repository.

#### 2) Configuration

Have a look the sample config.txt given and configure it to your liking.

Sample config
```
#Daemon RPC address
daemon-json-rpc=http://127.0.0.1:18081
#Wallet RPC address
wallet-json-rpc=http://127.0.0.1:8082
#Http Server address with port
http-server=http://*:7707/
#Pool wallet address
wallet-address=41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV
#Redis database server address
redis-server=127.0.0.1
#Redis database Id
redis-database=0
#Number of seconds before removing a miner without getting a response
client-timeout-seconds=60
#Starting difficulty for all miners
base-difficulty=150
#Ban when this percentage of shares are invalid
ban-reject-percentage=50
#Minutes to ban the miner for
ban-time-minutes=60
#Block depth required for transactions to occur
block-confirms=60
#Percentage of pool fee
pool-fee=5
#Base 58 prefix for address
base58-prefix=18
#Enable banning after this many shares
ban-after-shares=10
#Target time for shares
vardiff-targettime-seconds=15
#vardiff deviation from target time allowed
vardiff-targettime-deviation-allowed=5
#Percentage of maximum difficulty change in one step
vardiff-targettime-maxdeviation=10
#Maximum difficulty
vardiff-max-difficulty=15000
#Maximum concurrent works to miner
max-concurrent-works=15
#Tcp server port
stratum-server-port=7706
#Retarget difficulty for miners every this seconds
diffiulty-retarget=5
```

#### 3) Start the pool

##### On Linux:

```bash
mono MoneroPool.exe
```

##### On Windows:
```bash
MoneroPool.exe
```

#### 4) Configure the front-end

Edit the config.json
```javascript
{"scheme":"tcp","host":"192.168.1.4","port":6379}
```
Download and extract [Predis](https://github.com/nrk/predis) to the root directroy of front end. Host with a webserver capable of serving [PHP](http://www.php.net/) files.

#### Upgrading
When upgrading it is highly recommended to wait for an official release. Since the pool is still in extremely beta stage many big changes may be implemented so follow the step listed in release carefully while upgrading.

### Setting up Testnet

No cryptonote based coins have a testnet mode (yet) but you can effectively create a testnet with the following steps:

* Open `/src/p2p/net_node.inl` and remove lines with `ADD_HARDCODED_SEED_NODE` to prevent it from connecting to mainnet (Monero example: http://git.io/0a12_Q)
* Build the coin from source
* You now need to run two instance of the daemon and connect them to each other (without a connection to another instance the daemon will not accept RPC requests)
  * Run first instance with `./coind --p2p-bind-port 28080 --allow-local-ip`
  * Run second instance with `./coind --p2p-bind-port 5011 --rpc-bind-port 5010 --add-peer 0.0.0.0:28080 --allow-local-ip`
* You should now have a local testnet setup. The ports can be changes as long as the second instance is pointed to the first instance, obviously

*Credit to surfer43 for these instructions*

Credits
===
* [archit](https://github.com/archit120) - Developer of the project.
* [zone117x](https://github.com/zone117x) - Difficulty->Target and the README.
* Everyone who helped me with using Linux on IRC.
