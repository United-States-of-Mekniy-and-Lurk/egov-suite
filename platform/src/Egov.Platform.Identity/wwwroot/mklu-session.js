const keepaliveUrl = document.body.dataset.sessionKeepaliveUrl;

if (keepaliveUrl) {
  const activeSessionWindow = 15 * 60_000;
  let lastActivityAt = Date.now();
  let keepaliveRequest;

  const keepSessionAlive = () => {
    if (document.visibilityState === 'hidden') return Promise.resolve();
    if (Date.now() - lastActivityAt > activeSessionWindow) return Promise.resolve();
    if (keepaliveRequest) return keepaliveRequest;

    keepaliveRequest = fetch(keepaliveUrl, {
      credentials: 'same-origin',
      cache: 'no-store',
      headers: { Accept: 'application/json' }
    })
      .then((response) => {
        document.dispatchEvent(new CustomEvent(
          response.ok ? 'mklu:session-active' : 'mklu:session-expired',
          { detail: { status: response.status } }));
      })
      .catch(() => {})
      .finally(() => { keepaliveRequest = undefined; });

    return keepaliveRequest;
  };

  ['input', 'keydown', 'pointerdown'].forEach((eventName) => {
    document.addEventListener(eventName, () => { lastActivityAt = Date.now(); }, { passive: true });
  });
  window.setInterval(keepSessionAlive, 30_000);
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
      lastActivityAt = Date.now();
      keepSessionAlive();
    }
  });
}