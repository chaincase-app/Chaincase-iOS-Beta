# Chaincase - The Private way to Bitcoin
A non-custodial iOS bitcoin wallet supporting [Chaumian CoinJoin](https://github.com/nopara73/ZeroLink/#ii-chaumian-coinjoin).

The main privacy features on the network level:
- Tor-only by default.
- BIP 158 block filters for private light client.

and on the blockchain level:
- Intuitive ZeroLink CoinJoin integration.
- Superb coin selection and labeling.
- Dust attack protections.

special thanks to [Wasabi](https://github.com/zkSNACKs/WalletWasabi) for making this possible

## [Download Chaincase on iOS TestFlight](https://testflight.apple.com/join/e31v3Ydj)

Chaincase is a Xamarin.Forms application built on top of the famed Wasabi Wallet privacy wallet.

It binds to [Tor.framework](https://github.com/iCepa/Tor.framework) for an anonymous connection to the outside world

This is *experimental* beta software. Your feedback is greatly appreciated 🗽

## Building for iOS

make sure to have the Wasabi submodule installed:
```console
git submodule update --init --recursive
```

pull the Tor binary:
```console
git lfs pull
```

And install a provisioning profile to make use of the entitlements:
https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/device-provisioning/
