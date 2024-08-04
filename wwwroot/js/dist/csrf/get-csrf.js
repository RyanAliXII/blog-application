var getCSRF = function () {
    var _a;
    var input = document.querySelector('input[name="__RequestVerificationToken"]');
    return (_a = input === null || input === void 0 ? void 0 : input.value) !== null && _a !== void 0 ? _a : "";
};

export { getCSRF as default, getCSRF };
