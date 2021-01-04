var originalFetch = window.fetch;

window.fetch = function () {
  var args = [];
  for (var _i = 0; _i < arguments.length; _i++) {
    args[_i] = arguments[_i];
  }
  var url = args[0];
  if (typeof url === 'string' && url.match(/\.svg/)) {
    return new Promise(function (resolve, reject) {
      var req = new XMLHttpRequest();
      req.open('GET', url, true);
      req.addEventListener('load', function () {
        resolve({ ok: true, status: 200, text: function () { return Promise.resolve(req.responseText); } });
      });
      req.addEventListener('error', reject);
      req.send();
    });
  }
  else {
    return originalFetch.apply(void 0, args);
  }
};
