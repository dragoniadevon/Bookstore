// Общий JavaScript сайта.

// Блокируем кнопку отправки после первого нажатия, если у неё есть
// атрибут data-loading-text (например, "Оплатить"). Так пользователь
// не отправит форму дважды, пока сервер обрабатывает запрос.
document.addEventListener("submit", function (event) {
    var form = event.target;

    // Если форму проверяет jQuery Validation и в ней есть ошибки — ничего не делаем
    if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) {
        return;
    }

    var button = form.querySelector("button[type=submit][data-loading-text]");
    if (button) {
        button.disabled = true;
        button.textContent = button.getAttribute("data-loading-text");
    }
});
