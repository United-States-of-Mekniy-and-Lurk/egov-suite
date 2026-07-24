lucide.createIcons();

const menuButton = document.querySelector('.mklu-menu-button');
const navigation = document.querySelector('.mklu-header-nav');

menuButton?.addEventListener('click', () => {
	const isOpen = menuButton.getAttribute('aria-expanded') === 'true';
	menuButton.setAttribute('aria-expanded', String(!isOpen));
	navigation?.classList.toggle('is-open', !isOpen);
});
