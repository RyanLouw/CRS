const menuToggle = document.querySelector('.menu-toggle');
menuToggle?.addEventListener('click', () => {
    const menu = document.getElementById('navigation-links');
    const opened = menu.classList.toggle('is-open');
    menuToggle.setAttribute('aria-expanded', String(opened));
});
