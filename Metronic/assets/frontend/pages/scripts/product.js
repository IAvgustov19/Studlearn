const images = document.querySelectorAll('.product-slider .slider img');
const thumbnails = document.querySelectorAll('.product-slider .thumbnail-slider img');
let currentIndex = 0;

function showImage(index) {
    images[currentIndex].classList.remove('active');
    thumbnails[currentIndex].classList.remove('active');
    images[index].classList.add('active');
    thumbnails[index].classList.add('active');
    currentIndex = index;
}

document.querySelector('.product-slider').addEventListener('click', function (event) {
    if (event.target.classList.contains('prev')) {
        let index = currentIndex - 1;
        if (index < 0) {
            index = images.length - 1;
        }
        showImage(index);
    } else if (event.target.classList.contains('next')) {
        let index = currentIndex + 1;
        if (index >= images.length) {
            index = 0;
        }
        showImage(index);
    } else if (event.target.tagName === 'IMG' && event.target.parentElement.classList.contains('thumbnail-slider')) {
        const thumbnailIndex = Array.from(event.target.parentElement.children).indexOf(event.target);
        showImage(thumbnailIndex);
    }
});

document.querySelector('.slide-btn-left').addEventListener('click', function () {
    const thumbnailSlider = document.querySelector('.thumbnail-slider-container');
    thumbnailSlider.scroll({
        left: thumbnailSlider.scrollLeft - 150,
        behavior: 'smooth'
    });
});

document.querySelector('.slide-btn-right').addEventListener('click', function () {
    const thumbnailSlider = document.querySelector('.thumbnail-slider-container');
    thumbnailSlider.scroll({
        left: thumbnailSlider.scrollLeft + 150,
        behavior: 'smooth'
    });
});

document.querySelectorAll('.thumbnail-slider img').forEach(function (thumbnail, index) {
    thumbnail.addEventListener('click', function () {
        document.querySelectorAll('.thumbnail-slider img').forEach(function (img) {
            img.classList.remove('active');
        });

        thumbnail.classList.add('active');

        showImage(index);
    });
});

document.querySelectorAll('.rate-star').forEach(function (star, index) {
    star.addEventListener('click', function () {
        document.querySelectorAll('.rate-star').forEach(function (s, i) {
            if (i <= index) {
                s.classList.add('active');
            } else {
                s.classList.remove('active');
            }
        });
    });
});


showImage(currentIndex);

const imageList = document.querySelectorAll('.archive-slider img');
let curIndex = 0;

function showImageItem(index) {
    imageList[curIndex].classList.remove('active');
    imageList[index].classList.add('active');
    curIndex = index;
}

document.querySelector('.archive-slider').addEventListener('click', function (event) {
    if (event.target.classList.contains('prev-slide')) {
        let index = curIndex - 1;
        if (index < 0) {
            index = imageList.length - 1;
        }
        showImageItem(index);
    } else if (event.target.classList.contains('next-slide')) {
        let index = curIndex + 1;
        if (index >= imageList.length) {
            index = 0;
        }
        showImageItem(index);
    }
});

showImageItem(currentIndex);
