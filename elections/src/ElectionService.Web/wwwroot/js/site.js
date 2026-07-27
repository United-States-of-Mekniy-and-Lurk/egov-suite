const menuButton = document.querySelector('.menu-toggle');
const navigation = document.querySelector('#site-navigation');

menuButton?.addEventListener('click', () => {
  const isOpen = menuButton.getAttribute('aria-expanded') === 'true';
  menuButton.setAttribute('aria-expanded', String(!isOpen));
  navigation?.classList.toggle('open', !isOpen);
});

const keepaliveUrl = document.body.dataset.sessionKeepaliveUrl;
const sessionNotice = document.querySelector('[data-session-notice]');

if (keepaliveUrl && sessionNotice) {
  document.addEventListener('mklu:session-active', () => sessionNotice.setAttribute('hidden', ''));
  document.addEventListener('mklu:session-expired', (event) => {
    if (event.detail.status === 401) sessionNotice.removeAttribute('hidden');
  });
}