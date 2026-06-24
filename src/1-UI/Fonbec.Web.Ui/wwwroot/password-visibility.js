window.togglePasswordVisibility = function (inputElement) {
    if (!inputElement) {
        return;
    }

    inputElement.type = inputElement.type === 'password' ? 'text' : 'password';
};
