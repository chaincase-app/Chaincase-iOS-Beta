function dispatchEventWrapper(evt) {
  window.dispatchEvent(new CustomEvent(evt))
}

if (!navigator.share && navigator.clipboard) {
  navigator.share = (data) => {
    const toast = document.createElement('ion-toast');
    toast.message = data.title ? `${data.title} copied to clipboard` : 'Copied to clipboard';
    toast.duration = 2000;
    navigator.clipboard.writeText(data.text || data.url).catch(reason => console.error(reason));
    document.body.appendChild(toast);
    return toast.present();
  }
} else if (!navigator.share) {
  navigator.share = (data) => {
    console.warn("Wanted to use share API but not supported on this platform.", data);
    return Promise.resolve();
  }
}
