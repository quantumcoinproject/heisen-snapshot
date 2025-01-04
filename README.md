# Heisen Snapshot
As a prerequisite for Heisen launch, a snapshot of addresses that had ERC20 DogeP tokens as of cut-off block [21518735](https://etherscan.io/block/21518735) had to be created (Dec-31-2024 12:00:11 AM +UTC). This list is read from the Ethereum blockchain. This repository contains code that calculates this list from the Ethereum blockchain and calculates the proportion of Heisen Tokens (1:1). The code also optionally outputs this data to a csv file and a html file from a template.

The generated html is available at the https://snapshot.quantumcoin.org

# How is the list of addresses containing the tokens generated?
As part of DogeP ERC20 smart contract, standard transfer events are emitted. Using the [Nethereum .NET library](https://nethereum.com), these events are read from the Ethereum blockchain using JSON APIs. The starting block from which DogeP tokens were created and the cut-off block is passed as parameters. These events are used to calculate the addresses and their token count as of the cut-off block. All the values are configurable in config files. This method should work for any ERC20 token in general, as long as the specific token's smart contracts emits the transfer events properly.

