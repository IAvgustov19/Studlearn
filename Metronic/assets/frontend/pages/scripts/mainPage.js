let switcher = false;


document.addEventListener('DOMContentLoaded', function() {
    const articlesButton = document.querySelector('.all-articles');
    const hiddenArticles = document.querySelectorAll('.article-box:nth-child(n+4)');

    articlesButton.addEventListener('click', function() {
        if (window.innerWidth <= 600) {
            hiddenArticles.forEach(function(article, index) {
                setTimeout(function() {
                    article.style.display = switcher ? 'none' : 'flex';
                }, index * 200);
            });
            switcher = !switcher;
        }
    });
});
