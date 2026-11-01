// Updates "in 2 Tagen, 21 Std." countdowns. The server already renders the text, so the page works without JavaScript.
(function () {
  var nodes = document.querySelectorAll('[data-countdown]');
  if (!nodes.length) return;
  function text(ms) {
    if (ms <= 0) return 'Jetzt verfügbar';
    var min = Math.floor(ms / 60000), h = Math.floor(min / 60), d = Math.floor(h / 24);
    if (d >= 1) return 'in ' + d + (d === 1 ? ' Tag' : ' Tagen') + ', ' + (h % 24) + ' Std.';
    if (h >= 1) return 'in ' + h + ' Std. ' + (min % 60) + ' Min.';
    return 'in ' + Math.max(1, min) + ' Min.';
  }
  function tick() {
    var now = Date.now();
    nodes.forEach(function (n) {
      var t = Date.parse(n.getAttribute('data-countdown'));
      if (!isNaN(t)) n.textContent = text(t - now);
    });
  }
  tick();
  setInterval(tick, 30000);
})();
