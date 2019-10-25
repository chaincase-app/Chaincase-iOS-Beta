# Chaincase
Xamarin.Forms WasabiWallet implementation

https://github.com/zkSNACKs/WalletWasabi

Wasabi wallet is built on .NET Core. .NET Core is meant for console applications and so has a hacked-on-top UI.
I see wasabidaemon eventually being a .NET standard library with the presentation layer on top. UX 相處得很好 will help foster adoption.

https://github.com/lassana/XamarinFormsPinView This may be a place to stop to encrypt the wallet
https://github.com/iCepa/Tor.framework I think this is the only way Tor can be incorporated into an application in iOS at the moment.

# Build
after you clone the repository, don't forget to

```console
git submodule update --init --recursive
```

The macOS version should be stable to build and recieve coins.

To use testnet, go to `/Users/<youruser>/.xwasabi/Client/Config.json` and make sure the Network line reads: `Network": "TestNet`. Beware, anything lower than the `DustThreshold` amount will not show up in the UI.

_This software is experimental. Use at your own risk. You may lose coins._
