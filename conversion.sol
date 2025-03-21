// SPDX-License-Identifier: MIT
pragma solidity >=0.6.0 <0.8.0;
pragma abicoder v2;

interface IConversionContract {
    event OnRequestConversion(
        address indexed quantumAddress,
        string ethAddress,
        string ethereumSignature
    );

    event OnSubmitBurnProof(
        address indexed submitterAddress
    );
}

contract ConversionContract is IConversionContract {
    
     struct ConversionRequest {
        string ethAddress;
        string ethSignature;
    }

    ConversionRequest[] public ConversionRequests;
    string[] public BurnProofs;

    function requestConversion(string calldata ethAddress, string calldata ethSignature) external returns (uint8) {
        ConversionRequests.push(ConversionRequest(ethAddress, ethSignature)); //just a request, anyone can request
        emit OnRequestConversion(msg.sender, ethAddress, ethSignature);
        return 0;
    }

    function submitBurnproof(string calldata burnProof) external returns (uint8) {
        BurnProofs.push(burnProof); //anyone can submit a burn proof
        emit OnSubmitBurnProof(msg.sender);
        return 0;
    }
}
