function navToggle() {
    const navBtnOpen = document.getElementById('navBtnOpen');
    const navBtnClose = document.getElementById('navBtnClose');
    const mainNav = document.getElementById('mainNav');
    const mobileContainer = document.querySelector('.products-header-mobile-container');
    const menuContainer = document.querySelector('.menu-container');
    const point = 680;

    navBtnOpen.onclick = function () {
        mainNav.classList.remove('nav-hidden');
        mainNav.style.right = '0';
        mobileContainer.style.display = 'none';
        currentHeight = menuContainer.style.height;
        menuContainer.style.height = "400px";
    }

    navBtnClose.onclick = function () {
        mainNav.style.right = '-100%';
        menuContainer.style.height = "100px";
        setTimeout(function () {
            mainNav.classList.add('nav-hidden');
            mobileContainer.style.display = 'flex';
        }, 1500);
    }

    window.addEventListener("resize", resizeHandler, false);

    function resizeHandler() {
        if (document.documentElement.clientWidth >= point) {
            mainNav.style.height = 'auto';
        } else {
            if (mainNav.classList.contains('nav-hidden')) {
                mainNav.style.height = 270;
            } else {
                mainNav.style.height = 300;
            }
        }
    }
}

navToggle();
