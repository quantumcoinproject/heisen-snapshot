# Mainnet Snapshot
As a prerequisite for DP mainnet launch, a snapshot of addresses that had ERC20 DogeP tokens as of cut-off block [17595510](https://etherscan.io/block/17595510) had to be created. This list is read from the Ethereum blockchain. This repository contains code that calculates this list from the Ethereum blockchain and calculates the proportion of DP coins based on the [blockchain allocation whitepaper](https://dogeprotocol.org/whitepapers/Doge-Protocol-Blockchain-Allocation-Whitepaper.pdf). The code also optionally output this data to a csv file and a html file from a template.

The generated html is available at the https://snapshot.dpscan.app

# How the token addresses are generated?
As part of DogeP ERC20 smart contract, standard transfer events are emitted. Using the [Nethereum .NET library](https://nethereum.com), these events are read from the Ethereum blockchain using JSON APIs. The starting block from which DogeP tokens were created and the cut-off block is passed as parameters. These events are used to calculate the addresses and their token count as of the cut-off block. All the values are configurable in config files. This method should work for any ERC20 token in general, as long as the specific token's smart contracts emits the transfer events properly.

