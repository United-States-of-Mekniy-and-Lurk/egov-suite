const search = document.querySelector('#service-search');
const cards = [...document.querySelectorAll('.service-card')];
const filters = [...document.querySelectorAll('[data-category]')].filter(element => element.tagName === 'BUTTON');
const empty = document.querySelector('#empty-results');
let category = 'all';

function filterServices() {
  const query = search.value.trim().toLowerCase();
  let visible = 0;

  for (const card of cards) {
    const matchesQuery = card.dataset.search.includes(query);
    const matchesCategory = category === 'all' || card.dataset.category === category;
    card.hidden = !(matchesQuery && matchesCategory);
    if (!card.hidden) visible += 1;
  }

  empty.hidden = visible > 0;
}

search?.addEventListener('input', filterServices);
for (const filter of filters) {
  filter.addEventListener('click', () => {
    category = filter.dataset.category;
    for (const item of filters) item.classList.toggle('active', item === filter);
    filterServices();
  });
}