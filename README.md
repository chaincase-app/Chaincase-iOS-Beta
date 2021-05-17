![Chaincase Logomark](https://user-images.githubusercontent.com/8525467/118331682-e9983700-b4d6-11eb-96c8-c14fe3250742.png)

### Chaincase
> The only private bitcoin wallet on iOS

📲️ [Beta Available on TestFlight](https://testflight.apple.com/join/zCW4kvBS)

🐦️ [Follow us on Twitter](https://twitter.com/chaincaseapp)

💬 [Say hi on Telegram](https://t.me/joinchat/R54C370QON9L-5Xq)

# Preserve your Privacy

### 🔀 Keep your blockchain privacy with [CoinJoin](https://en.bitcoin.it/wiki/CoinJoin)
- Full coin selection & label support

### 🧅 Keep your data private with [Tor](https://en.wikipedia.org/wiki/Tor_(anonymity_network)) by default

### 🛰 Sync privately with [BIP 158](https://github.com/bitcoin/bips/blob/master/bip-0158.mediawiki)

Chaincase is *experimental* beta software. Use at your own risk. Your feedback is greatly appreciated 🗽

## How does it work?
[▶️ Watch the video](https://youtu.be/Ctl1mIvaR9I)

[![Chaincase CoinJoin Tutorial Video "My Privacy Has Been Preserved!"](https://user-images.githubusercontent.com/8525467/118557826-be5e5380-b733-11eb-9115-aee38d2337f7.jpg)](https://youtu.be/Ctl1mIvaR9I)


## Building for iOS

make sure to have the Wasabi submodule installed:
```console
git submodule update --init --recursive
```

pull or build the Tor binary:
```console
git lfs pull
```

And install a provisioning profile to make use of the entitlements:
https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/device-provisioning/
